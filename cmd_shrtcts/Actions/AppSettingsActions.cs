using Newtonsoft.Json;
using Spectre.Console;

namespace cmd_shrtcts;

public static partial class Actions
{
    public static void SetConfigPath(string unused)
    {
        Loader.EnsureUserAppSettingsExists();

        var existing = new ConfigurationModel();
        var userAppSettingsPath = Loader.GetUserAppSettingsPath();

        if (File.Exists(userAppSettingsPath))
        {
            try
            {
                var json = File.ReadAllText(userAppSettingsPath);
                existing = JsonConvert.DeserializeObject<ConfigurationModel>(json) ?? new ConfigurationModel();
            }
            catch
            {
                existing = new ConfigurationModel();
            }
        }

        var configPath = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter full path to your config json file:")
                .PromptStyle("green")
                .Validate(path =>
                {
                    if (string.IsNullOrWhiteSpace(path))
                        return ValidationResult.Error("Path cannot be empty.");

                    if (!File.Exists(path))
                        return ValidationResult.Error("File not found.");

                    return ValidationResult.Success();
                }));

        existing.ApplicationVars ??= new ApplicationVars();

        var current = (existing.ApplicationVars.InputConfigs ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (!current.Any(x => string.Equals(x, configPath, StringComparison.OrdinalIgnoreCase)))
        {
            current.Add(configPath);
        }

        existing.ApplicationVars.InputConfigs = current.ToArray();

        File.WriteAllText(userAppSettingsPath, JsonConvert.SerializeObject(existing, Formatting.Indented));
        AnsiConsole.MarkupLine($"[green]Saved:[/] {userAppSettingsPath}");
    }

    private sealed class ConfigurationModel
    {
        public ApplicationVars? ApplicationVars { get; set; }
    }

    private sealed class ApplicationVars
    {
        public string[]? InputConfigs { get; set; }
    }
}
