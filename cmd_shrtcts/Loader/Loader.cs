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
        public static string SUCCESS_SOUND_FILE_PATH = @".\Data\Sounds\chime.wav";
        public static string ERROR_SOUND_FILE_PATH = @".\Data\Sounds\chord.wav";
        public static string CHROME_BROWSER_PATH = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
        public static string OUTPUT_LOG_FILE_PATH = @".\log.txt";
        public static string[] INPUT_CONFIG_LOCATIONS =
            {
                @".\Data\Configs\system-config.json"
            };

        //SHARED OBJECTS
        public static Dictionary<string, Action<object>>? actionsDictionary1;
        public static Dictionary<string, Root>? actionsDictionary;

        public class Root
        {
            public List<string>? AdditionalNames { get; set; }
            public string? action { get; set; }
            public string? parameter { get; set; }
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
            string newPath = new DirectoryInfo(Path.Combine(Loader.ASSEMBLY_LOCATION, fileName)).FullName;
            return newPath;
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
                    if (!File.Exists(useThisConfigFilePath))
                    {
                        throw new Exception(message: "File Not Found");
                    }
                }

                if (File.Exists(useThisConfigFilePath))
                {
                    string json = File.ReadAllText(useThisConfigFilePath);
                    List<Root> config = JsonConvert.DeserializeObject<List<Root>>(json);

                    if (config != null && config.Count > 0)
                    {
                        foreach (Root a in config)
                        {
                            foreach (string b in a.AdditionalNames)
                            {
                                if (TryGetActionDelegate(a.action, out Action<object> action))
                                {
                                    actionsDictionary1[b] = action;
                                    actions[b] = new Root { AdditionalNames = a.AdditionalNames, action = a.action, parameter = a.parameter };
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
                else
                {
                    LogText("Configuration file not found: " + useThisConfigFilePath);
                    Actions.PlaySound("error");
                }
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
                case "list":
                    action = (parameter) => Actions.ListActions(parameter.ToString());
                    return true;
                case "PutTextOnClipboard":
                    action = (parameter) => Actions.TextToClipboard(parameter.ToString());
                    return true;
                case "Menu":
                    action = (parameter) => Actions.SelectMenu();
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
