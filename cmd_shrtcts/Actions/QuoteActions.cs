using Newtonsoft.Json;
using Spectre.Console;

namespace cmd_shrtcts;

public static partial class Actions
{
    internal const string QuotesEnabledKey = "QuotesEnabled";

    public static void AddQuote(string unused)
    {
        Console.WriteLine("");
        var text = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]New quote:[/]")
                .ValidationErrorMessage("[red]Quote cannot be empty.[/]")
        );
        var path = EnsureUserQuotesFile();
        File.AppendAllText(path, Environment.NewLine + text.Trim());
        AnsiConsole.MarkupLine("[green]Quote added.[/]");
    }

    public static void RemoveQuote(string unused)
    {
        var path = EnsureUserQuotesFile();
        var quotes = LoadQuoteLines(path);
        if (quotes.Count == 0) { AnsiConsole.MarkupLine("[yellow]No quotes found.[/]"); return; }

        Console.WriteLine("");
        var chosen = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select quote to [red]remove[/]:")
                .PageSize(20)
                .MoreChoicesText("Move up/down for more")
                .AddChoices(quotes.Select(Markup.Escape))
        );
        // chosen is the escaped display text; match back to original
        var original = quotes.FirstOrDefault(q => Markup.Escape(q) == chosen) ?? chosen;
        quotes.Remove(original);
        File.WriteAllLines(path, quotes);
        AnsiConsole.MarkupLine("[green]Quote removed.[/]");
    }

    public static void EditQuote(string unused)
    {
        var path = EnsureUserQuotesFile();
        var quotes = LoadQuoteLines(path);
        if (quotes.Count == 0) { AnsiConsole.MarkupLine("[yellow]No quotes found.[/]"); return; }

        Console.WriteLine("");
        var chosen = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select quote to [cyan]edit[/]:")
                .PageSize(20)
                .MoreChoicesText("Move up/down for more")
                .AddChoices(quotes.Select(Markup.Escape))
        );
        var original = quotes.FirstOrDefault(q => Markup.Escape(q) == chosen) ?? chosen;
        var idx = quotes.IndexOf(original);

        Console.WriteLine("");
        AnsiConsole.MarkupLine($"[grey]Current:[/] {Markup.Escape(original)}");
        Console.WriteLine("");

        var updated = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]New text[/] [grey](leave blank to keep current):[/]")
                .AllowEmpty()
        );
        if (string.IsNullOrWhiteSpace(updated))
        {
            AnsiConsole.MarkupLine("[grey]No changes made.[/]");
            return;
        }
        quotes[idx] = updated.Trim();
        File.WriteAllLines(path, quotes);
        AnsiConsole.MarkupLine("[green]Quote updated.[/]");
    }

    public static void ToggleQuotes(string unused)
    {
        var newValue = !Loader.QUOTES_ENABLED;
        SaveUserBoolSetting(QuotesEnabledKey, newValue, Loader.GetUserAppSettingsPath());
        Loader.QUOTES_ENABLED = newValue;
        var status = newValue ? "[green]enabled[/]" : "[yellow]disabled[/]";
        AnsiConsole.MarkupLine($"Random quotes are now {status}.");
    }

    // Returns the path to the user quotes file, creating it (from bundled) if it doesn't exist yet.
    private static string EnsureUserQuotesFile()
    {
        var userPath = Loader.GetUserQuotesPath();
        if (!File.Exists(userPath))
        {
            var bundled = Loader.ChangeFromLocalToDirectoryPath(
                Path.Combine("Data", "Quotes", "movie-quotes.txt"));
            File.WriteAllText(userPath, File.Exists(bundled) ? File.ReadAllText(bundled) : "");
        }
        return userPath;
    }

    private static List<string> LoadQuoteLines(string path)
        => File.ReadAllLines(path)
               .Select(l => l.Trim())
               .Where(l => !string.IsNullOrWhiteSpace(l))
               .ToList();

    private static void SaveUserBoolSetting(string key, bool value, string userSettingsPath)
    {
        try
        {
            Loader.EnsureUserAppSettingsExists();
            var config = new Dictionary<string, object>();
            if (File.Exists(userSettingsPath))
                config = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    File.ReadAllText(userSettingsPath)) ?? new();
            config[key] = value;
            File.WriteAllText(userSettingsPath,
                JsonConvert.SerializeObject(config, Formatting.Indented));
        }
        catch (Exception ex) { Loader.LogText($"Error saving setting {key}: {ex}"); }
    }
}
