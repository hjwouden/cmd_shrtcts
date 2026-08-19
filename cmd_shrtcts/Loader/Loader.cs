using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;

[assembly: InternalsVisibleTo("cmd_shrtcts.Tests")]

namespace cmd_shrtcts
{
    public class Loader
    {
        //CONFIGURATION VALUES
        public static string ASSEMBLY_LOCATION = "";
        public static string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
        public static string? SUCCESS_SOUND_FILE_PATH = Path.Combine("Data", "Sounds", "chime.wav");
        public static string? ERROR_SOUND_FILE_PATH = Path.Combine("Data", "Sounds", "chord.wav");
        public static string OUTPUT_LOG_FILE_PATH = "log.txt";
        public static int COMMAND_WINDOW_TIMEOUT_SECONDS = 5; // How long the command window stays open (in seconds)
        public static bool QUOTES_ENABLED = true;
        public static string[] INPUT_CONFIG_LOCATIONS =
            {
                Path.Combine("Data", "Configs", "system-config.json")
            };

        // Set by integration tests to redirect all user-data reads/writes to a temp directory.
        // Always null in production.
        internal static string? _testUserDataDirOverride = null;

        // Set by integration tests to redirect appsettings specifically (takes precedence over _testUserDataDirOverride).
        // Always null in production.
        internal static string? _testAppSettingsOverride = null;

        public static string GetUserDataDirectory()
        {
            if (_testUserDataDirOverride != null) return _testUserDataDirOverride;
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(baseDir, "cmd_shrtcts");
        }

        public static string GetUserAppSettingsPath()
        {
            if (_testAppSettingsOverride != null) return _testAppSettingsOverride;
            return Path.Combine(GetUserDataDirectory(), "appsettings.json");
        }

        public static string GetUserQuotesPath()
        {
            return Path.Combine(GetUserDataDirectory(), "quotes.txt");
        }

        // Returns the user quotes file if it exists, otherwise falls back to the bundled file.
        public static string GetEffectiveQuotesPath()
        {
            var userPath = GetUserQuotesPath();
            if (File.Exists(userPath)) return userPath;
            return ChangeFromLocalToDirectoryPath(Path.Combine("Data", "Quotes", "movie-quotes.txt"));
        }

        public static string GetPackagedAppSettingsPath()
        {
            return Path.Combine(ASSEMBLY_LOCATION, "appsettings.json");
        }

        public static void EnsureUserAppSettingsExists()
        {
            var userDir = GetUserDataDirectory();
            var userSettings = GetUserAppSettingsPath();
            if (File.Exists(userSettings))
            {
                return;
            }

            Directory.CreateDirectory(userDir);

            var packaged = GetPackagedAppSettingsPath();
            if (File.Exists(packaged))
            {
                File.Copy(packaged, userSettings, overwrite: false);
            }
            else
            {
                var defaultConfig = new
                {
                    ApplicationVars = new
                    {
                        InputConfigs = new[] { Path.Combine("Data", "Configs", "system-config.json") }
                    }
                };
                File.WriteAllText(userSettings, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented) + Environment.NewLine);
            }
        }

        //SHARED OBJECTS
        public static Dictionary<string, Action<object>>? actionsDictionary1;
        public static Dictionary<string, Root>? actionsDictionary;

        public class Root
        {
            public List<string>? AdditionalNames { get; set; }
            public string? action { get; set; }
            public string? parameter { get; set; }
            // Optional per-OS overrides for 'parameter' (e.g. a notes folder that differs on
            // Mac vs Windows). When the one matching the current OS is set, it wins over
            // 'parameter'. Supports leading '~' (home) and %VAR% environment expansion.
            public string? parameterWindows { get; set; }
            public string? parameterMac { get; set; }
            public string? parameterLinux { get; set; }
            public string? category { get; set; }
            public string? description { get; set; }
            // Optional browser for OpenWebPage entries: "chrome", "edge", "firefox".
            // Null/empty means open in the operating system's default browser.
            public string? browser { get; set; }
        }

        public static bool TryGetParameterFromJson(string value, out object? parameter)
        {
            if(Loader.actionsDictionary != null)
            {
                if (Loader.actionsDictionary.TryGetValue(value, out Loader.Root? result))
                {
                    parameter = result.parameter;
                    return true;
                }
            }
            parameter = null;
            return false;
        }

        public static string ChangeFromLocalToDirectoryPath(string fileName)
        {
            // Normalize path separators if they are hardcoded as \
            string normalizedFileName = fileName.Replace('\\', Path.DirectorySeparatorChar);
            return Path.Combine(Loader.ASSEMBLY_LOCATION, normalizedFileName);
        }

