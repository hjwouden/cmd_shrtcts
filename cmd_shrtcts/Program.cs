namespace cmd_shrtcts
{
    internal class Program
    {
        static bool TryGetParameterFromJson(string value, out object parameter)
        {
            if (Loader.actionsDictionary.TryGetValue(value, out Loader.Root result))
            {
                parameter = result.parameter;
                return true;
            }
            parameter = null;
            return false;
        }

        static void Main(string[] args)
        {
            // Load Actions
            Loader.LogText("Test Logging Start");
            Loader.actionsDictionary = Loader.LoadActionsDictionary(Loader.INPUT_CONFIG_LOCATION);

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