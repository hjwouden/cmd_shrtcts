# cmd_shrtcts
C# .NET Core Command line application, used to increase productivity with customizable shortcuts

## usage
cmd_shrtcts builds to an executable, that is passed command parameters to execute shortcuts quickly.
This exe file can be executed from the commandline, but to make it faster to access, I recommend configuring Windows PowerToys.
Windows PowerToys Run, allows you, at anytime to use 'Alt + Space' and a search bar appears on the screen. 
I then added the location of the output exe of cmd_shrtcts to my PATH variables.

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

## setup links
1. Install Windows PowerToys (https://learn.microsoft.com/en-us/windows/powertoys/install)
2. Download cmd_shrtcts repo, build, add custom shortcuts to configuration file
3. Add build output exe to PATH (add steps)
4. Start using shrtcts!