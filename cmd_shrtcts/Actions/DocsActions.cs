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
            ("cc note",          "Create a dated note and open it in Notepad/Notepad++/VS Code"),
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
            ("cc cqn",           "Configure quick note folder and editor"),
            ("cc cwl",           "Configure work log file path"),
            ("cc ctimer",        "Configure timer data file path"),
            ("cc addquote",      "Add a quote to the random pool"),
            ("cc togglequotes",  "Enable / disable random quotes"),
            ("cc successsound",  "Set a custom sound played after a successful action"),
            ("cc errorsound",    "Set a custom sound played after a failed action"),
            ("cc closetimeout",  "Set how long a launcher command window waits before closing"),
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
        var topics = new[] { "Getting Started", "Timer", "Notes & Work Log", "Managing Shortcuts", "Quotes", "Sounds & Window Behavior" };
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
            "Sounds & Window Behavior" => SoundsWindowDocs,
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
        "  [bold]Enter[/] = mark the goal done early   [bold]P[/] / [bold]Space[/] = pause or resume   [bold]E[/] / [bold]Esc[/] = stop without finishing",
        "",
        "[bold]When time runs out (or you stop early)[/]",
        "  You are asked for an outcome:",
        "  1. [green]Completed ✓[/]     — goal is logged as done and added to history",
        "  2. [yellow]Need more time[/]  — enter extra minutes, countdown continues",
        "  3. [cyan]Save for later[/]  — goal is saved; resume it the next time you run [cyan]cc timer[/]",
        "",
        "[bold]Multiple saved goals[/]",
        "  You can have more than one goal saved at once. [cyan]cc timer[/] lists them and offers:",
        "  · Pick a saved goal to resume it where you left off",
        "  · [green]+ Start a new goal[/]     — begins another one alongside the saved goals",
        "  · Abandon a saved goal     — logs it as Abandoned and removes it from the list",
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
        "[bold]Three separate features, three separate files — at a glance:[/]",
        "",
        "  [cyan]cc sn[/]    single checkbox line, appended silently, no file opens   → sticky note",
        "  [cyan]cc wl[/]    single timestamped line, appended silently, no file opens → work log",
        "  [cyan]cc note[/]  brand-new dated file created AND opened in an editor      → quick note",
        "",
        "[grey]────────────────────────────────────────────────────[/]",
        "",
        "[bold underline]cc sn — Sticky Note[/]  [grey](aliases: td, todo)[/]",
        "",
        "  Appends a Markdown checkbox item to ONE ongoing notes file:",
        "  [grey]- [[ ]] your text here[/]",
        "",
        "  You are prompted to type your note, then it is appended immediately.",
        "  No file is opened — this is a fast, fire-and-forget capture.",
        "  Type [bold]exit[/] to cancel without writing.",
        "",
        "  Configure the file path:  [cyan]cc cn[/]  (configurenote)",
        "",
        "[grey]────────────────────────────────────────────────────[/]",
        "",
        "[bold underline]cc wl — Work Log[/]  [grey](aliases: worklog)[/]",
        "",
        "  Appends a timestamped entry to a SEPARATE, ongoing work log file:",
        "  [grey]06-23-2026 2:15 PM = your entry here[/]",
        "",
        "  Useful for tracking what you worked on throughout the day — same",
        "  fire-and-forget style as [cyan]cc sn[/], but its own file and timestamped",
        "  instead of a checkbox.",
        "  Type [bold]exit[/] to cancel without writing.",
        "",
        "  Configure the file path:  [cyan]cc cwl[/]  (configureworklog)",
        "",
        "[grey]────────────────────────────────────────────────────[/]",
        "",
        "[bold underline]cc note — Quick Note[/]  [grey](aliases: quicknote, qn)[/]",
        "",
        "  Creates a brand-new file named [grey]quickNote-<date-time>.md[/] in your",
        "  configured folder and opens it right away — handy for meeting notes or",
        "  anything longer than one line, unlike the single silently-appended line",
        "  of [cyan]cc sn[/] or [cyan]cc wl[/].",
        "",
        "  Opens in Notepad by default on Windows; TextEdit on macOS.",
        "  Notepad++ or VS Code can be selected instead — see below.",
        "",
        "  Configure the folder and editor:  [cyan]cc cqn[/]  (configurequicknote)",
        "  On first use (if unconfigured) you'll be prompted for a folder to save into.",
        "",
        "All three files are plain text / Markdown and open in any editor.",
        "None of them is created until you use its command for the first time.",
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

    private static readonly string[] SoundsWindowDocs =
    {
        "[bold]Success / error sounds[/]",
        "  A short sound plays after an action succeeds or fails (chime.wav / chord.wav",
        "  by default).",
        "",
        "  [cyan]cc successsound[/]  — set or clear a custom sound played on success",
        "  [cyan]cc errorsound[/]    — set or clear a custom sound played on failure",
        "  Point either at your own [grey].wav[/] file, or clear it to revert to the bundled default.",
        "",
        "[bold]Launcher window close timeout[/]",
        "  [cyan]OpenCMD[/] / [cyan]OpenPowerShell[/] shortcuts open a window, run the command,",
        "  then auto-close after a short countdown (persistent variants like",
        "  [cyan]OpenCMDPersistent[/] stay open instead).",
        "",
        "  [cyan]cc closetimeout[/]  (aliases: [cyan]windowtimeout[/])",
        "  Set how many seconds that countdown lasts. 0 closes the window immediately.",
    };
}
