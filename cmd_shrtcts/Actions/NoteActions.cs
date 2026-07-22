using Spectre.Console;

namespace cmd_shrtcts;

public static partial class Actions
{
    private static readonly string NoteConfigKey = "NotesFilePath";

    public static void ConfigureNote(string unused)
    {
        Loader.EnsureUserAppSettingsExists();

        var userAppSettingsPath = Loader.GetUserAppSettingsPath();
        var currentNotesPath = GetNotesFilePath(userAppSettingsPath);

        // Show current configuration
        Console.WriteLine("");
        if (!string.IsNullOrWhiteSpace(currentNotesPath))
        {
            AnsiConsole.MarkupLine($"[cyan]Current notes file:[/] {currentNotesPath}");
            Console.WriteLine("");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]No notes file configured yet.[/]");
            Console.WriteLine("");
        }

        // Present options
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to do?")
                .PageSize(10)
                .AddChoices(new[]
                {
                    "Set new location",
                    currentNotesPath != null ? "Clear current location" : "Cancel",
                    "Cancel"
                })
        );

        switch (choice)
        {
            case "Set new location":
                PromptAndSetNotesFilePath(userAppSettingsPath);
                break;

            case "Clear current location":
                if (currentNotesPath != null)
                {
                    ClearNotesFilePath(userAppSettingsPath);
                    AnsiConsole.MarkupLine("[green]Notes configuration cleared.[/]");
                }
                break;

            case "Cancel":
                AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
                break;
        }
    }

    public static void AddNote(string unused)
    {
        Loader.EnsureUserAppSettingsExists();

        var userAppSettingsPath = Loader.GetUserAppSettingsPath();
        var notesFilePath = GetNotesFilePath(userAppSettingsPath);

        // If no notes file path is configured, prompt for one
        if (string.IsNullOrWhiteSpace(notesFilePath))
        {
            notesFilePath = PromptForNotesFilePath(userAppSettingsPath);
            if (string.IsNullOrWhiteSpace(notesFilePath))
            {
                AnsiConsole.MarkupLine("[red]No notes file path provided. Cancelled.[/]");
                return;
            }
        }

        // Ensure the directory exists
        var directory = Path.GetDirectoryName(notesFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Prompt for the note content
        Console.WriteLine("");
        AnsiConsole.MarkupLine("[green]Enter your note (or type 'exit' to cancel):[/]");
        var noteContent = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(noteContent) || noteContent.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
            return;
        }

        try
        {
            // Append the note as a markdown todo item
            var todoItem = $"- [ ] {noteContent}";

            if (File.Exists(notesFilePath))
            {
                File.AppendAllText(notesFilePath, $"{Environment.NewLine}{todoItem}");
            }
            else
            {
                File.WriteAllText(notesFilePath, todoItem);
            }

            AnsiConsole.MarkupLine($"[green]Note added to:[/] {notesFilePath}");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error saving note:[/] {ex.Message}");
            Loader.LogText($"Error in AddNote: {ex}");
        }
    }

    private static string? GetNotesFilePath(string userAppSettingsPath)
    {
        try
        {
            if (!File.Exists(userAppSettingsPath))
            {
                return null;
            }

            var json = File.ReadAllText(userAppSettingsPath);
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            if (config != null && config.TryGetValue(NoteConfigKey, out var value))
            {
                return value?.ToString();
            }
        }
        catch
        {
            // Silently fail if config can't be read
        }

        return null;
    }

    private static string PromptForNotesFilePath(string userAppSettingsPath)
    {
        var defaultPath = Path.Combine(Loader.GetUserDataDirectory(), "notes.md");

        AnsiConsole.MarkupLine("[yellow]No notes file configured.[/]");
        AnsiConsole.MarkupLine($"[cyan]Default location: {defaultPath}[/]");

        var useDefault = AnsiConsole.Confirm("Use default location?", defaultValue: true);
        string notesPath;

        if (useDefault)
        {
            notesPath = defaultPath;
        }
        else
        {
            notesPath = AnsiConsole.Ask<string>("Enter full path to notes file:");
        }

        // Ensure it has .md extension
        if (!notesPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            notesPath += ".md";
        }

        // Save the path to user appsettings
        SaveNotesFilePath(userAppSettingsPath, notesPath);

        return notesPath;
    }

    private static void PromptAndSetNotesFilePath(string userAppSettingsPath)
    {
        var defaultPath = Path.Combine(Loader.GetUserDataDirectory(), "notes.md");

        AnsiConsole.MarkupLine($"[cyan]Default location: {defaultPath}[/]");
        var useDefault = AnsiConsole.Confirm("Use default location?", defaultValue: true);

        string notesPath;
        if (useDefault)
        {
            notesPath = defaultPath;
        }
        else
        {
            notesPath = AnsiConsole.Ask<string>("Enter full path to notes file:");
        }

        // Ensure it has .md extension
        if (!notesPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            notesPath += ".md";
        }

        SaveNotesFilePath(userAppSettingsPath, notesPath);
        AnsiConsole.MarkupLine($"[green]Notes configured at:[/] {notesPath}");
    }

    private static void SaveNotesFilePath(string userAppSettingsPath, string notesPath)
    {
        try
        {
            var config = new Dictionary<string, object> { { NoteConfigKey, notesPath } };

            if (File.Exists(userAppSettingsPath))
            {
                var json = File.ReadAllText(userAppSettingsPath);
                config = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json) 
                    ?? new Dictionary<string, object>();
                config[NoteConfigKey] = notesPath;
            }

            var updatedJson = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(userAppSettingsPath, updatedJson);
        }
        catch (Exception ex)
        {
            Loader.LogText($"Error saving notes path to config: {ex}");
        }
    }

    private static void ClearNotesFilePath(string userAppSettingsPath)
    {
        try
        {
            if (!File.Exists(userAppSettingsPath))
            {
                return;
            }

            var json = File.ReadAllText(userAppSettingsPath);
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            if (config != null && config.ContainsKey(NoteConfigKey))
            {
                config.Remove(NoteConfigKey);
                var updatedJson = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(userAppSettingsPath, updatedJson);
            }
        }
        catch (Exception ex)
        {
            Loader.LogText($"Error clearing notes path from config: {ex}");
        }
    }
}