        // Chooses the parameter appropriate to the current OS: a matching parameterMac/
        // parameterWindows/parameterLinux wins over the generic 'parameter'. The chosen
        // OS-specific value is path-expanded (~ and %VAR%); the generic value is returned as-is
        // for backward compatibility.
        internal static string? ResolveEffectiveParameter(Root a)
        {
            string? osSpecific =
                OperatingSystem.IsWindows() ? a.parameterWindows :
                OperatingSystem.IsMacOS() ? a.parameterMac :
                a.parameterLinux;

            if (!string.IsNullOrWhiteSpace(osSpecific))
                return ExpandPath(osSpecific);

            return a.parameter;
        }

        // Expands a leading '~' to the user's home directory and %VAR% environment variables.
        internal static string ExpandPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            string expanded = Environment.ExpandEnvironmentVariables(path);

            if (expanded == "~")
                expanded = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            else if (expanded.StartsWith("~/") || expanded.StartsWith("~\\"))
                expanded = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    expanded.Substring(2));

            return expanded;
        }

        public static Dictionary<string, Root> LoadActionsDictionary(string[] configFiles)
        {
            Loader.LogText($"Loader: Loading Actions from Sources:");
            configFiles.ToList().ForEach(value => Loader.LogText("\t" + value));

            Dictionary<string, Root> actions = new Dictionary<string, Root>();
            actionsDictionary1 = new Dictionary<string, Action<object>>();

            foreach(string configFile in configFiles) 
            {
                string useThisConfigFilePath = configFile;
               
                if (!File.Exists(useThisConfigFilePath))
                {
                    useThisConfigFilePath = ChangeFromLocalToDirectoryPath(configFile);
                }

                if (!File.Exists(useThisConfigFilePath))
                {
                    LogText($"Configuration file not found: {configFile} (checked: {useThisConfigFilePath})");
                    continue; // Skip this config file instead of crashing
                }

                try
                {
                    string json = File.ReadAllText(useThisConfigFilePath);
                    List<Root>? config = JsonConvert.DeserializeObject<List<Root>>(json);

                    if (config != null && config.Count > 0)
                    {
                        foreach (Root a in config)
                        {
                            if (a.AdditionalNames == null || a.AdditionalNames.Count == 0)
                            {
                                LogText("Skipping config entry with no additional names");
                                continue;
                            }

                            foreach (string b in a.AdditionalNames)
                            {
                                // OpenWebPage entries capture their per-entry browser choice so it
                                // reaches Actions.OpenWebPage at invoke time; all other actions use
                                // the generic delegate.
                                Action<object>? action;
                                if (string.Equals(a.action, "OpenWebPage", StringComparison.OrdinalIgnoreCase))
                                {
                                    string? browser = a.browser;
                                    action = (parameter) => Actions.OpenWebPage(parameter?.ToString() ?? string.Empty, browser);
                                }
                                else if (!TryGetActionDelegate(a.action, out action) || action == null)
                                {
                                    LogText("Invalid action configuration: " + b);
                                    Actions.PlaySound("error");
                                    continue;
                                }

                                // Store keys in lowercase for case-insensitive lookup
                                string lowerKey = b.ToLowerInvariant();
                                actionsDictionary1[lowerKey] = action;
                                actions[lowerKey] = new Root { AdditionalNames = a.AdditionalNames, action = a.action, parameter = ResolveEffectiveParameter(a), parameterWindows = a.parameterWindows, parameterMac = a.parameterMac, parameterLinux = a.parameterLinux, category = a.category, description = a.description, browser = a.browser };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogText($"Error loading config file {useThisConfigFilePath}: {ex.Message}");
                }
            }

            if (actions.Count == 0)
            {
                LogText("WARNING: No actions loaded from any config file!");
            }

            return actions;
        }

        public static bool TryGetActionDelegate(string? actionName, out Action<object>? action)
        {
            switch (actionName)
            {
                case "OpenWebPage":
                    action = (parameter) => Actions.OpenWebPage(parameter.ToString() ?? string.Empty);
                    return true;
                case "AddToConfig":
                    action = (parameter) => Actions.AddToConfig(parameter.ToString() ?? string.Empty);
                    return true;
                case "OpenFile":
                    action = (parameter) => Actions.OpenFile(parameter.ToString() ?? string.Empty);
                    return true;
                case "OpenCMD":
                    action = (parameter) => Actions.OpenCMD(parameter.ToString() ?? string.Empty);
                    return true;
                case "OpenCMDWithParams":
                    action = (parameter) => Actions.OpenCMDWithParams(parameter.ToString()!.Split(',')[0], parameter.ToString()!.Split(',')[1]);
                    return true;
                case "list":
                    action = (parameter) => Actions.ListActions(parameter.ToString() ?? string.Empty);
                    return true;
                case "PutTextOnClipboard":
                    action = (parameter) => Actions.TextToClipboard(parameter.ToString() ?? string.Empty);
                    return true;
                case "Menu":
                    action = (parameter) => Actions.SelectMenu();
                    return true;
                case "SetConfigPath":
                    action = (parameter) => Actions.SetConfigPath(parameter.ToString() ?? string.Empty);
                    return true;
                case "RemoveFromConfig":
                    action = (parameter) => Actions.RemoveFromConfig(parameter.ToString() ?? string.Empty);
                    return true;
                case "RemoveConfigPath":
                    action = (parameter) => Actions.RemoveConfigPath(parameter.ToString() ?? string.Empty);
                    return true;
                case "AddNote":
                    action = (parameter) => Actions.AddNote(parameter.ToString() ?? string.Empty);
                    return true;
                case "ConfigureNote":
                    action = (parameter) => Actions.ConfigureNote(parameter.ToString() ?? string.Empty);
                    return true;
                case "OpenCMDAtLocation":
                    action = (parameter) => Actions.OpenCMDAtLocation(parameter.ToString() ?? string.Empty);
                    return true;
                case "OpenCMDPersistent":
                    action = (parameter) => Actions.OpenCMDPersistent(parameter.ToString() ?? string.Empty);
                    return true;
                case "QuickNote":
                    action = (parameter) => Actions.QuickNote(parameter.ToString() ?? string.Empty);
                    return true;
                case "ConfigureSuccessSound":
                    action = (parameter) => Actions.ConfigureSuccessSound(parameter.ToString() ?? string.Empty);
                    return true;
                case "ConfigureErrorSound":
                    action = (parameter) => Actions.ConfigureErrorSound(parameter.ToString() ?? string.Empty);
                    return true;
                case "EditConfig":
                    action = (parameter) => Actions.EditConfig(parameter.ToString() ?? string.Empty);
                    return true;
                case "AddQuote":
                    action = (parameter) => Actions.AddQuote(parameter.ToString() ?? string.Empty);
                    return true;
                case "RemoveQuote":
                    action = (parameter) => Actions.RemoveQuote(parameter.ToString() ?? string.Empty);
                    return true;
                case "EditQuote":
                    action = (parameter) => Actions.EditQuote(parameter.ToString() ?? string.Empty);
                    return true;
                case "ToggleQuotes":
                    action = (parameter) => Actions.ToggleQuotes(parameter.ToString() ?? string.Empty);
                    return true;
                case "AddWorkLog":
                    action = (parameter) => Actions.AddWorkLog(parameter.ToString() ?? string.Empty);
                    return true;
                case "ConfigureWorkLog":
                    action = (parameter) => Actions.ConfigureWorkLog(parameter.ToString() ?? string.Empty);
                    return true;
                case "StartTimer":
                    action = (parameter) => Actions.StartTimer(parameter.ToString() ?? string.Empty);
                    return true;
                case "ViewTimerHistory":
                    action = (parameter) => Actions.ViewTimerHistory(parameter.ToString() ?? string.Empty);
                    return true;
                case "ConfigureTimer":
                    action = (parameter) => Actions.ConfigureTimer(parameter.ToString() ?? string.Empty);
                    return true;
                case "ShowHelp":
                    action = (parameter) => Actions.ShowHelp(parameter.ToString() ?? string.Empty);
                    return true;
                case "ShowDocs":
                    action = (parameter) => Actions.ShowDocs(parameter.ToString() ?? string.Empty);
                    return true;
                case "ConfigureCloseTimeout":
                    action = (parameter) => Actions.ConfigureCloseTimeout(parameter.ToString() ?? string.Empty);
                    return true;
                default:
                    action = null;
                    return false;
            }
        }


        public static void LogText(string logValue)
        {

            string timestamp = DateTime.Now.ToString(DATE_TIME_FORMAT);
            string formattedMessage = string.Format("[{0}] {1}", timestamp, logValue);


            string logFilePath = OUTPUT_LOG_FILE_PATH;

            using (StreamWriter logWriter = File.AppendText(logFilePath))
            {
                logWriter.WriteLine(formattedMessage);
                logWriter.Flush();
                logWriter.Close();
            }
        }


    }
}
