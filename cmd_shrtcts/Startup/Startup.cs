using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cmd_shrtcts
{
    public class Startup
    {
        private IConfiguration _configuration;

        public Startup(string[] args)
        {
            SetSystemVariables();

            Loader.LogText("Startup: Loading AppSettings...");

            Loader.EnsureUserAppSettingsExists();

            var userAppSettingsPath = Loader.GetUserAppSettingsPath();
            var packagedAppSettingsPath = Loader.GetPackagedAppSettingsPath();

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Loader.ASSEMBLY_LOCATION)
                .AddJsonFile(userAppSettingsPath, optional: true, reloadOnChange: true)
                .AddJsonFile(packagedAppSettingsPath, optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();
            
            _configuration = configuration;

            Loader.LogText("Startup: Loaded AppSettings");

            LoadVariables();
            LoadActions();
        }

        public string[]? GetInputConfigsArray()
        {
            Loader.LogText("GetInputConfigsArray");
            return _configuration.GetSection("ApplicationVars:InputConfigs").Get<string[]>();
        }

        public bool LoadVariables()
        {
            Loader.LogText("Startup: Loaded Variables");
            Loader.INPUT_CONFIG_LOCATIONS = GetInputConfigsArray() ?? new string[] { @".\Data\Configs\system-config.json" };
            LoadCustomSoundPaths();
            return true;
        }

        private void LoadCustomSoundPaths()
        {
            var userSettings = Loader.GetUserAppSettingsPath();
            if (!File.Exists(userSettings)) return;

            try
            {
                var json = File.ReadAllText(userSettings);
                var config = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                if (config == null) return;

                if (config.TryGetValue(Actions.SuccessSoundKey, out var successVal))
                {
                    var path = successVal?.ToString();
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                        Loader.SUCCESS_SOUND_FILE_PATH = path;
                }

                if (config.TryGetValue(Actions.ErrorSoundKey, out var errorVal))
                {
                    var path = errorVal?.ToString();
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                        Loader.ERROR_SOUND_FILE_PATH = path;
                }

                if (config.TryGetValue(Actions.QuotesEnabledKey, out var quotesVal)
                    && bool.TryParse(quotesVal?.ToString(), out var quotesEnabled))
                {
                    Loader.QUOTES_ENABLED = quotesEnabled;
                }

                if (config.TryGetValue(Actions.CloseTimeoutKey, out var timeoutVal)
                    && int.TryParse(timeoutVal?.ToString(), out var timeoutSecs)
                    && timeoutSecs >= 0)
                {
                    Loader.COMMAND_WINDOW_TIMEOUT_SECONDS = timeoutSecs;
                }
            }
            catch (Exception ex)
            {
                Loader.LogText($"Error loading custom sound paths: {ex.Message}");
            }
        }

        internal void LoadActions()
        {
            Loader.LogText("Startup: Loaded Actions");
            Loader.actionsDictionary = Loader.LoadActionsDictionary(Loader.INPUT_CONFIG_LOCATIONS);
        }

        internal void ProcessParameter(string value)
        {
            Loader.LogText($"Processing Entered Parameter: {value}");

            // Normalize input to lowercase for case-insensitive lookup
            string normalizedValue = value.ToLowerInvariant();

            if (Loader.actionsDictionary1?.TryGetValue(normalizedValue, out Action<object>? action) == true)
            {
                if (!Loader.TryGetParameterFromJson(normalizedValue, out object? parameter) || (parameter as string) == "prompt")
                {
                    Loader.LogText("Enter a parameter:");
                    parameter = Console.ReadLine() ?? string.Empty;
                }

                // Invoke desired Action
                action!.Invoke(parameter ?? string.Empty);
                Actions.PlaySound("success");
                Actions.DisplayRandomQuote();

                // Show countdown and close the terminal window when running interactively.
                // Skip for "system" category actions (config wizards, menus) — those are
                // run from the user's own terminal, not a launcher-opened one — and for actions
                // that open their own persistent window (closing the front window would close it).
                Loader.Root? root = null;
                Loader.actionsDictionary?.TryGetValue(normalizedValue, out root);
                bool isSystemAction = string.Equals(root?.category, "system", StringComparison.OrdinalIgnoreCase);
                bool opensOwnWindow = root?.action is "OpenCMDPersistent" or "OpenCMDAtLocation";

                if (!Console.IsOutputRedirected && !isSystemAction && !opensOwnWindow)
                    Actions.ShowLauncherCountdownAndClose();
            }
            else
            {
                Loader.LogText("Invalid value!");
                Actions.PlaySound("error");
            }
        }

        internal void SetSystemVariables()
        {
            var dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            Loader.ASSEMBLY_LOCATION = Path.GetDirectoryName(dllPath) ?? AppContext.BaseDirectory;
            
            Loader.OUTPUT_LOG_FILE_PATH = Path.Combine(Loader.ASSEMBLY_LOCATION, "log.txt");
            
            var successSoundPath = Path.Combine(Loader.ASSEMBLY_LOCATION, Loader.SUCCESS_SOUND_FILE_PATH ?? string.Empty);
            var errorSoundPath = Path.Combine(Loader.ASSEMBLY_LOCATION, Loader.ERROR_SOUND_FILE_PATH ?? string.Empty);
            
            Loader.SUCCESS_SOUND_FILE_PATH = File.Exists(successSoundPath) ? successSoundPath : null;
            Loader.ERROR_SOUND_FILE_PATH = File.Exists(errorSoundPath) ? errorSoundPath : null;
        }


    }
}
