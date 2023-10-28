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

        public string[] GetStringArray()
        {
            string[]? tsettings = _configuration.GetSection("Test:TestList").Get<string[]>();
            if(tsettings == null)
            {
                return new string[] { "", ""};
            }
            else
            {
                return tsettings;
            }

            
        }

        public bool LoadVariables()
        {
            Loader.INPUT_CONFIG_LOCATIONS = GetStringArray();
            return true;
        }
    }
}
