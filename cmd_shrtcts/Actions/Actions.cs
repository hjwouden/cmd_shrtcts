using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace cmd_shrtcts
{
    public static class Actions
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

        public static void ListActions(string param)
        {
            AnsiConsole.Markup("[underline red]cmd_shrts[/]\n");

            Dictionary<string, string[]> newPairs = new Dictionary<string, string[]>();

            var root = new Tree("Root");

            foreach (var x in Loader.actionsDictionary)
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
                Loader.LogText(ex.ToString());
            }


        }


    }
}
