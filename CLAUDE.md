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

**`Actions`** is a `static partial` class split across three files:
- `Actions.cs` — core actions: `OpenWebPage`, `OpenCMD`, `OpenCMDAtLocation`, `OpenCMDPersistent`, `OpenCMDWithParams`, `OpenFile`, `TextToClipboard`, `AddToConfig`, `ListActions`, `SelectMenu`, `PlaySound`, `DisplayRandomQuote`, `QuickNote`
- `ConfigActions.cs` — `RemoveFromConfig`, `RemoveConfigPath`
- `NoteActions.cs` — `AddNote`, `ConfigureNote`
- `AppSettingsActions.cs` — `SetConfigPath`

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
| `OpenFile` | Opens a file with its default associated application |
| `PutTextOnClipboard` | Copies contents of a text file to clipboard |
| `AddToConfig` | Interactive wizard to add a new shortcut |
| `RemoveFromConfig` | Interactive wizard to remove a shortcut |
| `SetConfigPath` | Adds a config JSON file path to user appsettings |
| `RemoveConfigPath` | Removes a config JSON file path from user appsettings |
| `AddNote` | Appends a markdown todo item to a notes file |
| `ConfigureNote` | Sets/clears the notes file path in user appsettings |
| `QuickNote` | Creates a timestamped `.md` file and opens it in Notepad |
| `list` | Prints all loaded shortcuts as a tree |
| `Menu` | Interactive selection menu of all shortcuts |
