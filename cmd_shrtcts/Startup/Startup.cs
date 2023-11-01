using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
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

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Loader.ASSEMBLY_LOCATION)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();
            
            _configuration = configuration;

            Loader.LogText("Startup: Loaded AppSettings");

            LoadVariables();
            Loader.LogText("Startup: Loaded Variables");
            LoadActions();
            Loader.LogText("Startup: Loaded Actions");
        }

        public string[]? GetInputConfigsArray()
        {
            Loader.LogText("GetInputConfigsArray");
            return _configuration.GetSection("ApplicationVars:InputConfigs").Get<string[]>();
        }

        public bool LoadVariables()
        {
            Loader.INPUT_CONFIG_LOCATIONS = GetInputConfigsArray() ?? new string[] { @".\Data\Configs\system-config.json" };
            return true;
        }

        internal void LoadActions()
        {
            Loader.actionsDictionary = Loader.LoadActionsDictionary(Loader.INPUT_CONFIG_LOCATIONS);
        }

        internal void ProcessParameter(string value)
        {
            Loader.LogText($"Processing Entered Parameter: {value}");


            if (Loader.actionsDictionary1.TryGetValue(value, out Action<object> action))
            {
                if (!Loader.TryGetParameterFromJson(value, out object parameter) || parameter == "prompt")
                {
                    Loader.LogText("Enter a parameter:");
                    parameter = Console.ReadLine();
                }


                // Invoke desired Action
                action.Invoke(parameter);
                Loader.PlaySound("success");
            }
            else
            {
                Loader.LogText("Invalid value!");
                Loader.PlaySound("error");
            }
        }

        internal void SetSystemVariables()
        {
            Loader.ASSEMBLY_LOCATION = System.Reflection.Assembly.GetExecutingAssembly().Location;
            Loader.ASSEMBLY_LOCATION = Loader.ChangeFromLocalToDirectoryPath(@"..\");
            Loader.OUTPUT_LOG_FILE_PATH = Loader.ChangeFromLocalToDirectoryPath(@".\log.txt");
            Loader.SUCCESS_SOUND_FILE_PATH = Loader.ChangeFromLocalToDirectoryPath(Loader.SUCCESS_SOUND_FILE_PATH);
            Loader.ERROR_SOUND_FILE_PATH = Loader.ChangeFromLocalToDirectoryPath(Loader.ERROR_SOUND_FILE_PATH);

        }


    }
}
