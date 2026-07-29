using Spectre.Console;

namespace cmd_shrtcts;

public static partial class Actions
{
    public static void ShowHelp(string unused)
    {
        Console.WriteLine();
        AnsiConsole.Write(new Rule("[bold blue]cmd_shrtcts[/]").LeftJustified());
        AnsiConsole.MarkupLine("[grey]Command shortcuts for PowerToys Run · Raycast · any launcher[/]");
        Console.WriteLine();

        PrintHelpSection("NAVIGATION", new[]
        {
            ("cc list",          "Show all shortcuts as a tree"),
            ("cc menu",          "Browse shortcuts interactively"),
            ("cc help",          "This help page"),
            ("cc docs",          "Detailed feature guides"),
        });

        PrintHelpSection("DAILY USE", new[]
        {
            ("cc timer",         "Goal-focused countdown timer with history"),
            ("cc timerhistory",  "View completed goals table"),
            ("cc sn",            "Append a quick todo item to your notes file"),
            ("cc wl",            "Append a timestamped work log entry"),
        });

        PrintHelpSection("MANAGE SHORTCUTS", new[]
        {
            ("cc add",           "Add a new shortcut"),
            ("cc edit",          "Edit an existing shortcut"),
            ("cc remove",        "Delete a shortcut"),
            ("cc setconfig",     "Register an additional config file"),
        });

        PrintHelpSection("SETTINGS", new[]
        {
            ("cc cn",            "Configure notes file path"),
            ("cc cwl",           "Configure work log file path"),
            ("cc ctimer",        "Configure timer data file path"),
            ("cc addquote",      "Add a quote to the random pool"),
            ("cc togglequotes",  "Enable / disable random quotes"),
        });

        AnsiConsole.MarkupLine("[grey]Run [bold]cc docs[/] for detailed guides · [bold]cc list[/] for all shortcuts.[/]");
        Console.WriteLine();
    }

    private static void PrintHelpSection(string title, (string command, string description)[] items)
    {
        AnsiConsole.MarkupLine($"[bold yellow]{title}[/]");
        foreach (var (cmd, desc) in items)
            AnsiConsole.MarkupLine($"  [cyan]{cmd,-24}[/]  {desc}");
        Console.WriteLine();
    }

    public static void ShowDocs(string unused)
    {
        Console.WriteLine();
        var topics = new[] { "Getting Started", "Timer", "Notes & Work Log", "Managing Shortcuts", "Quotes" };
        var topic = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Documentation — choose a topic:[/]")
                .PageSize(10)
                .AddChoices(topics)
        );

        Console.WriteLine();
        AnsiConsole.Write(new Rule($"[bold]{Markup.Escape(topic)}[/]").LeftJustified());
        Console.WriteLine();

        var lines = topic switch
        {
            "Getting Started"    => GettingStartedDocs,
            "Timer"              => TimerDocs,
            "Notes & Work Log"   => NotesWorkLogDocs,
            "Managing Shortcuts" => ManagingShortcutsDocs,
            "Quotes"             => QuotesDocs,
            _                    => Array.Empty<string>(),
        };

        foreach (var line in lines)
            AnsiConsole.MarkupLine(line);

