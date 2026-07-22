using Microsoft.Extensions.Configuration;
using Spectre.Console;


namespace cmd_shrtcts
{
    internal class Program
    {
        static void Main(string[] args)
        {
            AnsiConsole.Markup("[underline red]cmd_shrts[/]\n");
            //AnsiConsole.Write(new FigletText("CMD SHRTCTS").LeftJustified().Color(Color.Red));
            try
            {
                Startup startApp = new Startup(args);
                
                // Check for provided action in list
                if (args.Length > 0)
                {
                    // Join all arguments with spaces to support multi-word commands
                    string fullCommand = string.Join(" ", args);
                    startApp.ProcessParameter(fullCommand);
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
                throw;
            }
            
        }
    }
}