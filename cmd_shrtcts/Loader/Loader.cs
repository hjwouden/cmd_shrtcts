using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;

namespace cmd_shrtcts
{
    public class Loader
    {
        //CONFIGURATION VALUES
        public static string ASSEMBLY_LOCATION = "";
        public static string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
        public static string SUCCESS_SOUND_FILE_PATH = Path.Combine("Data", "Sounds", "chime.wav");
        public static string ERROR_SOUND_FILE_PATH = Path.Combine("Data", "Sounds", "chord.wav");
        public static string CHROME_BROWSER_PATH = OperatingSystem.IsWindows() 
            ? "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe" 
            : "google-chrome"; // Fallback for Linux/Mac, though 'open' is preferred on Mac
        public static string OUTPUT_LOG_FILE_PATH = "log.txt";
        public static int COMMAND_WINDOW_TIMEOUT_SECONDS = 5; // How long the command window stays open (in seconds)
        public static string[] INPUT_CONFIG_LOCATIONS =
            {
                Path.Combine("Data", "Configs", "system-config.json")
            };

        public static string GetUserDataDirectory()
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(baseDir, "cmd_shrtcts");
        }

        public static string GetUserAppSettingsPath()
        {
            return Path.Combine(GetUserDataDirectory(), "appsettings.json");
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
            public string? category { get; set; }
            public string? description { get; set; }
        }

        public static bool TryGetParameterFromJson(string value, out object parameter)
        {
            if(Loader.actionsDictionary != null)
            {
                if (Loader.actionsDictionary.TryGetValue(value, out Loader.Root result))
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
                    List<Root> config = JsonConvert.DeserializeObject<List<Root>>(json);

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
                                if (TryGetActionDelegate(a.action, out Action<object> action))
                                {
                                    // Store keys in lowercase for case-insensitive lookup
                                    string lowerKey = b.ToLowerInvariant();
                                    actionsDictionary1[lowerKey] = action;
                                    actions[lowerKey] = new Root { AdditionalNames = a.AdditionalNames, action = a.action, parameter = a.parameter, category = a.category, description = a.description };
                                }
                                else
                                {
                                    LogText("Invalid action configuration: " + b);
                                    Actions.PlaySound("error");
                                }
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

        public static bool TryGetActionDelegate(string actionName, out Action<object> action)
        {
            switch (actionName)
            {
                case "OpenWebPage":
                    action = (parameter) => Actions.OpenWebPage(parameter.ToString());
                    return true;
                case "AddToConfig":
                    action = (parameter) => Actions.AddToConfig(parameter.ToString());
                    return true;
                case "OpenFile":
                    action = (parameter) => Actions.OpenFile(parameter.ToString());
                    return true;
                case "OpenCMD":
                    action = (parameter) => Actions.OpenCMD(parameter.ToString());
                    return true;
                case "OpenCMDWithParams":
                    action = (parameter) => Actions.OpenCMDWithParams(parameter.ToString().Split(',')[0], parameter.ToString().Split(',')[1]);
                    return true;
                case "list":
                    action = (parameter) => Actions.ListActions(parameter.ToString());
                    return true;
                case "PutTextOnClipboard":
                    action = (parameter) => Actions.TextToClipboard(parameter.ToString());
                    return true;
                case "Menu":
                    action = (parameter) => Actions.SelectMenu();
                    return true;
                case "SetConfigPath":
                    action = (parameter) => Actions.SetConfigPath(parameter.ToString());
                    return true;
                case "RemoveFromConfig":
                    action = (parameter) => Actions.RemoveFromConfig(parameter.ToString());
                    return true;
                case "RemoveConfigPath":
                    action = (parameter) => Actions.RemoveConfigPath(parameter.ToString());
                    return true;
                case "AddNote":
                    action = (parameter) => Actions.AddNote(parameter.ToString());
                    return true;
                case "ConfigureNote":
                    action = (parameter) => Actions.ConfigureNote(parameter.ToString());
                    return true;
                case "OpenCMDAtLocation":
                    action = (parameter) => Actions.OpenCMDAtLocation(parameter.ToString());
                    return true;
                case "OpenCMDPersistent":
                    action = (parameter) => Actions.OpenCMDPersistent(parameter.ToString());
                    return true;
                case "QuickNote":
                    action = (parameter) => Actions.QuickNote(parameter.ToString());
                    return true;
                case "ConfigureSuccessSound":
                    action = (parameter) => Actions.ConfigureSuccessSound(parameter.ToString());
                    return true;
                case "ConfigureErrorSound":
                    action = (parameter) => Actions.ConfigureErrorSound(parameter.ToString());
                    return true;
                case "EditConfig":
                    action = (parameter) => Actions.EditConfig(parameter.ToString());
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
