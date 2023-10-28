using Microsoft.Extensions.Configuration;

namespace cmd_shrtcts
{
    internal class Program
    {
        IConfiguration Configuration;

        public Program(IConfiguration configuration)
        {
            Configuration = configuration;
        }


        static bool TryGetParameterFromJson(string value, out object parameter)
        {
            if (!Loader.actionsDictionary.TryGetValue(value, out Loader.Root result))
            {
                parameter = null;
                return false;
            }
            parameter = result.parameter;
            return true;
        }

        static void Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            Loader.LogText("Test Config Load");
            List<string> testVal = configuration.GetSection("Test:TestList").Get<List<string>>();
            Loader.LogText(testVal.ToString());

            

            // Load Actions
            Loader.LogText("Test Logging Start");
            //Loader.setConfigurations();
            Loader.actionsDictionary = Loader.LoadActionsDictionary(Loader.INPUT_CONFIG_LOCATIONS);

            // Check for provided action in list
            if (args.Length > 0)
            {
                string value = args[0];
                Loader.LogText($"Processing Entered Parameter: {value}");



                if (Loader.actionsDictionary1.TryGetValue(value, out Action<object> action))
                {
                    if (!TryGetParameterFromJson(value, out object parameter) || parameter == "prompt")
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
}