        Console.WriteLine();
    }

    private static readonly string[] GettingStartedDocs =
    {
        "[bold]What is cmd_shrtcts?[/]",
        "A global .NET CLI tool for running configurable shortcuts from launchers",
        "like Windows PowerToys Run or macOS Raycast.",
        "",
        "[bold]Invoking shortcuts[/]",
        "  In PowerToys Run: type [cyan]cc[/] then a space then your shortcut name.",
        "  In a terminal:    just type [cyan]cc <shortcut>[/] directly.",
        "",
        "[bold]Key commands[/]",
        "  [cyan]cc list[/]     — see all loaded shortcuts",
        "  [cyan]cc menu[/]     — browse and run shortcuts interactively",
        "  [cyan]cc add[/]      — add a new shortcut via wizard",
        "  [cyan]cc docs[/]     — this guide",
        "",
        "[bold]How shortcuts work[/]",
        "  Each shortcut has one or more aliases, an action type, and a parameter.",
        "  Aliases are what you type; the action defines what happens.",
        "  Available actions: OpenWebPage, OpenCMD, OpenCMDAtLocation, OpenFile,",
        "  AddNote, AddWorkLog, StartTimer, PutTextOnClipboard, and more.",
        "",
        "[bold]Where files live (Windows)[/]",
        "  Settings:   [grey]%LOCALAPPDATA%\\cmd_shrtcts\\appsettings.json[/]",
        "  Data files: [grey]%LOCALAPPDATA%\\cmd_shrtcts\\[/]",
        "",
        "  [bold]Where files live (macOS)[/]",
        "  Settings:   [grey]~/.local/share/cmd_shrtcts/appsettings.json[/]",
        "",
        "Run [cyan]cc docs[/] and pick a topic to learn about specific features.",
    };

    private static readonly string[] TimerDocs =
    {
        "[bold]Starting a timer[/]",
        "  [cyan]cc timer[/]",
        "  You will be asked: [italic]What is your goal?[/]",
        "  Then: [italic]How many minutes to focus?[/]",
        "  The countdown begins immediately in your terminal.",
        "",
        "[bold]During the countdown[/]",
        "  A colour-coded timer counts down in your terminal window:",
        "  [green]Green[/]  → plenty of time remaining",
        "  [yellow]Yellow[/] → under 5 minutes",
        "  [red]Red[/]    → under 1 minute",
        "  Press [bold]Enter[/] at any time to mark the goal done early.",
        "",
        "[bold]When time runs out (or you press Enter)[/]",
        "  You are asked for an outcome:",
        "  1. [green]Completed ✓[/]     — goal is logged as done and added to history",
        "  2. [yellow]Need more time[/]  — enter extra minutes, countdown continues",
        "  3. [cyan]Save for later[/]  — goal is saved; resume it the next time you run [cyan]cc timer[/]",
        "",
        "[bold]Resuming a saved goal[/]",
        "  If you have a saved goal, [cyan]cc timer[/] will show it first and offer:",
        "  · Resume saved goal        — picks up where you left off",
        "  · Abandon it and start new — logs as Abandoned, starts fresh",
        "  · Cancel",
        "",
        "[bold]Viewing your history[/]",
        "  [cyan]cc timerhistory[/]  (alias: [cyan]th[/])",
        "  Shows a table: goal · allocated time · actual time spent · date · status",
        "",
        "[bold]Configuring where history is saved[/]",
        "  [cyan]cc ctimer[/]  — set a custom path for the timer data file",
    };

    private static readonly string[] NotesWorkLogDocs =
    {
        "[bold underline]cc sn — Sticky Note[/]  [grey](aliases: note, AddNote)[/]",
        "",
        "  Appends a Markdown checkbox item to your notes file:",
        "  [grey]- [[ ]] your text here[/]",
        "",
        "  You are prompted to type your note, then it is appended immediately.",
        "  Type [bold]exit[/] to cancel without writing.",
        "",
        "  Configure the file path:  [cyan]cc cn[/]  (configurenote)",
        "",
        "[grey]────────────────────────────────────────────────────[/]",
        "",
        "[bold underline]cc wl — Work Log[/]  [grey](aliases: worklog, AddWorkLog)[/]",
        "",
        "  Appends a timestamped entry to your work log file:",
        "  [grey]06-23-2026 2:15 PM = your entry here[/]",
        "",
        "  Useful for tracking what you worked on throughout the day.",
        "  Type [bold]exit[/] to cancel without writing.",
        "",
        "  Configure the file path:  [cyan]cc cwl[/]  (configureworklog)",
        "",
        "Both files are plain text / Markdown and open in any editor.",
        "Neither file is created until you use the command for the first time.",
    };

    private static readonly string[] ManagingShortcutsDocs =
    {
        "[bold]Adding a shortcut[/]",
        "  [cyan]cc add[/]  (alias: [cyan]new[/])",
        "  Walks you through: pick aliases, choose an action type, set a parameter.",
        "  Saved to your user config file.",
        "",
        "[bold]Editing a shortcut[/]",
        "  [cyan]cc edit[/]",
        "  Pick from your existing shortcuts to update name, action, or parameter.",
        "",
        "[bold]Removing a shortcut[/]",
        "  [cyan]cc remove[/]  (alias: [cyan]delete[/])",
        "  Pick from your existing shortcuts to delete them.",
        "",
        "[bold]Using multiple config files[/]",
        "  Shortcuts can live in separate JSON files — useful for work vs personal,",
        "  or for sharing a config with a team.",
        "",
        "  Register a file:    [cyan]cc setconfig[/]  — enter the full path to a .json file",
        "  Unregister a file:  [cyan]cc removeconfig[/]",
        "",
        "  All registered files are loaded at startup and merged together.",
        "  User shortcuts override system shortcuts with the same alias.",
        "",
        "[bold]Config file location[/]",
        "  Windows: [grey]%LOCALAPPDATA%\\cmd_shrtcts\\appsettings.json[/]",
        "  macOS:   [grey]~/.local/share/cmd_shrtcts/appsettings.json[/]",
        "",
        "  The [grey]ApplicationVars.InputConfigs[/] array in that file lists all loaded config paths.",
    };

    private static readonly string[] QuotesDocs =
    {
        "A random quote can be displayed after each action — a small touch of",
        "personality as you work through your day.",
        "",
        "[bold]Toggle quotes on or off[/]",
        "  [cyan]cc togglequotes[/]",
        "",
        "[bold]Add a quote to your personal pool[/]",
        "  [cyan]cc addquote[/]",
        "  Type the quote when prompted. Saved to your user quotes file.",
        "",
        "[bold]Edit or remove quotes[/]",
        "  [cyan]cc editquote[/]    — pick and update a quote",
        "  [cyan]cc removequote[/]  — pick and delete a quote",
        "",
        "[bold]How quotes are sourced[/]",
        "  If you have added any personal quotes, your quotes file is used.",
        "  Otherwise the bundled movie-quotes.txt file is used as a fallback.",
        "",
        "  Your quotes file: [grey]%LOCALAPPDATA%\\cmd_shrtcts\\quotes.txt[/]",
        "",
        "Quotes appear after the action completes, in a muted style.",
        "They do not appear if quotes are disabled via [cyan]cc togglequotes[/].",
    };
}
