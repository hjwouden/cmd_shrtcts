using Spectre.Console;

namespace cmd_shrtcts;

public static partial class Actions
{
    internal const string SuccessSoundKey = "SuccessSoundPath";
    internal const string ErrorSoundKey = "ErrorSoundPath";

    public static void ConfigureSuccessSound(string unused)
    {
        var newPath = PromptForSoundPath(SuccessSoundKey, "success chime");
        if (newPath != null)
            Loader.SUCCESS_SOUND_FILE_PATH = newPath;
    }

    public static void ConfigureErrorSound(string unused)
    {
        var newPath = PromptForSoundPath(ErrorSoundKey, "error chord");
        if (newPath != null)
            Loader.ERROR_SOUND_FILE_PATH = newPath;
    }

    // Returns the newly-set path, or null if the user cancelled or cleared.
    private static string PromptForSoundPath(string key, string soundKind)
    {
        Loader.EnsureUserAppSettingsExists();
        var userAppSettingsPath = Loader.GetUserAppSettingsPath();
        var current = GetSoundConfigPath(key, userAppSettingsPath);

        Console.WriteLine("");
        if (!string.IsNullOrWhiteSpace(current))
            AnsiConsole.MarkupLine($"[cyan]Current {soundKind} sound:[/] {current}");
        else
            AnsiConsole.MarkupLine($"[yellow]No custom {soundKind} sound configured (using bundled default).[/]");
        Console.WriteLine("");

        var choices = new List<string> { "Set new path", "Cancel" };
        if (!string.IsNullOrWhiteSpace(current))
            choices.Insert(1, "Clear (revert to default)");

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"Configure {soundKind} sound:")
                .PageSize(10)
                .AddChoices(choices)
        );

        switch (choice)
        {
            case "Set new path":
                var path = AnsiConsole.Prompt(
                    new TextPrompt<string>($"Enter full path to {soundKind} sound file (.wav):")
                        .PromptStyle("green")
                        .Validate(p =>
                        {
                            if (string.IsNullOrWhiteSpace(p))
                                return ValidationResult.Error("Path cannot be empty.");
                            if (!File.Exists(p))
                                return ValidationResult.Error("File not found.");
                            return ValidationResult.Success();
                        }));
                SaveSoundConfigPath(key, path, userAppSettingsPath);
                AnsiConsole.MarkupLine($"[green]Saved {soundKind} sound:[/] {path}");
                return path;

            case "Clear (revert to default)":
                ClearSoundConfigPath(key, userAppSettingsPath);
                AnsiConsole.MarkupLine($"[green]{soundKind} sound reset to default (takes effect next run).[/]");
                return null;

            default:
                AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
                return null;
        }
    }

    internal static string GetSoundConfigPath(string key, string userAppSettingsPath)
    {
        try
        {
            if (!File.Exists(userAppSettingsPath)) return null;
            var json = File.ReadAllText(userAppSettingsPath);
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (config != null && config.TryGetValue(key, out var value))
                return value?.ToString();
        }
        catch { }
        return null;
    }

    private static void SaveSoundConfigPath(string key, string path, string userAppSettingsPath)
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
            config[key] = path;
            File.WriteAllText(userAppSettingsPath, Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented));
        }
        catch (Exception ex)
        {
            Loader.LogText($"Error saving sound config path: {ex}");
        }
    }

    private static void ClearSoundConfigPath(string key, string userAppSettingsPath)
    {
        try
        {
            if (!File.Exists(userAppSettingsPath)) return;
            var json = File.ReadAllText(userAppSettingsPath);
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (config != null && config.ContainsKey(key))
            {
                config.Remove(key);
                File.WriteAllText(userAppSettingsPath, Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented));
            }
        }
        catch (Exception ex)
        {
            Loader.LogText($"Error clearing sound config path: {ex}");
        }
    }
}
