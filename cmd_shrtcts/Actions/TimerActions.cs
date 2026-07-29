using Newtonsoft.Json;
using Spectre.Console;

namespace cmd_shrtcts;

public static partial class Actions
{
    private const string TimerConfigKey = "TimerDataPath";

    private class TimerData
    {
        public List<ActiveGoal> ActiveGoals { get; set; } = new();
        public List<GoalRecord> History { get; set; } = new();

        // Legacy single-goal field, kept only so old data files migrate cleanly.
        [JsonProperty("Active", NullValueHandling = NullValueHandling.Ignore)]
        public ActiveGoal? LegacyActive { get; set; }
    }

    private sealed record GoalMenuItem(string Label, ActiveGoal? Goal)
    {
        public static GoalMenuItem For(ActiveGoal g) =>
            new($"{Markup.Escape(g.Goal)}  [grey]({g.ElapsedMinutes}/{g.AllocatedMinutes} min)[/]", g);
    }

    private class ActiveGoal
    {
        public string Goal { get; set; } = "";
        public int AllocatedMinutes { get; set; }
        public int ElapsedMinutes { get; set; }
        public string SavedAt { get; set; } = "";
    }

    private class GoalRecord
    {
        public string Goal { get; set; } = "";
        public int AllocatedMinutes { get; set; }
        public int MinutesSpent { get; set; }
        public string Date { get; set; } = "";
        public string Status { get; set; } = "";
    }

    public static void StartTimer(string unused)
    {
        Loader.EnsureUserAppSettingsExists();
        var dataPath = GetTimerFilePath(Loader.GetUserAppSettingsPath());
        var data = LoadTimerData(dataPath);

        while (true)
        {
            if (data.ActiveGoals.Count == 0)
            {
                StartNewGoal(data, dataPath);
                return;
            }

            Console.WriteLine();
            AnsiConsole.MarkupLine($"[yellow]Saved goals ({data.ActiveGoals.Count}):[/]");
            Console.WriteLine();

            const string startNewLabel = "+ Start a new goal";
            const string abandonLabel = "Abandon a saved goal";
            const string cancelLabel = "Cancel";

            var items = new List<GoalMenuItem> { new(startNewLabel, null) };
            items.AddRange(data.ActiveGoals.Select(GoalMenuItem.For));
            items.Add(new GoalMenuItem(abandonLabel, null));
            items.Add(new GoalMenuItem(cancelLabel, null));

            var picked = AnsiConsole.Prompt(
                new SelectionPrompt<GoalMenuItem>()
                    .Title("Pick a goal to work on")
                    .PageSize(15)
                    .MoreChoicesText("Move up and down to reveal more choices")
                    .UseConverter(i => i.Label)
                    .AddChoices(items)
            );

            if (picked.Label == cancelLabel) return;

            if (picked.Label == startNewLabel)
            {
                StartNewGoal(data, dataPath);
                return;
            }

            if (picked.Label == abandonLabel)
            {
                AbandonSavedGoal(data, dataPath);
                continue;
            }

            RunTimerLoop(picked.Goal!, data, dataPath);
            return;
        }
    }

