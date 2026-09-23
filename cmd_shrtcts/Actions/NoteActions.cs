using Spectre.Console;

namespace cmd_shrtcts;

public static partial class Actions
{
    private static readonly string NoteConfigKey = "NotesFilePath";

    // Resolves a user-entered notes location into a concrete file path:
    //  - An existing folder (or a path ending in a separator) => a default file placed INSIDE it,
    //    e.g. "~/Notes/00_Inbox" -> "~/Notes/00_Inbox/notes.md".
    //  - A path that already has an extension => used as-is (e.g. "todo.md", "list.txt").
    //  - A path with no extension that isn't a folder => ".md" is appended.
    // Also expands a leading '~' and %VAR% environment variables.
    internal static string NormalizeNotesPath(string input, string defaultFileName = "notes.md")
    {
        var path = Loader.ExpandPath(input.Trim());

        bool looksLikeDirectory =
            Directory.Exists(path)
            || path.EndsWith(Path.DirectorySeparatorChar)
            || path.EndsWith(Path.AltDirectorySeparatorChar);

        if (looksLikeDirectory)
        {
            var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.Combine(trimmed, defaultFileName);
        }

        // A file path — give it a .md extension only if it has none.
        if (string.IsNullOrEmpty(Path.GetExtension(path)))
            path += ".md";

        return path;
    }

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

    private static readonly string QuickNoteFolderConfigKey = "QuickNoteFolderPath";
    private static readonly string QuickNoteEditorConfigKey = "QuickNoteEditor";

    public static void ConfigureQuickNote(string unused)
    {
        Loader.EnsureUserAppSettingsExists();

        var userAppSettingsPath = Loader.GetUserAppSettingsPath();
        var currentFolder = GetQuickNoteFolderPath(userAppSettingsPath);
        var currentEditor = GetQuickNoteEditor(userAppSettingsPath);

        Console.WriteLine("");
        AnsiConsole.MarkupLine(string.IsNullOrWhiteSpace(currentFolder)
            ? "[yellow]No quick note folder configured yet.[/]"
            : $"[cyan]Current quick note folder:[/] {currentFolder}");
        AnsiConsole.MarkupLine($"[cyan]Current editor:[/] {DisplayEditorName(currentEditor)}");
        Console.WriteLine("");

        var choices = new List<string> { "Set folder location", "Set editor" };
        if (!string.IsNullOrWhiteSpace(currentFolder))
            choices.Add("Clear folder location");
        choices.Add("Cancel");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to do?")
                .PageSize(10)
                .AddChoices(choices)
        );

