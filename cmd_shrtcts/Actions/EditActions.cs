using Newtonsoft.Json;
using Spectre.Console;

namespace cmd_shrtcts;

public static partial class Actions
{
    public static void EditConfig(string unused)
    {
        Console.WriteLine("");

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select config file to edit:")
                .PageSize(20)
                .MoreChoicesText("Move up and down to reveal more choices")
                .AddChoices(Loader.INPUT_CONFIG_LOCATIONS)
        );

        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, selection);
        var fileName = Path.GetFileName(selection);

        if (string.Equals(fileName, "system-config.json", StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine("[red]Cannot edit system-config.json.[/]");
            return;
        }

        if (!File.Exists(configPath))
        {
            AnsiConsole.MarkupLine($"[red]Config file not found:[/] {configPath}");
            return;
        }

        List<Loader.Root> items;
        try
        {
            items = JsonConvert.DeserializeObject<List<Loader.Root>>(File.ReadAllText(configPath))
                    ?? new List<Loader.Root>();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Failed to read config:[/] {ex.Message}");
            return;
        }

        if (items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No entries found in config.[/]");
            return;
        }

        var choices = items.Select((x, idx) => new EntryChoice(idx, x)).ToList();
        var chosen = AnsiConsole.Prompt(
            new SelectionPrompt<EntryChoice>()
                .Title("Select entry to edit:")
                .PageSize(25)
                .MoreChoicesText("Move up and down to reveal more choices")
                .UseConverter(c => c.Display)
                .AddChoices(choices)
        );

        var entry = items[chosen.Index];

        Console.WriteLine("");
        AnsiConsole.MarkupLine("[cyan]Edit each field — press Enter to keep the current value.[/]");
        Console.WriteLine("");

        // Names
        var currentNames = string.Join(", ", entry.AdditionalNames ?? new List<string>());
        var newNamesInput = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Names[/] [grey](comma-separated):[/]")
                .DefaultValue(currentNames)
        );
        entry.AdditionalNames = newNamesInput
            .Split(',')
            .Select(n => n.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        // Action type
        var actionTypes = new[]
        {
            "OpenWebPage", "OpenCMD", "OpenCMDPersistent", "PutTextOnClipboard",
            "OpenFile", "OpenCMDWithParams", "OpenCMDAtLocation", "QuickNote", "AddNote"
        };
        var keepLabel = $"Keep current ({entry.action})";
        var newAction = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green]Action type:[/]")
                .PageSize(12)
                .AddChoices(new[] { keepLabel }.Concat(actionTypes))
        );
        if (newAction != keepLabel)
            entry.action = newAction;

        // Parameter
        var paramPrompt = new TextPrompt<string>("[green]Parameter:[/]").AllowEmpty();
        if (!string.IsNullOrEmpty(entry.parameter))
            paramPrompt = paramPrompt.DefaultValue(entry.parameter);
        entry.parameter = AnsiConsole.Prompt(paramPrompt);

        // Browser (only relevant for OpenWebPage; cleared for other actions)
        if (string.Equals(entry.action, "OpenWebPage", StringComparison.OrdinalIgnoreCase))
            entry.browser = PromptForBrowser(entry.browser);
        else
            entry.browser = null;

        // Description
        var descPrompt = new TextPrompt<string>("[green]Description[/] [grey](optional — shown as help text in menu):[/]").AllowEmpty();
        if (!string.IsNullOrWhiteSpace(entry.description))
            descPrompt = descPrompt.DefaultValue(entry.description);
        var newDesc = AnsiConsole.Prompt(descPrompt);
        entry.description = string.IsNullOrWhiteSpace(newDesc) ? null : newDesc.Trim();

        items[chosen.Index] = entry;

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };
        File.WriteAllText(configPath, JsonConvert.SerializeObject(items, settings));

        AnsiConsole.MarkupLine("[green]Entry updated.[/]");
    }
}
