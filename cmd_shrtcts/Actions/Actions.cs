using Newtonsoft.Json;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static cmd_shrtcts.Loader;
using static System.Net.Mime.MediaTypeNames;

namespace cmd_shrtcts
{
    public static partial class Actions
    {
        // Opens a URL. When browser is "chrome", "edge", or "firefox" the page opens in that
        // browser; otherwise (null/empty/unrecognized) it opens in the OS default browser.
        // Any failure to launch the requested browser falls back to the OS default.
        public static void OpenWebPage(string path, string? browser = null)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    var exe = WindowsBrowserExecutable(browser);
                    if (exe == null)
                        OpenInOsDefaultBrowser(path);
                    else
                        Process.Start(new ProcessStartInfo(exe) { Arguments = path, UseShellExecute = true });
                }
                else if (OperatingSystem.IsMacOS())
                {
                    var app = MacBrowserApp(browser);
                    if (app == null)
                        Process.Start("open", path);
                    else
                        Process.Start("open", new[] { "-a", app, path });
                }
                else // Linux
                {
                    var cmd = LinuxBrowserCommand(browser);
                    Process.Start(cmd ?? "xdg-open", path);
                }
            }
            catch (Exception ex)
            {
                Loader.LogText($"OpenWebPage: could not launch browser '{browser}' for '{path}': {ex.Message}. Falling back to OS default.");
                try { OpenInOsDefaultBrowser(path); }
                catch (Exception ex2) { Loader.LogText($"OpenWebPage: OS default fallback also failed: {ex2.Message}"); }
            }
        }

        // Interactive picker for the browser an OpenWebPage entry should use.
        // Returns "chrome"/"edge"/"firefox", or null for the OS default browser.
        // Pass the entry's current value to offer a "keep current" choice when editing.
        private static string? PromptForBrowser(string? current)
        {
            const string osDefault = "Default (OS default browser)";
            var normalized = NormalizeBrowser(current);

            var choices = new List<string>();
            string? keepLabel = null;
            if (!string.IsNullOrWhiteSpace(current))
            {
                keepLabel = $"Keep current ({normalized ?? "default"})";
                choices.Add(keepLabel);
            }
            choices.AddRange(new[] { osDefault, "Chrome", "Edge", "Firefox" });

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Open in which browser?[/]")
                    .AddChoices(choices));

            if (choice == keepLabel) return normalized;
            if (choice == osDefault) return null;
            return choice.ToLowerInvariant();
        }

        private static void OpenInOsDefaultBrowser(string path)
        {
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            else if (OperatingSystem.IsMacOS())
                Process.Start("open", path);
            else
                Process.Start("xdg-open", path);
        }

        // Normalizes a user-supplied browser choice to a known token, or null for "OS default".
        internal static string? NormalizeBrowser(string? browser)
        {
            switch (browser?.Trim().ToLowerInvariant())
            {
                case "chrome":
                case "google chrome":
                    return "chrome";
                case "edge":
                case "microsoft edge":
                    return "edge";
                case "firefox":
                    return "firefox";
                default:
                    return null; // OS default
            }
        }

        // macOS application name for `open -a`; null means OS default.
        internal static string? MacBrowserApp(string? browser) => NormalizeBrowser(browser) switch
        {
            "chrome" => "Google Chrome",
            "edge" => "Microsoft Edge",
            "firefox" => "Firefox",
            _ => null,
        };

        // Windows executable name (resolved via ShellExecute App Paths); null means OS default.
        internal static string? WindowsBrowserExecutable(string? browser) => NormalizeBrowser(browser) switch
        {
            "chrome" => "chrome",
            "edge" => "msedge",
            "firefox" => "firefox",
            _ => null,
        };

        // Linux launcher command; null means use xdg-open (OS default).
        internal static string? LinuxBrowserCommand(string? browser) => NormalizeBrowser(browser) switch
        {
            "chrome" => "google-chrome",
            "edge" => "microsoft-edge",
            "firefox" => "firefox",
            _ => null,
        };


        private record MenuEntry(string Key, string Display);

        private static List<MenuEntry> GetMenuEntries(string? excludeCategory = null, string? onlyCategory = null)
        {
            var entries = new List<MenuEntry>();
            var alreadyInserted = new List<string>();

            foreach (var x in Loader.actionsDictionary ?? new Dictionary<string, Loader.Root>())
            {
                var cat = x.Value?.category;
                if (excludeCategory != null && cat == excludeCategory) continue;
                if (onlyCategory != null && cat != onlyCategory) continue;

                if (!alreadyInserted.Contains(x.Key))
                {
                    var desc = x.Value?.description;
                    var display = string.IsNullOrWhiteSpace(desc)
                        ? x.Key
                        : $"{x.Key}  [grey]— {desc}[/]";
                    entries.Add(new MenuEntry(x.Key, display));
                    alreadyInserted.Add(x.Key);

                    if (x.Value?.AdditionalNames != null)
                        foreach (var y in x.Value.AdditionalNames)
                            if (!alreadyInserted.Contains(y.ToLowerInvariant()))
                                alreadyInserted.Add(y.ToLowerInvariant());
                }
            }

            return entries;
        }

        public static string[] getHelpMenuChoices(string? excludeCategory = null, string? onlyCategory = null)
            => GetMenuEntries(excludeCategory, onlyCategory).Select(e => e.Key).ToArray();


        public static void ListActions(string param)
        {
            AnsiConsole.Markup("[underline red]cmd_shrts[/]\n");

            Dictionary<string, string[]> newPairs = new Dictionary<string, string[]>();
            List<string> alreadyInserted = new List<string>();

            var root = new Tree("Root");

            foreach (var x in Loader.actionsDictionary ?? new Dictionary<string, Loader.Root>())
            {
                if (!alreadyInserted.Contains(x.Key))
                {
                    var a = root.AddNode(x.Key);
                    alreadyInserted.Add(x.Key);
                    if (x.Value?.AdditionalNames != null)
                    {
                        foreach (var y in x.Value.AdditionalNames)
                        {
                            if (!alreadyInserted.Contains(y.ToLowerInvariant()))
                            {
                                a.AddNode(y);
                                alreadyInserted.Add(y.ToLowerInvariant());
                            }
                        }
                    }
                }
            }

            AnsiConsole.Write(root);
        }

        private const string SystemSettingsLabel = "System Settings >";

        public static void SelectMenu()
        {
            ShowMenuTier(title: "Select from Menu:", excludeCategory: "system", includeSystemEntry: true);
        }

        private static void ShowMenuTier(string title, string? excludeCategory = null, string? onlyCategory = null, bool includeSystemEntry = false)
        {
            var entries = GetMenuEntries(excludeCategory: excludeCategory, onlyCategory: onlyCategory);

            if (includeSystemEntry)
                entries.Add(new MenuEntry(SystemSettingsLabel, $"{SystemSettingsLabel}  [grey]— configuration and setup[/]"));

            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<MenuEntry>()
                    .Title(title)
                    .PageSize(25)
                    .MoreChoicesText("Move up and down to reveal more choices")
                    .UseConverter(e => e.Display)
                    .AddChoices(entries)
            );

            LogText("Menu Selection: " + selection.Key);

            if (selection.Key == SystemSettingsLabel)
            {
                ShowMenuTier(title: "System Settings:", onlyCategory: "system");
                return;
            }

            string normalizedSelection = selection.Key.ToLowerInvariant();
            if (Loader.actionsDictionary1?.TryGetValue(normalizedSelection, out Action<object>? action) == true)
            {
                if (!Loader.TryGetParameterFromJson(normalizedSelection, out object? parameter) || parameter?.ToString() == "prompt")
                {
                    LogText("Enter a parameter:");
                    parameter = Console.ReadLine();
                }
                action!.Invoke(parameter ?? string.Empty);
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Action not found:[/] {selection.Key}");
            }
        }

        public static void TextToClipboard(string pathToTextFile)
        {
            try
            {
                // Check if the file exists
                if (!File.Exists(pathToTextFile))
                {
                    AnsiConsole.MarkupLine($"[red]Error: File not found:[/] {pathToTextFile}");
                    Loader.LogText($"TextToClipboard: File not found - {pathToTextFile}");
                    return;
                }

                // Read the file content
                string text = File.ReadAllText(pathToTextFile);

                if (string.IsNullOrWhiteSpace(text))
                {
                    AnsiConsole.MarkupLine("[yellow]Warning: File is empty.[/]/]");
                    return;
                }

                // Use TextCopy to copy to clipboard (works cross-platform)
                TextCopy.ClipboardService.SetText(text);

                AnsiConsole.MarkupLine($"[green]Text copied to clipboard from:[/] {pathToTextFile}");
                Loader.LogText($"TextToClipboard: Successfully copied text to clipboard from {pathToTextFile}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                Loader.LogText($"TextToClipboard: {ex}");
            }
        }

        public static void OpenFile(string filePath)
        {
            try
            {
                // Check if the file exists
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Error: File '{filePath}' not found.");
                    return; // Exit the method if the file doesn't exist
                }

                // Use the Process class to start the associated program
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true // Use the shell for proper file association handling
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening file: {ex.Message}");
                // You might want to log the error for debugging
            }
        }


        public static void AddToConfig(string input)
        {

            Console.WriteLine("");

            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                .Title("Select file to save action to:")
                .PageSize(20)
                .MoreChoicesText("Move up and down to reveal more choices")
                .AddChoices(Loader.INPUT_CONFIG_LOCATIONS)
            );

            LogText("Menu Selection: " + selection);

            string absoluteFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, selection);

            string filePath = absoluteFilePath;

            // Get input for the JSON object parameters
            Console.WriteLine("Enter comma-separated additional names (e.g., name1, name2): ");
            string namesInput = Console.ReadLine() ?? string.Empty;
            List<string> additionalNames = namesInput.Split(',').Select(name => name.Trim()).ToList();

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select the Action Type:")
                    .PageSize(10)
                    .MoreChoicesText("Move up and down to reveal more choices")
                    .AddChoices(new[]
                    {
                        "OpenWebPage", "OpenCMD", "OpenCMDPersistent", "PutTextOnClipboard", "OpenFile", "OpenCMDWithParams", "OpenCMDAtLocation", "QuickNote", "AddNote"
                    })
            );

            string? parameter;
            string? browser = null;
            switch (action)
            {
                case "OpenWebPage":
                    Console.WriteLine("Enter URL:");
                    parameter = Console.ReadLine();
                    browser = PromptForBrowser(null);
                    break;

                case "OpenCMD":
                    Console.WriteLine("Enter the command (e.g., dir, cc menu, etc):");
                    parameter = Console.ReadLine();
                    break;

                case "OpenCMDPersistent":
                    Console.WriteLine("Enter the command to run (e.g., python file.py, node app.js, etc):");
                    parameter = Console.ReadLine();
                    break;

                case "PutTextOnClipboard":
                    Console.WriteLine("Enter full path to text file:");
                    parameter = Console.ReadLine();
                    break;

                case "AddNote":
                    Console.WriteLine("This action doesn't require a parameter.");
                    parameter = "";
                    break;

                case "OpenFile":
                    Console.WriteLine("Enter full path to file:");
                    parameter = Console.ReadLine();
                    break;

                case "OpenCMDWithParams":
                    Console.WriteLine("Enter the command:");
                    var cmd = Console.ReadLine();
                    Console.WriteLine("Enter the parameter/password:");
                    var param = Console.ReadLine();
                    parameter = $"{cmd},{param}";
                    break;

                case "OpenCMDAtLocation":
                    Console.WriteLine("Enter the directory path:");
                    parameter = Console.ReadLine();
                    break;

                case "QuickNote":
                    Console.WriteLine("Enter the folder path where notes will be saved:");
                    parameter = Console.ReadLine();
                    break;

                default:
                    Console.WriteLine("Enter the parameter:");
                    parameter = Console.ReadLine();
                    break;
            }

            var description = AnsiConsole.Prompt(
                new TextPrompt<string>("[grey]Description (optional — shown as help text in the menu):[/]")
                    .AllowEmpty()
            );

            var jsonObject = new Loader.Root
            {
                AdditionalNames = additionalNames,
                action = action,
                parameter = parameter,
                description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                browser = browser
            };

            List<dynamic> jsonObjects = new List<dynamic>();
            if (File.Exists(filePath))
            {
                string fileContent = File.ReadAllText(filePath);
                jsonObjects = JsonConvert.DeserializeObject<List<dynamic>>(fileContent) ?? new List<dynamic>();
            }

            jsonObjects.Add(jsonObject);

            // Ignore nulls so optional fields (description, browser, category) are only written when set.
            var writeSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };
            string updatedJson = JsonConvert.SerializeObject(jsonObjects, writeSettings);
            File.WriteAllText(filePath, updatedJson);

            Console.WriteLine("JSON object appended successfully!");
        }


        public static void AddActionToConfig(string configFilePath, string jsonActionToAdd)
        {

        }


        /// <summary>
        /// Opens a Command Prompt and closes after specified timeout
        /// </summary>
        /// <param name="cmd"></param>
        public static void OpenCMD(string cmd)
        {
            if (OperatingSystem.IsWindows())
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.UseShellExecute = true;
                // /C runs and exits; /NOBREAK makes timeout work even when stdin is redirected
                process.StartInfo.Arguments = $"/C {cmd} & timeout /t {Loader.COMMAND_WINDOW_TIMEOUT_SECONDS} /NOBREAK";
                process.Start();
            }
            else if (OperatingSystem.IsMacOS())
            {
                // Write a temp script that runs the command then shows a countdown
                var tempScript = Path.Combine(Path.GetTempPath(), $"sc_cmd_{Guid.NewGuid():N}.sh");
                var countdown = Loader.COMMAND_WINDOW_TIMEOUT_SECONDS;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("#!/bin/sh");
                sb.AppendLine(cmd);
                sb.AppendLine("echo ''");
                sb.AppendLine($"i={countdown}");
                sb.AppendLine("while [ $i -gt 0 ]; do");
                sb.AppendLine("  printf \"\\rClosing in %d...  \" $i");
                sb.AppendLine("  sleep 1");
                sb.AppendLine("  i=$((i - 1))");
                sb.AppendLine("done");
                sb.AppendLine("echo ''");
                File.WriteAllText(tempScript, sb.ToString());
                File.SetUnixFileMode(tempScript,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.OtherRead);

                // Open in Terminal.app; close the window after countdown + 1s buffer via osascript
                var psi = new ProcessStartInfo("osascript") { UseShellExecute = false, CreateNoWindow = true };
                psi.ArgumentList.Add("-e"); psi.ArgumentList.Add("tell application \"Terminal\"");
                psi.ArgumentList.Add("-e"); psi.ArgumentList.Add($"set newTab to do script \"{tempScript}\"");
                psi.ArgumentList.Add("-e"); psi.ArgumentList.Add("activate");
                psi.ArgumentList.Add("-e"); psi.ArgumentList.Add($"delay {countdown + 1}");
                psi.ArgumentList.Add("-e"); psi.ArgumentList.Add("try");
                psi.ArgumentList.Add("-e"); psi.ArgumentList.Add("close window of newTab");
                psi.ArgumentList.Add("-e"); psi.ArgumentList.Add("end try");
                psi.ArgumentList.Add("-e"); psi.ArgumentList.Add("end tell");
                Process.Start(psi);
            }
            else // Linux
            {
                Process.Start(new ProcessStartInfo("/bin/sh")
                {
                    Arguments = $"-c \"{cmd}; sleep {Loader.COMMAND_WINDOW_TIMEOUT_SECONDS}\"",
                    UseShellExecute = false
                });
            }
        }

        public static void OpenCMDWithParams(string cmd, string param)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
                Arguments = OperatingSystem.IsWindows() ? "" : $"-c \"{cmd}\"",
                RedirectStandardInput = true,
                UseShellExecute = false
            };

            Process cmdProcess = new Process
            {
                StartInfo = startInfo
            };

            cmdProcess.Start();

            if (OperatingSystem.IsWindows())
            {
                // Write the command to the standard input stream
                cmdProcess.StandardInput.WriteLine(cmd);
            }

            // Write the password/param to the standard input stream
            cmdProcess.StandardInput.WriteLine(param);
            cmdProcess.StandardInput.Flush();

            cmdProcess.WaitForExit();
            cmdProcess.Close();
        }

        public static void OpenCMDAtLocation(string directoryPath)
        {
            try
            {
                // Verify the directory exists
                if (!Directory.Exists(directoryPath))
                {
                    AnsiConsole.MarkupLine($"[red]Error: Directory not found:[/] {directoryPath}");
                    Loader.LogText($"OpenCMDAtLocation: Directory not found - {directoryPath}");
                    return;
                }

                if (OperatingSystem.IsWindows())
                {
                    // Use start command to open cmd in a new window without intermediate window
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C start cmd.exe /K cd /d \"{directoryPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    Process.Start(startInfo);
                }
                else if (OperatingSystem.IsMacOS())
                {
                    // Open Terminal.app at the specified directory
                    Process.Start("open", $"-a Terminal \"{directoryPath}\"");
                }
                else // Linux
                {
                    // Try common terminal emulators
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "x-terminal-emulator",
                        Arguments = $"--working-directory=\"{directoryPath}\"",
                        UseShellExecute = false
                    };
                    Process.Start(startInfo);
                }

                Loader.LogText($"OpenCMDAtLocation: Opened command prompt at {directoryPath}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error opening command prompt:[/] {ex.Message}");
                Loader.LogText($"OpenCMDAtLocation: {ex}");
            }
        }

        public static void OpenCMDPersistent(string cmd)
        {
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.UseShellExecute = true;
                // Use /K to run command and keep window open (instead of /C which closes)
                process.StartInfo.Arguments = $"/K {cmd}";
                process.Start();

                Loader.LogText($"OpenCMDPersistent: Opened persistent command prompt with: {cmd}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error opening command prompt:[/] {ex.Message}");
                Loader.LogText($"OpenCMDPersistent: {ex}");
            }
        }

        public static void QuickNote(string folderPath)
        {
            try
            {
                // Ensure the folder exists
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    Loader.LogText($"QuickNote: Created directory {folderPath}");
                }

                // Create filename with date and time for unique files
                string fileName = $"quickNote-{DateTime.Now:yyyy-MM-dd-HHmmss}.md";
                string filePath = Path.Combine(folderPath, fileName);

                // Create the file with a header
                string header = $"# Quick Notes - {DateTime.Now:MMMM dd, yyyy h:mm tt}\n\n";
                File.WriteAllText(filePath, header);
                Loader.LogText($"QuickNote: Created new file {filePath}");

                // Open the file in Notepad
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    Arguments = filePath,
                    UseShellExecute = true
                };
                Process.Start(startInfo);

                AnsiConsole.MarkupLine($"[green]Opened quick note:[/] {filePath}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error creating quick note:[/] {ex.Message}");
                Loader.LogText($"QuickNote: {ex}");
            }
        }

        public static void PlaySound(string kind)
        {
            string? filePath = Loader.ERROR_SOUND_FILE_PATH;
            if (kind == "success")
            {
                filePath = Loader.SUCCESS_SOUND_FILE_PATH;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                return; // Sound file not available, skip silently
            }

            try
            {
                if (OperatingSystem.IsWindows())
                {
                    SoundPlayer player = new SoundPlayer(filePath);
                    player.PlaySync(); // blocks until the sound finishes, no extra sleep needed
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("afplay", filePath)?.WaitForExit(); // blocks until afplay finishes
                }
            }
            catch (Exception ex)
            {
                Loader.LogText(ex.ToString());
            }
        }

        public static void DisplayRandomQuote()
        {
            if (!Loader.QUOTES_ENABLED) return;

            try
            {
                string quotesFilePath = Loader.GetEffectiveQuotesPath();

                if (!File.Exists(quotesFilePath))
                {
                    Loader.LogText($"Quotes file not found: {quotesFilePath}");
                    return;
                }

                // Read all quotes from the file
                string[] allQuotes = File.ReadAllLines(quotesFilePath);

                if (allQuotes.Length == 0)
                {
                    Loader.LogText("No quotes found in the quotes file");
                    return;
                }

                // Pick a random quote
                Random random = new Random();
                int randomIndex = random.Next(allQuotes.Length);
                string quote = allQuotes[randomIndex].Trim();

                // Display the quote with styling
                AnsiConsole.MarkupLine($"[bold yellow] {quote} [/]");
                Loader.LogText($"Displayed quote: {quote}");
            }
            catch (Exception ex)
            {
                Loader.LogText($"Error displaying quote: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows a countdown in the terminal and then closes it. Called after a shortcut
        /// action completes when cc/sc is running in an interactive (non-redirected) terminal
        /// — i.e. the terminal was opened by the launcher (Raycast, PowerToys) to run this command.
        /// </summary>
        public static void ShowLauncherCountdownAndClose()
        {
            for (int i = Loader.COMMAND_WINDOW_TIMEOUT_SECONDS; i > 0; i--)
            {
                Console.Write($"\rClosing in {i}...  ");
                Thread.Sleep(1000);
            }
            Console.WriteLine();

            if (OperatingSystem.IsMacOS())
            {
                // Close the Terminal.app window this process ran in.
                // Uses the front-window heuristic: since the launcher opened a dedicated
                // terminal for this shortcut and activated it, it should be the front window.
                var psi = new ProcessStartInfo("osascript") { UseShellExecute = false, CreateNoWindow = true };
                psi.ArgumentList.Add("-e");
                psi.ArgumentList.Add("tell application \"Terminal\" to close front window");
                Process.Start(psi);
            }
            // Windows: the cmd.exe window that PowerToys Run opened with /C will close
            // automatically when cc.exe exits. If it uses /K, the user needs to change that
            // in their PowerToys Run shell settings.
        }


    }
}