        switch (choice)
        {
            case "Set folder location":
                PromptAndSetQuickNoteFolderPath(userAppSettingsPath);
                break;

            case "Set editor":
                var newEditor = PromptForEditor(currentEditor);
                SaveQuickNoteEditor(userAppSettingsPath, newEditor);
                AnsiConsole.MarkupLine($"[green]Quick note editor set to:[/] {DisplayEditorName(newEditor)}");
                break;

            case "Clear folder location":
                ClearQuickNoteFolderPath(userAppSettingsPath);
                AnsiConsole.MarkupLine("[green]Quick note folder cleared.[/]");
                break;

            case "Cancel":
                AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
                break;
        }
    }

    private static string? GetQuickNoteFolderPath(string userAppSettingsPath)
    {
        try
        {
            if (!File.Exists(userAppSettingsPath))
            {
                return null;
            }

            var json = File.ReadAllText(userAppSettingsPath);
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            if (config != null && config.TryGetValue(QuickNoteFolderConfigKey, out var value))
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

    private static string? GetQuickNoteEditor(string userAppSettingsPath)
    {
        try
        {
            if (!File.Exists(userAppSettingsPath))
            {
                return null;
            }

            var json = File.ReadAllText(userAppSettingsPath);
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            if (config != null && config.TryGetValue(QuickNoteEditorConfigKey, out var value))
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

    private static string PromptForQuickNoteFolderPath(string userAppSettingsPath)
    {
        var defaultPath = Path.Combine(Loader.GetUserDataDirectory(), "QuickNotes");

        AnsiConsole.MarkupLine("[yellow]No quick note folder configured.[/]");
        AnsiConsole.MarkupLine($"[cyan]Default location: {defaultPath}[/]");

        var useDefault = AnsiConsole.Confirm("Use default location?", defaultValue: true);
        var folderPath = useDefault
            ? defaultPath
            : Loader.ExpandPath(AnsiConsole.Ask<string>("Enter folder path for quick notes:").Trim());

        SaveQuickNoteFolderPath(userAppSettingsPath, folderPath);
        AnsiConsole.MarkupLine($"[cyan]Quick notes folder:[/] {folderPath}");

        return folderPath;
    }

    private static void PromptAndSetQuickNoteFolderPath(string userAppSettingsPath)
    {
        var defaultPath = Path.Combine(Loader.GetUserDataDirectory(), "QuickNotes");

        AnsiConsole.MarkupLine($"[cyan]Default location: {defaultPath}[/]");
        var useDefault = AnsiConsole.Confirm("Use default location?", defaultValue: true);
        var folderPath = useDefault
            ? defaultPath
            : Loader.ExpandPath(AnsiConsole.Ask<string>("Enter folder path for quick notes:").Trim());

        SaveQuickNoteFolderPath(userAppSettingsPath, folderPath);
        AnsiConsole.MarkupLine($"[green]Quick notes will be saved to:[/] {folderPath}");
    }

    private static void SaveQuickNoteFolderPath(string userAppSettingsPath, string folderPath)
    {
        try
        {
            var config = new Dictionary<string, object>();

            if (File.Exists(userAppSettingsPath))
            {
                var json = File.ReadAllText(userAppSettingsPath);
                config = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json)
                    ?? new Dictionary<string, object>();
            }
            config[QuickNoteFolderConfigKey] = folderPath;

            var updatedJson = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(userAppSettingsPath, updatedJson);
        }
        catch (Exception ex)
        {
            Loader.LogText($"Error saving quick note folder path to config: {ex}");
        }
    }

    private static void SaveQuickNoteEditor(string userAppSettingsPath, string? editor)
    {
        try
        {
            var config = new Dictionary<string, object>();

            if (File.Exists(userAppSettingsPath))
            {
                var json = File.ReadAllText(userAppSettingsPath);
                config = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json)
                    ?? new Dictionary<string, object>();
            }

            if (string.IsNullOrEmpty(editor))
                config.Remove(QuickNoteEditorConfigKey);
            else
                config[QuickNoteEditorConfigKey] = editor;

            var updatedJson = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(userAppSettingsPath, updatedJson);
        }
        catch (Exception ex)
        {
            Loader.LogText($"Error saving quick note editor to config: {ex}");
        }
    }

    private static void ClearQuickNoteFolderPath(string userAppSettingsPath)
    {
        try
        {
            if (!File.Exists(userAppSettingsPath))
            {
                return;
            }

            var json = File.ReadAllText(userAppSettingsPath);
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            if (config != null && config.ContainsKey(QuickNoteFolderConfigKey))
            {
                config.Remove(QuickNoteFolderConfigKey);
                var updatedJson = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(userAppSettingsPath, updatedJson);
            }
        }
        catch (Exception ex)
        {
            Loader.LogText($"Error clearing quick note folder path from config: {ex}");
        }
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

        // Resolve folder-vs-file (a folder gets a default notes file placed inside it).
        notesPath = NormalizeNotesPath(notesPath);
        AnsiConsole.MarkupLine($"[cyan]Notes file:[/] {notesPath}");

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

        // Resolve folder-vs-file (a folder gets a default notes file placed inside it).
        notesPath = NormalizeNotesPath(notesPath);

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