    private static void StartNewGoal(TimerData data, string dataPath)
    {
        Console.WriteLine();
        var goal = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]What is your goal?[/]")
                .ValidationErrorMessage("[red]Goal cannot be empty.[/]")
        );

        var allocatedMinutes = AnsiConsole.Prompt(
            new TextPrompt<int>("[green]How many minutes to focus?[/]")
                .ValidationErrorMessage("[red]Please enter a number greater than 0.[/]")
                .Validate(m => m > 0
                    ? ValidationResult.Success()
                    : ValidationResult.Error("Must be greater than 0."))
        );

        var activeGoal = new ActiveGoal
        {
            Goal = goal,
            AllocatedMinutes = allocatedMinutes,
            ElapsedMinutes = 0,
            SavedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
        };
        data.ActiveGoals.Add(activeGoal);
        SaveTimerData(dataPath, data);

        RunTimerLoop(activeGoal, data, dataPath);
    }

    private static void AbandonSavedGoal(TimerData data, string dataPath)
    {
        const string cancelLabel = "Cancel";
        var items = data.ActiveGoals.Select(GoalMenuItem.For).ToList();
        items.Add(new GoalMenuItem(cancelLabel, null));

        Console.WriteLine();
        var picked = AnsiConsole.Prompt(
            new SelectionPrompt<GoalMenuItem>()
                .Title("Abandon which goal?")
                .PageSize(15)
                .UseConverter(i => i.Label)
                .AddChoices(items)
        );

        if (picked.Label == cancelLabel) return;

        var chosen = picked.Goal!;
        data.History.Add(new GoalRecord
        {
            Goal = chosen.Goal,
            AllocatedMinutes = chosen.AllocatedMinutes,
            MinutesSpent = chosen.ElapsedMinutes,
            Date = DateTime.Now.ToString("MM-dd-yyyy"),
            Status = "Abandoned"
        });
        data.ActiveGoals.Remove(chosen);
        SaveTimerData(dataPath, data);

        Console.WriteLine();
        AnsiConsole.MarkupLine($"[red]Abandoned:[/] {Markup.Escape(chosen.Goal)}");
    }

    // Runs the countdown and handles outcome. Loops on "Need more time".
    private static void RunTimerLoop(ActiveGoal activeGoal, TimerData data, string dataPath)
    {
        string goal = activeGoal.Goal;
        int allocatedMinutes = activeGoal.AllocatedMinutes;
        int totalElapsed = activeGoal.ElapsedMinutes;

        while (true)
        {
            int minutesRemaining = allocatedMinutes - totalElapsed;
            if (minutesRemaining <= 0) minutesRemaining = allocatedMinutes;

            Console.WriteLine();
            AnsiConsole.MarkupLine($"[bold green]Goal:[/] {Markup.Escape(goal)}");
            AnsiConsole.MarkupLine("  [grey]Enter[/] = complete early   [grey]P[/] = pause / resume   [grey]E[/] = end");
            Console.WriteLine();

            var startTime = DateTime.Now;
            var endTime = startTime.AddMinutes(minutesRemaining);
            bool completedEarly = false;
            bool interrupted = false;
            bool paused = false;
            DateTime pauseStartTime = default;
            TimeSpan totalPausedDuration = TimeSpan.Zero;

            // Use ANSI colour codes directly — AnsiConsole doesn't play well with \r
            while (!completedEarly && !interrupted)
            {
                if (!paused && DateTime.Now >= endTime)
                    break;

                var elapsedNow = paused ? pauseStartTime : DateTime.Now;
                double sessionElapsedMinutes = (elapsedNow - startTime - totalPausedDuration).TotalMinutes;
                double percent = allocatedMinutes > 0
                    ? Math.Clamp((totalElapsed + sessionElapsedMinutes) / allocatedMinutes * 100.0, 0, 100)
                    : 0;
                string progressBar = BuildProgressBar(percent);

                if (paused)
                {
                    Console.Write($"\r  \x1b[33m⏸  PAUSED\x1b[0m  (P to resume)  {progressBar}          ");
                }
                else
                {
                    var rem = endTime - DateTime.Now;
                    int mins = (int)rem.TotalMinutes;
                    int secs = rem.Seconds;
                    string colour = rem.TotalMinutes > 5 ? "\x1b[32m" : rem.TotalMinutes > 1 ? "\x1b[33m" : "\x1b[31m";
                    Console.Write($"\r  {colour}⏱  {mins:D2}:{secs:D2}\x1b[0m  remaining  {progressBar}          ");
                }

                try
                {
                    if (!Console.IsInputRedirected && Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(intercept: true);
                        switch (key.Key)
                        {
                            case ConsoleKey.Enter:
                                completedEarly = true;
                                break;
                            case ConsoleKey.P:
                            case ConsoleKey.Spacebar:
                                if (!paused)
                                {
                                    paused = true;
                                    pauseStartTime = DateTime.Now;
                                }
                                else
                                {
                                    var pausedFor = DateTime.Now - pauseStartTime;
                                    totalPausedDuration += pausedFor;
                                    endTime += pausedFor;
                                    paused = false;
                                }
                                break;
                            case ConsoleKey.E:
                            case ConsoleKey.Escape:
                                interrupted = true;
                                break;
                        }
                    }
                }
                catch (InvalidOperationException) { /* not an interactive terminal */ }

                Thread.Sleep(250);
            }

            if (paused)
                totalPausedDuration += DateTime.Now - pauseStartTime;

            Console.WriteLine();
            Console.WriteLine();

            var activeTime = (DateTime.Now - startTime) - totalPausedDuration;
            int sessionMinutes = (int)Math.Ceiling(Math.Max(0, activeTime.TotalMinutes));
            totalElapsed += sessionMinutes;

            if (completedEarly)
                AnsiConsole.MarkupLine("[green]Finished early![/]");
            else if (interrupted)
                AnsiConsole.MarkupLine("[yellow]Timer stopped.[/]");
            else
                AnsiConsole.MarkupLine("[yellow]Time's up![/]");

            Console.WriteLine();

            var outcome = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[bold]{Markup.Escape(goal)}[/] — what's the outcome?")
                    .AddChoices("Completed ✓", "Need more time", "Save for later")
            );

            if (outcome == "Completed ✓")
            {
                data.History.Add(new GoalRecord
                {
                    Goal = goal,
                    AllocatedMinutes = allocatedMinutes,
                    MinutesSpent = totalElapsed,
                    Date = DateTime.Now.ToString("MM-dd-yyyy"),
                    Status = "Completed"
                });
                data.ActiveGoals.Remove(activeGoal);
                SaveTimerData(dataPath, data);

                Console.WriteLine();
                AnsiConsole.MarkupLine("[green]Logged.[/]");
                ShowRecentGoals(data);
                break;
            }
            else if (outcome == "Need more time")
            {
                Console.WriteLine();
                int extra = AnsiConsole.Prompt(
                    new TextPrompt<int>("[green]How many more minutes?[/]")
                        .Validate(m => m > 0
                            ? ValidationResult.Success()
                            : ValidationResult.Error("Must be greater than 0."))
                );
                allocatedMinutes += extra;

                activeGoal.AllocatedMinutes = allocatedMinutes;
                activeGoal.ElapsedMinutes = totalElapsed;
                SaveTimerData(dataPath, data);
                // loop continues
            }
            else // Save for later
            {
                activeGoal.ElapsedMinutes = totalElapsed;
                SaveTimerData(dataPath, data);

                Console.WriteLine();
                AnsiConsole.MarkupLine("[cyan]Goal saved. Resume it next time with 'cc timer'.[/]");
                break;
            }
        }
    }

    public static void ViewTimerHistory(string unused)
    {
        Loader.EnsureUserAppSettingsExists();
        var dataPath = GetTimerFilePath(Loader.GetUserAppSettingsPath());
        var data = LoadTimerData(dataPath);

        Console.WriteLine();

        if (data.ActiveGoals.Count > 0)
        {
            AnsiConsole.MarkupLine($"[yellow]Saved goals ({data.ActiveGoals.Count}):[/]");
            foreach (var g in data.ActiveGoals)
                AnsiConsole.MarkupLine($"  • {Markup.Escape(g.Goal)}  [grey]({g.ElapsedMinutes}/{g.AllocatedMinutes} min spent so far)[/]");
            Console.WriteLine();
        }

        if (data.History.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No goals logged yet.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn(new TableColumn("[bold]Goal[/]").NoWrap())
            .AddColumn(new TableColumn("[bold]Allocated[/]").Centered())
            .AddColumn(new TableColumn("[bold]Spent[/]").Centered())
            .AddColumn(new TableColumn("[bold]Date[/]").Centered())
            .AddColumn(new TableColumn("[bold]Status[/]").Centered());

        foreach (var r in data.History.TakeLast(20))
        {
            var statusMarkup = r.Status switch
            {
                "Completed" => "[green]✓ Completed[/]",
                "Abandoned" => "[red]✗ Abandoned[/]",
                _ => $"[yellow]{Markup.Escape(r.Status)}[/]"
            };

            table.AddRow(
                Markup.Escape(r.Goal),
                $"{r.AllocatedMinutes} min",
                $"{r.MinutesSpent} min",
                r.Date,
                statusMarkup
            );
        }

        AnsiConsole.Write(table);
    }

    public static void ConfigureTimer(string unused)
    {
        Loader.EnsureUserAppSettingsExists();
        var settingsPath = Loader.GetUserAppSettingsPath();
        var currentPath = GetTimerFilePath(settingsPath);
        var defaultPath = Path.Combine(Loader.GetUserDataDirectory(), "timer-data.json");

        Console.WriteLine();
        AnsiConsole.MarkupLine($"[cyan]Current timer data file:[/] {currentPath}");
        Console.WriteLine();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to do?")
                .PageSize(10)
                .AddChoices("Set new location", "Reset to default", "Cancel")
        );

        switch (choice)
        {
            case "Set new location":
                var newPath = AnsiConsole.Ask<string>("[green]Enter full path for timer data file:[/]");
                if (!newPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    newPath += ".json";
                SaveTimerFilePath(settingsPath, newPath);
                AnsiConsole.MarkupLine($"[green]Timer data will be saved to:[/] {newPath}");
                break;

            case "Reset to default":
                SaveTimerFilePath(settingsPath, defaultPath);
                AnsiConsole.MarkupLine($"[green]Reset to default:[/] {defaultPath}");
                break;
        }
    }

    private static void ShowRecentGoals(TimerData data)
    {
        var recent = data.History.TakeLast(5).ToList();
        if (recent.Count == 0) return;

        Console.WriteLine();
        AnsiConsole.MarkupLine("[grey]Recent goals:[/]");
        foreach (var r in recent)
        {
            var icon = r.Status == "Completed" ? "[green]✓[/]" : "[red]✗[/]";
            AnsiConsole.MarkupLine($"  {icon} {Markup.Escape(r.Goal)}  [grey]({r.MinutesSpent} min · {r.Date})[/]");
        }
    }

    private static string BuildProgressBar(double percent, int width = 24)
    {
        int filled = Math.Clamp((int)Math.Round(width * percent / 100.0), 0, width);
        string colour = percent >= 100 ? "\x1b[32m" : percent >= 60 ? "\x1b[33m" : "\x1b[36m";
        string bar = new string('█', filled) + new string('░', width - filled);
        return $"{colour}[{bar}]\x1b[0m {percent,3:F0}%";
    }

    private static string GetTimerFilePath(string userAppSettingsPath)
    {
        var defaultPath = Path.Combine(Loader.GetUserDataDirectory(), "timer-data.json");
        try
        {
            if (!File.Exists(userAppSettingsPath)) return defaultPath;
            var config = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                File.ReadAllText(userAppSettingsPath));
            if (config != null && config.TryGetValue(TimerConfigKey, out var val))
                return val?.ToString() ?? defaultPath;
        }
        catch { }
        return defaultPath;
    }

    private static void SaveTimerFilePath(string userAppSettingsPath, string path)
    {
        try
        {
            var config = new Dictionary<string, object>();
            if (File.Exists(userAppSettingsPath))
                config = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    File.ReadAllText(userAppSettingsPath)) ?? new();
            config[TimerConfigKey] = path;
            File.WriteAllText(userAppSettingsPath,
                JsonConvert.SerializeObject(config, Formatting.Indented));
        }
        catch (Exception ex) { Loader.LogText($"Error saving timer file path: {ex}"); }
    }

    private static TimerData LoadTimerData(string path)
    {
        try
        {
            if (!File.Exists(path)) return new TimerData();
            var data = JsonConvert.DeserializeObject<TimerData>(File.ReadAllText(path)) ?? new TimerData();
            if (data.LegacyActive != null)
            {
                data.ActiveGoals.Add(data.LegacyActive);
                data.LegacyActive = null;
            }
            return data;
        }
        catch { return new TimerData(); }
    }

    private static void SaveTimerData(string path, TimerData data)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
        }
        catch (Exception ex) { Loader.LogText($"Error saving timer data: {ex}"); }
    }
}
