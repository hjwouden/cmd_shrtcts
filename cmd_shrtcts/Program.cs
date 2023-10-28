using Microsoft.Extensions.Configuration;

namespace cmd_shrtcts
{
    internal class Program
    {



        static void Main(string[] args)
        {
            Startup startApp = new Startup(args);
            startApp.LoadVariables();
            startApp.LoadActions();
            
            // Check for provided action in list
            if (args.Length > 0)
            {
                startApp.ProcessParameter(args[0]);

                
            }
        }
    }
}