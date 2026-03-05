using Newtonsoft.Json;
using Spectre.Console;

namespace cmd_shrtcts;

public static partial class Actions
{
    public static void RemoveFromConfig(string input)
    {
        Console.WriteLine("");

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select file to remove action from:")
                .PageSize(20)
                .MoreChoicesText("Move up and down to reveal more choices")
                .AddChoices(Loader.INPUT_CONFIG_LOCATIONS)
        );

        var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, selection);

        if (string.Equals(Path.GetFileName(selection), "system-config.json", StringComparison.OrdinalIgnoreCase) ||
            selection.EndsWith("Data\\Configs\\system-config.json", StringComparison.OrdinalIgnoreCase) ||
            selection.EndsWith("Data/Configs/system-config.json", StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine("[red]Refusing to edit system-config.json.[/]");
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
            items = JsonConvert.DeserializeObject<List<Loader.Root>>(File.ReadAllText(configPath)) ?? new List<Loader.Root>();
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

        var choices = items
            .Select((x, idx) => new EntryChoice(idx, x))
            .ToList();

        var chosen = AnsiConsole.Prompt(
            new SelectionPrompt<EntryChoice>()
                .Title("Select entry to delete:")
                .PageSize(25)
                .MoreChoicesText("Move up and down to reveal more choices")
                .UseConverter(c => c.Display)
                .AddChoices(choices)
        );

        var confirm = AnsiConsole.Confirm($"Delete: {chosen.Display} ?", defaultValue: false);
        if (!confirm)
        {
            AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
            return;
        }

        items.RemoveAt(chosen.Index);

        var updatedJson = JsonConvert.SerializeObject(items, Formatting.Indented);
        File.WriteAllText(configPath, updatedJson);

        AnsiConsole.MarkupLine("[green]Entry deleted.[/]");
    }

    private sealed record EntryChoice(int Index, Loader.Root Item)
    {
        public string Display
        {
            get
            {
                var names = Item.AdditionalNames ?? new List<string>();
                var head = names.Count > 0 ? names[0] : "(no name)";
                var action = Item.action ?? "(no action)";
                return $"{head}  [grey]({action})[/]";
            }
        }
    }
}
