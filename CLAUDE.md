# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this project is

`cmd_shrtcts` is a .NET 9.0 CLI tool (packaged as a global dotnet tool) that lets users run configurable shortcuts from a launcher like Windows PowerToys Run or macOS Raycast. The command name is `cc` on Windows and `sc` on macOS (to avoid conflicts with platform-native commands).

## Build & Run

```bash
# Build
dotnet build cmd_shrtcts/cmd_shrtcts.sln

# Run tests
dotnet test cmd_shrtcts.Tests/cmd_shrtcts.Tests.csproj

# Run a single test class
dotnet test cmd_shrtcts.Tests/cmd_shrtcts.Tests.csproj --filter "FullyQualifiedName~LoaderTests"

# Pack and install as global tool (first time)
dotnet pack cmd_shrtcts/cmd_shrtcts.csproj -o ./nupkg
dotnet tool install --global --add-source ./nupkg cmd_shrtcts

# After install, invoke from anywhere
cc help      # Windows
sc help      # macOS
```

On Windows, building in Release mode triggers `Properties/post-build.bat` automatically, which re-packs and updates the global `cc` tool. This increments a dev version counter stored in `.toolversion.counter`.

## Architecture

### Execution flow

```
Program.Main(args)
  └─ Startup(args)           // loads config, builds action dictionary
       └─ ProcessParameter   // looks up command in dictionary, invokes action
```

**`Loader`** is a static class that acts as the shared state container:
- `ASSEMBLY_LOCATION`, `INPUT_CONFIG_LOCATIONS`, and sound/log paths are static fields set at startup.
- `actionsDictionary` (keyed `string → Root`) and `actionsDictionary1` (keyed `string → Action<object>`) are both populated from JSON config files. All keys are stored lowercase for case-insensitive lookup.
- `TryGetActionDelegate` is the registry that maps action type name strings (e.g. `"OpenWebPage"`) to delegates. **Adding a new action type requires a new case here.**

