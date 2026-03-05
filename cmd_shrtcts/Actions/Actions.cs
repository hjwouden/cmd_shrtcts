using Newtonsoft.Json;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using static cmd_shrtcts.Loader;
using static System.Net.Mime.MediaTypeNames;

namespace cmd_shrtcts
{
    public static partial class Actions
    {
        public static void OpenWebPage(string path)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = Loader.CHROME_BROWSER_PATH;
            startInfo.Arguments = $@"--new-window {path} ";
            process.StartInfo = startInfo;
            process.Start();
        }


        public static string[] getHelpMenuChoices()
        {
            List<string> root = new List<string>();
            List<string> alreadyInserted = new List<string>();

            foreach (var x in Loader.actionsDictionary)
            {
                if (!alreadyInserted.Contains(x.Key))
                {
                    root.Add(x.Key);
                    alreadyInserted.Add(x.Key);
                    foreach (var y in x.Value.AdditionalNames)
                    {
                        if (!alreadyInserted.Contains(y))
                        {
                            //a.AddNode(y);
                            alreadyInserted.Add(y);
                        }
                    }
                }
            }

            return root.ToArray();

        }


        public static void ListActions(string param)
        {
            AnsiConsole.Markup("[underline red]cmd_shrts[/]\n");

            Dictionary<string, string[]> newPairs = new Dictionary<string, string[]>();
            List<string> alreadyInserted = new List<string>();

            var root = new Tree("Root");

            foreach (var x in Loader.actionsDictionary)
            {
                if (!alreadyInserted.Contains(x.Key))
                {
                    var a = root.AddNode(x.Key);
                    alreadyInserted.Add(x.Key);
                    foreach (var y in x.Value.AdditionalNames)
                    {
                        if (!alreadyInserted.Contains(y))
                        {
                            a.AddNode(y);
                            alreadyInserted.Add(y);
                        }
                    }
                }
            }

            AnsiConsole.Write(root);
        }

        public static void SelectMenu()
        {
            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                .Title("Select from Menu:")
                .PageSize(25)
                .MoreChoicesText("Move up and down to reveal more choices")
                .AddChoices(getHelpMenuChoices())
            );

            LogText("Menu Selection: " +  selection);

            OpenCMD("cc " + selection);


        }

        public static void TextToClipboard(string pathToTextFile)
        {
            //string text = File.ReadAllText(pathToTextFile);
            //Thread thread = new Thread(() => Clipboard.SetText(text));
            //thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
            //thread.Start();
            //thread.Join();

        }

        public static void OpenFile(string filePath)
        {
            try
            {
                // Check if the file exists
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Error: File '{filePath}' not found.");
                    return; // Exit the method if the file doesn't exist
                }

                // Use the Process class to start the associated program
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true // Use the shell for proper file association handling
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening file: {ex.Message}");
                // You might want to log the error for debugging
            }
        }


        public static void AddToConfig(string input)
        {

            Console.WriteLine("");

            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                .Title("Select file to save action to:")
                .PageSize(20)
                .MoreChoicesText("Move up and down to reveal more choices")
                .AddChoices(Loader.INPUT_CONFIG_LOCATIONS)
            );

            LogText("Menu Selection: " + selection);

            string absoluteFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, selection);

            string filePath = absoluteFilePath;

            // Get input for the JSON object parameters
            Console.WriteLine("Enter comma-separated additional names (e.g., name1, name2): ");
            string namesInput = Console.ReadLine();
            List<string> additionalNames = namesInput.Split(',').Select(name => name.Trim()).ToList();

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select the Action Type:")
                    .PageSize(10)
                    .MoreChoicesText("Move up and down to reveal more choices")
                    .AddChoices(new[]
                    {
                        "OpenWebPage", "OpenCMD", "PutTextOnClipboard", "OpenFile", "OpenCMDWithParams"
                    })
            );

            string parameter;
            switch (action)
            {
                case "OpenWebPage":
                    Console.WriteLine("Enter URL:");
                    parameter = Console.ReadLine();
                    break;

                case "OpenCMD":
                    Console.WriteLine("Enter the command (e.g., dir, cc menu, etc):");
                    parameter = Console.ReadLine();
                    break;

                case "PutTextOnClipboard":
                    Console.WriteLine("Enter full path to text file:");
                    parameter = Console.ReadLine();
                    break;

                case "OpenFile":
                    Console.WriteLine("Enter full path to file:");
                    parameter = Console.ReadLine();
                    break;

                case "OpenCMDWithParams":
                    Console.WriteLine("Enter the command:");
                    var cmd = Console.ReadLine();
                    Console.WriteLine("Enter the parameter/password:");
                    var param = Console.ReadLine();
                    parameter = $"{cmd},{param}";
                    break;

                default:
                    Console.WriteLine("Enter the parameter:");
                    parameter = Console.ReadLine();
                    break;
            }

            // Create the JSON object using an anonymous type
            var jsonObject = new
            {
                AdditionalNames = additionalNames,
                action = action,
                parameter = parameter
            };

            List<dynamic> jsonObjects = new List<dynamic>();
            if (File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(filePath);
                jsonObjects = JsonConvert.DeserializeObject<List<dynamic>>(fileContent);
            }

            // Add the new JSON object to the list
            jsonObjects.Add(jsonObject);

            // Serialize the updated list to JSON and overwrite the file
            string updatedJson = JsonConvert.SerializeObject(jsonObjects, Formatting.Indented);
            File.WriteAllText(filePath, updatedJson);

            Console.WriteLine("JSON object appended successfully!");
        }


        public static void AddActionToConfig(string configFilePath, string jsonActionToAdd)
        {

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

        public static void OpenCMDWithParams(string cmd, string param)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                RedirectStandardInput = true,
                UseShellExecute = false
            };

            Process cmdProcess = new Process
            {
                StartInfo = startInfo
            };

            cmdProcess.Start();

            // Write the command to the standard input stream
            cmdProcess.StandardInput.WriteLine(cmd);



            // Write the password to the standard input stream
            cmdProcess.StandardInput.WriteLine(param);
            cmdProcess.StandardInput.Flush();

            cmdProcess.WaitForExit();
            cmdProcess.Close();
        }

        public static void PlaySound(string kind)
        {
            string filePath = Loader.ERROR_SOUND_FILE_PATH;
            if (kind == "success")
            {
                filePath = Loader.SUCCESS_SOUND_FILE_PATH;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                return; // Sound file not available, skip silently
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
                Loader.LogText(ex.ToString());
            }


        }


    }
}
