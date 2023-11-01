using Microsoft.Extensions.Configuration;
using Spectre.Console;


namespace cmd_shrtcts
{
    internal class Program
    {
        static void Main(string[] args)
        {
            AnsiConsole.Markup("[underline red]cmd_shrts[/]\n");
           // AnsiConsole.Markup("Assembly: " + System.Reflection.Assembly.GetExecutingAssembly().Location);
            try
            {
                Startup startApp = new Startup(args);
                
                // Check for provided action in list
                if (args.Length > 0)
                {
                    startApp.ProcessParameter(args[0]);
                }
                else
                {
                    Loader.LogText("No Provided Action Parameter");
                }
                Loader.LogText("Done");
            }
            catch (Exception ex)
            {
                Loader.LogText(ex.ToString());
                throw ex;
            }
            
        }
    }
}