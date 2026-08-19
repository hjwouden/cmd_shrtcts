using Newtonsoft.Json;
using Spectre.Console;

namespace cmd_shrtcts;

public static partial class Actions
{
    internal const string CloseTimeoutKey = "CommandWindowTimeoutSeconds";

    // Lets the user set how many seconds the launcher window counts down before closing.
    public static void ConfigureCloseTimeout(string unused)
    {
        Loader.EnsureUserAppSettingsExists();
        var settingsPath = Loader.GetUserAppSettingsPath();

        Console.WriteLine();
        AnsiConsole.MarkupLine($"[cyan]Current window-close countdown:[/] {Loader.COMMAND_WINDOW_TIMEOUT_SECONDS} second(s)");
        Console.WriteLine();

        var seconds = AnsiConsole.Prompt(
            new TextPrompt<int>("[green]Seconds before the window closes (0 = close immediately):[/]")
                .DefaultValue(Loader.COMMAND_WINDOW_TIMEOUT_SECONDS)
                .Validate(s => s >= 0
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]Must be 0 or greater.[/]")));

        SaveUserIntSetting(CloseTimeoutKey, seconds, settingsPath);
        Loader.COMMAND_WINDOW_TIMEOUT_SECONDS = seconds;

        AnsiConsole.MarkupLine($"[green]Windows will now close after {seconds} second(s).[/]");
    }

    private static void SaveUserIntSetting(string key, int value, string userSettingsPath)
    {
        try
        {
            Loader.EnsureUserAppSettingsExists();
            var config = new Dictionary<string, object>();
            if (File.Exists(userSettingsPath))
                config = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    File.ReadAllText(userSettingsPath)) ?? new();
            config[key] = value;
            File.WriteAllText(userSettingsPath,
                JsonConvert.SerializeObject(config, Formatting.Indented));
        }
        catch (Exception ex) { Loader.LogText($"Error saving setting {key}: {ex}"); }
    }
}
