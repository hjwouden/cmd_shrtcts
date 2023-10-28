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
            Loader.LogText("Startup: Loading AppSettings...");

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();
            
            _configuration = configuration;

            Loader.LogText("Startup: Loaded AppSettings");
        }

        public string[]? GetInputConfigsArray()
        {
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
    }
}
