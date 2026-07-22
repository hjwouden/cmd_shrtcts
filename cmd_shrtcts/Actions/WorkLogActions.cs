using Spectre.Console;

namespace cmd_shrtcts;

public static partial class Actions
{
    private static readonly string WorkLogConfigKey = "WorkLogFilePath";

    public static void AddWorkLog(string unused)
    {
        Loader.EnsureUserAppSettingsExists();

        var userAppSettingsPath = Loader.GetUserAppSettingsPath();
        var workLogFilePath = GetWorkLogFilePath(userAppSettingsPath);

        if (string.IsNullOrWhiteSpace(workLogFilePath))
        {
            workLogFilePath = PromptForWorkLogFilePath(userAppSettingsPath);
            if (string.IsNullOrWhiteSpace(workLogFilePath))
            {
                AnsiConsole.MarkupLine("[red]No work log file path provided. Cancelled.[/]");
                return;
            }
        }

        var directory = Path.GetDirectoryName(workLogFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        Console.WriteLine("");
        AnsiConsole.MarkupLine("[green]Work log entry[/] [grey](or 'exit' to cancel):[/]");
        var entryContent = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(entryContent) || entryContent.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
            return;
        }

        try
        {
            // Format: "06-24-2026 7:19 AM = entry text"
            var timestamp = DateTime.Now.ToString("MM-dd-yyyy h:mm tt");
            var logEntry = $"{timestamp} = {entryContent}";

            if (File.Exists(workLogFilePath))
                File.AppendAllText(workLogFilePath, Environment.NewLine + logEntry);
            else
                File.WriteAllText(workLogFilePath, logEntry);

            AnsiConsole.MarkupLine($"[green]Logged to:[/] {workLogFilePath}");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error saving work log:[/] {ex.Message}");
            Loader.LogText($"Error in AddWorkLog: {ex}");
        }
    }

    public static void ConfigureWorkLog(string unused)
    {
        Loader.EnsureUserAppSettingsExists();

        var userAppSettingsPath = Loader.GetUserAppSettingsPath();
        var currentPath = GetWorkLogFilePath(userAppSettingsPath);

        Console.WriteLine("");
        if (!string.IsNullOrWhiteSpace(currentPath))
        {
            AnsiConsole.MarkupLine($"[cyan]Current work log file:[/] {currentPath}");
            Console.WriteLine("");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]No work log file configured yet.[/]");
            Console.WriteLine("");
        }

        var choices = new List<string> { "Set new location" };
        if (!string.IsNullOrWhiteSpace(currentPath))
            choices.Add("Clear current location");
        choices.Add("Cancel");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to do?")
                .PageSize(10)
                .AddChoices(choices)
        );

        switch (choice)
        {
            case "Set new location":
                PromptAndSetWorkLogFilePath(userAppSettingsPath);
                break;
            case "Clear current location":
                ClearWorkLogFilePath(userAppSettingsPath);
                AnsiConsole.MarkupLine("[green]Work log configuration cleared.[/]");
                break;
            default:
                AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
                break;
        }
    }

    private static string? GetWorkLogFilePath(string userAppSettingsPath)
    {
        try
        {
            if (!File.Exists(userAppSettingsPath)) return null;
            var json = File.ReadAllText(userAppSettingsPath);
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (config != null && config.TryGetValue(WorkLogConfigKey, out var value))
                return value?.ToString();
        }
        catch { }
        return null;
    }

    private static string PromptForWorkLogFilePath(string userAppSettingsPath)
    {
        var defaultPath = Path.Combine(Loader.GetUserDataDirectory(), "worklog.txt");

        AnsiConsole.MarkupLine("[yellow]No work log file configured.[/]");
        AnsiConsole.MarkupLine($"[cyan]Default location: {defaultPath}[/]");

        var useDefault = AnsiConsole.Confirm("Use default location?", defaultValue: true);
        var logPath = useDefault ? defaultPath : AnsiConsole.Ask<string>("Enter full path to work log file:");

        SaveWorkLogFilePath(userAppSettingsPath, logPath);
        return logPath;
    }

    private static void PromptAndSetWorkLogFilePath(string userAppSettingsPath)
    {
        var defaultPath = Path.Combine(Loader.GetUserDataDirectory(), "worklog.txt");

        AnsiConsole.MarkupLine($"[cyan]Default location: {defaultPath}[/]");
        var useDefault = AnsiConsole.Confirm("Use default location?", defaultValue: true);
        var logPath = useDefault ? defaultPath : AnsiConsole.Ask<string>("Enter full path to work log file:");

        SaveWorkLogFilePath(userAppSettingsPath, logPath);
        AnsiConsole.MarkupLine($"[green]Work log configured at:[/] {logPath}");
    }

    private static void SaveWorkLogFilePath(string userAppSettingsPath, string logPath)
    {
        try
        {
            var config = new Dictionary<string, object>();
            if (File.Exists(userAppSettingsPath))
                config = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    File.ReadAllText(userAppSettingsPath)) ?? new();
            config[WorkLogConfigKey] = logPath;
            File.WriteAllText(userAppSettingsPath,
                Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented));
        }
        catch (Exception ex) { Loader.LogText($"Error saving work log path: {ex}"); }
    }

    private static void ClearWorkLogFilePath(string userAppSettingsPath)
    {
        try
        {
            if (!File.Exists(userAppSettingsPath)) return;
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(
                File.ReadAllText(userAppSettingsPath));
            if (config != null && config.ContainsKey(WorkLogConfigKey))
            {
                config.Remove(WorkLogConfigKey);
                File.WriteAllText(userAppSettingsPath,
                    Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented));
            }
        }
        catch (Exception ex) { Loader.LogText($"Error clearing work log path: {ex}"); }
    }
}
