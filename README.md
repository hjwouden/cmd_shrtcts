# cmd_shrtcts
C# .NET Core Command line application, used to increase productivity with customizable shortcuts

## usage
cmd_shrtcts builds to an executable, that is passed command parameters to execute shortcuts quickly.
This exe file can be executed from the commandline, but to make it faster to access, I recommend configuring Windows PowerToys.
Windows PowerToys Run, allows you at anytime to use 'Alt + Space' and a search bar appears on the screen. 
I then added the location of the output exe of cmd_shrtcts to my PATH variables.
cmd_shrtcts also requires a configuration file, to be customized for what the commands are you want to be able to quickly enter to be able to make the tool useful. The following actions are ones that shrtcts can be configured to help with

## Actions
- OpenWebPage
- CopyTextToClipboard
- RunCmd

## example usage - Open Webpage/s
'Alt + Space' (opens PowerToys Run)
'>cc docs' (opens my shortcut link as specified in my configuration file)

## example usage - Help / List all commands
'Alt + Space' (opens PowerToys Run)
'>cc help' (lists all the available commends from configuration files)

## example usage - Copy to Clipboard Action
'Alt + Space' (opens PowerToys Run)
'>cc rca' (copies to my clipboard the template for Root Cause Analysis)
'ctrl + v' (paste the now copied template into the ticket I am doing Root Cause Analysis on)

## example usage - RunCmd
'Alt + Space' (opens PowerToys Run)
'>cc db' (runs my commandline script to open sql db as domain user for work access)

## Setup

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (or later)

### Installation (Global Tool)
You can install `cmd_shrtcts` as a global dotnet tool to easily access it from anywhere using the `sc` command.

1. Clone this repository.
2. Open a terminal in the project root.
3. Build and install the tool:
   ```bash
   dotnet pack cmd_shrtcts/cmd_shrtcts.csproj -o ./nupkg
   dotnet tool install --global --add-source ./nupkg cmd_shrtcts
   ```
4. Verify installation:
   ```bash
   sc help
   ```

---

### Windows Setup (PowerToys Run)
1. Install [Windows PowerToys](https://learn.microsoft.com/en-us/windows/powertoys/install).
2. Ensure `cmd_shrtcts` is installed as a global tool (see above) or the output `.exe` is in your `PATH`.
3. Open PowerToys Settings > PowerToys Run.
4. Use `Alt + Space` to open the search bar.
5. Type `>sc <command>` (e.g., `>sc docs`) to execute your shortcuts.

---

### Mac Setup (Raycast)
Raycast is a great alternative to PowerToys Run on macOS.

1. Install [Raycast](https://www.raycast.com/).
2. Install `cmd_shrtcts` as a global tool (see above).
3. Create a **Script Command** in Raycast:
   - Open Raycast > Settings > Extensions > Script Commands.
   - Click "Create Script Command".
   - Use the following template:
     ```bash
     #!/bin/bash

     # @raycast.schemaVersion 1
     # @raycast.title sc
     # @raycast.mode compact
     # @raycast.packageName Productivity
     # @raycast.argument1 { "type": "text", "placeholder": "command" }

     # Ensure dotnet tools are in PATH
     export PATH="$PATH:$HOME/.dotnet/tools"

     sc $1
     ```
4. Now you can open Raycast (`Cmd + Space` or your custom hotkey), type `sc`, then your shortcut (e.g., `sc docs`).

*Alternatively, you can just type `sc <command>` directly into your terminal.*

---

## Configuration
The tool uses JSON configuration files to define shortcuts.
- **System Config:** `Data/Configs/system-config.json` (Default examples)
- **User Config:** Located in `%LOCALAPPDATA%\cmd_shrtcts\appsettings.json` (Windows) or `~/.local/share/cmd_shrtcts/appsettings.json` (Mac).

You can add new shortcuts using the tool itself:
```bash
cc AddToConfig
```
Or by manually editing your configuration files.


## summary
The goal of this project was to create a tool that could executed quickly, be customizable to various needs, and be able to be done without having to move from the keyboard to any other app. There are countless apps that do the same thing as this tool but it has been a fun project to use to sharpen my skills. I hope you enjoy it as much as I do!

### tips for customization
1. Update your shortcuts frequently. I like using this tool for my works jira ticketing system, to quickly find tickets, I use shortcut commands like 'td' to be a shortcut for 'Tech Debt' so I can quickly access the jira for our tech debt, which changes each release cycle. I do similar things with 'docs' opens 4 pages which are the most important docs for my day to day work. I can open them all and not lose sight of documents/projects I am working on a little each day.

### roadmap for future additions
[x] - Add Action - Open File
[x] - Update Action - CopyToClipboard to work cross platform
[x] - Cross platform testing
[ ] - Additional Parameter support for things like 'Action: OpenWebPage in __ browser'