**`Actions`** is a `static partial` class split across nine files:
- `Actions.cs` — core actions: `OpenWebPage`, `OpenCMD`, `OpenCMDAtLocation`, `OpenCMDPersistent`, `OpenCMDWithParams`, `OpenPowerShell`, `OpenPowerShellAtLocation`, `OpenPowerShellPersistent`, `OpenPowerShellWithParams`, `OpenFile`, `TextToClipboard`, `AddToConfig`, `ListActions`, `SelectMenu`, `PlaySound`, `DisplayRandomQuote`, `QuickNote`
- `ConfigActions.cs` — `RemoveFromConfig`, `RemoveConfigPath`
- `NoteActions.cs` — `AddNote`, `ConfigureNote` (the "sticky note" / todo-item feature — see note below), `ConfigureQuickNote`
- `AppSettingsActions.cs` — `SetConfigPath`
- `EditActions.cs` — `EditConfig` (interactive wizard to edit an existing shortcut entry)
- `QuoteActions.cs` — `AddQuote`, `RemoveQuote`, `EditQuote`, `ToggleQuotes` (the random post-action quote pool)
- `SoundActions.cs` — `ConfigureSuccessSound`, `ConfigureErrorSound`
- `TimerActions.cs` — `StartTimer`, `ViewTimerHistory`, `ConfigureTimer` (focus-goal countdown timer with saved/resumable goals and history)
- `WorkLogActions.cs` — `AddWorkLog`, `ConfigureWorkLog`
- `WindowActions.cs` — `ConfigureCloseTimeout` (how long an `OpenCMD`/`OpenPowerShell` launcher window stays open before auto-closing)
- `DocsActions.cs` — `ShowHelp`, `ShowDocs` (the tool's own in-app `cc help` / `cc docs` reference — keep this in sync whenever an action type or built-in shortcut is added or changed)

**Note-like features are easy to conflate — they are three distinct, independently-configured things:**
- `AddNote` (`cc sn`, aliases `td`/`todo`) — appends a single Markdown checkbox line (`- [ ] ...`) to one configured "sticky note" file. No file is opened; it's a fire-and-forget capture. Configure the path with `ConfigureNote` (`cc cn`).
- `AddWorkLog` (`cc wl`, alias `worklog`) — appends a single timestamped line (`MM-dd-yyyy h:mm tt = ...`) to a separate work-log file, for a running log of what you did when. Configure the path with `ConfigureWorkLog` (`cc cwl`).
- `QuickNote` (`cc note`, aliases `quicknote`/`qn`) — creates a brand-new dated `.md` file each time (`quickNote-<date-time>.md`) in a configured folder and opens it in an editor (Notepad/Notepad++/VS Code on Windows, TextEdit on macOS), for longer free-form notes. Configure the folder/editor with `ConfigureQuickNote` (`cc cqn`).

### Configuration

Shortcuts are defined in JSON config files as arrays of objects:
```json
[
  {
    "AdditionalNames": ["shortcut-alias", "another-alias"],
    "action": "OpenWebPage",
    "parameter": "https://example.com"
  }
]
```

Config is loaded from two sources (user config takes precedence):
- **User config:** `%LOCALAPPDATA%\cmd_shrtcts\appsettings.json` (Windows) / `~/.local/share/cmd_shrtcts/appsettings.json` (Mac)
- **Bundled system config:** `Data/Configs/system-config.json` (ships with the tool, contains built-in commands like `help`, `list`, `menu`, `add`)

The `appsettings.json` `ApplicationVars.InputConfigs` array lists additional user config JSON file paths to load.

If `parameter` is `"prompt"` in a config entry, the tool will ask for user input at runtime.

### Available action types

| Action name | What it does |
|---|---|
| `OpenWebPage` | Opens URL in Chrome (Windows) or via `open`/`xdg-open` |
| `OpenCMD` | Runs command in a new cmd window, auto-closes after timeout |
| `OpenCMDPersistent` | Runs command in a new cmd window, stays open (`/K`) |
| `OpenCMDWithParams` | Pipes a command + second input (e.g. password) via stdin |
| `OpenCMDAtLocation` | Opens a terminal window at a specified directory |
| `OpenPowerShell` | Runs command in a new PowerShell window, auto-closes after timeout |
| `OpenPowerShellPersistent` | Runs command in a new PowerShell window, stays open (`-NoExit`) |
| `OpenPowerShellWithParams` | Pipes a command + second input (e.g. password) via stdin, in PowerShell |
| `OpenPowerShellAtLocation` | Opens a PowerShell window at a specified directory |
| `OpenFile` | Opens a file with its default associated application |
| `PutTextOnClipboard` | Copies contents of a text file to clipboard |
| `AddToConfig` | Interactive wizard to add a new shortcut |
| `RemoveFromConfig` | Interactive wizard to remove a shortcut |
| `SetConfigPath` | Adds a config JSON file path to user appsettings |
| `RemoveConfigPath` | Removes a config JSON file path from user appsettings |
| `AddNote` | Appends a markdown todo item to a notes file |
| `ConfigureNote` | Sets/clears the notes file path in user appsettings |
| `QuickNote` | Creates a timestamped `.md` file and opens it in the configured editor (Notepad/Notepad++/VS Code on Windows, TextEdit on macOS) |
| `ConfigureQuickNote` | Sets/clears the quick-note folder and picks the Windows editor (Notepad/Notepad++/VS Code) |
| `AddWorkLog` | Appends a timestamped entry to a work log file (distinct from `AddNote`/`QuickNote` — see note above) |
| `ConfigureWorkLog` | Sets/clears the file path where work log entries are saved |
| `EditConfig` | Interactive wizard to edit an existing shortcut entry in a config file |
| `AddQuote` | Adds a quote to the personal random-quote pool |
| `RemoveQuote` | Removes a quote from the personal random-quote pool |
| `EditQuote` | Edits an existing quote in the personal random-quote pool |
| `ToggleQuotes` | Enables/disables the random quote shown after each action |
| `StartTimer` | Starts/resumes a focus-goal countdown timer (supports multiple saved goals, pause/resume, and outcome logging) |
| `ViewTimerHistory` | Shows a table of completed/abandoned timer goals plus any in-progress saved goals |
| `ConfigureTimer` | Sets/resets the file path where timer goal data is saved |
| `ConfigureSuccessSound` | Sets/clears a custom sound played after a successful action |
| `ConfigureErrorSound` | Sets/clears a custom sound played after a failed action |
| `ConfigureCloseTimeout` | Sets how many seconds a launcher-opened command/PowerShell window waits before auto-closing |
| `ShowHelp` | Prints the `cc help` quick-reference card |
| `ShowDocs` | Prints the `cc docs` detailed, topic-based guides |
| `list` | Prints all loaded shortcuts as a tree |
| `Menu` | Interactive selection menu of all shortcuts |
