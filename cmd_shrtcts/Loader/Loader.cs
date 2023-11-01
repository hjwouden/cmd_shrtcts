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
        public static string DATE_TIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
        

        public static string OUTPUT_LOG_FILE_PATH = @".\log.txt";
        public static string[] INPUT_CONFIG_LOCATIONS = 
            {  
                @".\Data\Configs\system-config.json"
            };
        public static string SUCCESS_SOUND_FILE_PATH = @".\Data\Sounds\chime.wav";
        public static string ERROR_SOUND_FILE_PATH = @".\Data\Sounds\chord.wav";
        public static string CHROME_BROWSER_PATH = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
        public static string ASSEMBLY_LOCATION = "";

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
            if (!Loader.actionsDictionary.TryGetValue(value, out Loader.Root result))
            {
                parameter = null;
                return false;
            }
            parameter = result.parameter;
            return true;
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
                                    PlaySound("error");
                                }
                            }
                        }
                    }
                }
                else
                {
                    LogText("Configuration file not found: " + useThisConfigFilePath);
                    PlaySound("error");
                }
            }

            return actions;
        }

        public static bool TryGetActionDelegate(string actionName, out Action<object> action)
        {
            switch (actionName)
            {
                case "OpenWebPage":
                    action = (parameter) => OpenWebPage(parameter.ToString());
                    return true;
                case "AddToConfig":
                    action = (parameter) => AddToConfig(parameter.ToString());
                    return true;
                case "OpenFile":
                    action = (parameter) => OpenFile(parameter.ToString());
                    return true;
                case "OpenCMD":
                    action = (parameter) => OpenCMD(parameter.ToString());
                    return true;
                case "list":
                    action = (parameter) => ListActions(parameter.ToString());
                    return true;
                case "PutTextOnClipboard":
                    action = (parameter) => TextToClipboard(parameter.ToString());
                    return true;
                default:
                    action = null;
                    return false;
            }
        }

        public static void ListActions(string param)
        {
            AnsiConsole.Markup("[underline red]cmd_shrts[/]\n");

            Dictionary<string, string[]> newPairs = new Dictionary<string, string[]>();

            var root = new Tree("Root");

            foreach (var x in actionsDictionary)
            {
                var a = root.AddNode(x.Key);
                foreach (var y in x.Value.AdditionalNames)
                {
                    a.AddNode(y);
                }
            }

            AnsiConsole.Write(root);
        }


        public static void TextToClipboard(string pathToTextFile)
        {
            //string text = File.ReadAllText(pathToTextFile);
            //Thread thread = new Thread(() => Clipboard.SetText(text));
            //thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
            //thread.Start();
            //thread.Join();

        }

        public static void OpenWebPage(string path)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = CHROME_BROWSER_PATH;
            startInfo.Arguments = $@"--new-window {path} ";
            process.StartInfo = startInfo;
            process.Start();
        }

        public static void OpenFile(string path)
        {
            Console.WriteLine("Open File");
        }

        public static void AddToConfig(string test)
        {
            //open the config file, append to it, close it
            //Console.WriteLine("Enter the new Action Name");
            //string action = Console.ReadLine();
            //Console.WriteLine("Enter the new Action Shortcut");
            //string shortcut = Console.ReadLine();
            //Console.WriteLine("Enter the link");
            //string link = Console.ReadLine();

            //if (File.Exists(Loader.INPUT_CONFIG_LOCATIONS))
            //{
                //Future - Allow additions to the config file
            //}
        }

        /// <summary>
        /// Opens a Command Prompt and Keeps it open
        /// </summary>
        /// <param name="cmd"></param>
        public static void OpenCMD(string cmd)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.Arguments = $"/K {cmd}";
            process.Start();
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

        public static void PlaySound(string kind)
        {
            string filePath = Loader.ERROR_SOUND_FILE_PATH;
            if (kind == "success")
            {
                filePath = Loader.SUCCESS_SOUND_FILE_PATH;
            }

            try
            {
                SoundPlayer player = new SoundPlayer(filePath);

                // Play the sound
                player.Play();
                TimeSpan waitTime = TimeSpan.FromSeconds(2);
                Thread.Sleep(waitTime);
            }
            catch (Exception ex)
            {
                LogText(ex.ToString());
            }
            

        }
    }
}
