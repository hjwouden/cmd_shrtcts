using System.Reflection;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Testing;

namespace cmd_shrtcts.Tests
{
    // Each test method gets its own instance, so constructor/Dispose provide clean isolation per test.
    public class IntegrationTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _appSettingsPath;
        private readonly TextReader _originalConsoleIn;
        private readonly TextWriter _originalConsoleOut;
        private readonly IAnsiConsole _originalAnsiConsole;

        // TestConsole drives AnsiConsole.Prompt (TextPrompt, SelectionPrompt) headlessly.
        // SimulateInput() feeds Console.ReadLine()-based prompts; PushAnsiInput() feeds Spectre prompts.
        protected readonly TestConsole AnsiTestConsole;

        public IntegrationTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"cc-itest-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
            _appSettingsPath = Path.Combine(_tempDir, "appsettings.json");

            _originalConsoleIn = Console.In;
            _originalConsoleOut = Console.Out;
            _originalAnsiConsole = AnsiConsole.Console;

            Loader._testUserDataDirOverride = _tempDir;
            Loader._testAppSettingsOverride = _appSettingsPath;
            Loader.ASSEMBLY_LOCATION = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            Loader.OUTPUT_LOG_FILE_PATH = Path.Combine(_tempDir, "log.txt");
            Loader.SUCCESS_SOUND_FILE_PATH = null;
            Loader.ERROR_SOUND_FILE_PATH = null;

            Console.SetOut(new StringWriter()); // silence Console.Write* calls during tests

            // Replace the global AnsiConsole with a TestConsole so TextPrompt/SelectionPrompt
            // read from queued input rather than from a real TTY.
            AnsiTestConsole = new TestConsole();
            AnsiConsole.Console = AnsiTestConsole;
        }

        public void Dispose()
        {
            Console.SetIn(_originalConsoleIn);
            Console.SetOut(_originalConsoleOut);
            AnsiConsole.Console = _originalAnsiConsole;
            Loader._testUserDataDirOverride = null;
            Loader._testAppSettingsOverride = null;
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        private void WriteAppSettings(Dictionary<string, object> settings) =>
            File.WriteAllText(_appSettingsPath, JsonConvert.SerializeObject(settings, Formatting.Indented));

        // For Console.ReadLine()-based prompts (AddNote, AddWorkLog)
        private void SimulateInput(string text) =>
            Console.SetIn(new StringReader(text));

        // For AnsiConsole TextPrompt-based prompts (AddQuote, SetConfigPath)
        private void PushAnsiInput(string text) =>
            AnsiTestConsole.Input.PushTextWithEnter(text);

        // ── AddWorkLog ────────────────────────────────────────────────────────────

        [Fact]
        public void AddWorkLog_WritesTimestampedEntry_ToConfiguredFile()
        {
            var logPath = Path.Combine(_tempDir, "worklog.txt");
            WriteAppSettings(new() { ["WorkLogFilePath"] = logPath });
            SimulateInput("worked on login screen bug\n");

            Actions.AddWorkLog("");

            Assert.True(File.Exists(logPath));
            var content = File.ReadAllText(logPath).Trim();
            Assert.Matches(@"^\d{2}-\d{2}-\d{4} \d+:\d{2} [AP]M = worked on login screen bug$", content);
        }

        [Fact]
        public void AddWorkLog_AppendsEntry_WhenFileAlreadyExists()
        {
            var logPath = Path.Combine(_tempDir, "worklog.txt");
            File.WriteAllText(logPath, "06-24-2026 9:00 AM = first entry");
            WriteAppSettings(new() { ["WorkLogFilePath"] = logPath });
            SimulateInput("second entry\n");

            Actions.AddWorkLog("");

            var lines = File.ReadAllLines(logPath);
            Assert.Equal(2, lines.Length);
            Assert.Contains("= second entry", lines[1]);
        }

        [Fact]
        public void AddWorkLog_ExitInput_DoesNotCreateFile()
        {
            var logPath = Path.Combine(_tempDir, "worklog.txt");
            WriteAppSettings(new() { ["WorkLogFilePath"] = logPath });
            SimulateInput("exit\n");

            Actions.AddWorkLog("");

            Assert.False(File.Exists(logPath));
        }

        // ── AddNote ───────────────────────────────────────────────────────────────

        [Fact]
        public void AddNote_WritesMarkdownTodoItem_ToConfiguredFile()
        {
            var notesPath = Path.Combine(_tempDir, "notes.md");
            WriteAppSettings(new() { ["NotesFilePath"] = notesPath });
            SimulateInput("fix the login bug\n");

            Actions.AddNote("");

            Assert.True(File.Exists(notesPath));
            Assert.Equal("- [ ] fix the login bug", File.ReadAllText(notesPath).Trim());
        }

        [Fact]
        public void AddNote_AppendsTodoItem_WhenFileAlreadyExists()
        {
            var notesPath = Path.Combine(_tempDir, "notes.md");
            File.WriteAllText(notesPath, "- [ ] existing note");
            WriteAppSettings(new() { ["NotesFilePath"] = notesPath });
            SimulateInput("new task\n");

            Actions.AddNote("");

            var lines = File.ReadAllLines(notesPath);
            Assert.Equal(2, lines.Length);
            Assert.Equal("- [ ] existing note", lines[0]);
            Assert.Equal("- [ ] new task", lines[1]);
        }

        [Fact]
        public void AddNote_ExitInput_DoesNotCreateFile()
        {
            var notesPath = Path.Combine(_tempDir, "notes.md");
            WriteAppSettings(new() { ["NotesFilePath"] = notesPath });
            SimulateInput("exit\n");

            Actions.AddNote("");

            Assert.False(File.Exists(notesPath));
        }

        // ── ToggleQuotes ──────────────────────────────────────────────────────────

        [Fact]
        public void ToggleQuotes_DisablesQuotes_WhenCurrentlyEnabled()
        {
            Loader.QUOTES_ENABLED = true;
            WriteAppSettings(new() { [Actions.QuotesEnabledKey] = true });

            Actions.ToggleQuotes("");

            Assert.False(Loader.QUOTES_ENABLED);
            var saved = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(_appSettingsPath))!;
            Assert.False(Convert.ToBoolean(saved[Actions.QuotesEnabledKey]));
        }

        [Fact]
        public void ToggleQuotes_EnablesQuotes_WhenCurrentlyDisabled()
        {
            Loader.QUOTES_ENABLED = false;
            WriteAppSettings(new() { [Actions.QuotesEnabledKey] = false });

            Actions.ToggleQuotes("");

            Assert.True(Loader.QUOTES_ENABLED);
            var saved = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(_appSettingsPath))!;
            Assert.True(Convert.ToBoolean(saved[Actions.QuotesEnabledKey]));
        }

        [Fact]
        public void ToggleQuotes_RoundTrip_RestoresOriginalState()
        {
            Loader.QUOTES_ENABLED = true;
            WriteAppSettings(new() { [Actions.QuotesEnabledKey] = true });

            Actions.ToggleQuotes("");
            Actions.ToggleQuotes("");

            Assert.True(Loader.QUOTES_ENABLED);
        }

        // ── AddQuote ──────────────────────────────────────────────────────────────

        [Fact]
        public void AddQuote_WritesQuote_ToUserQuotesFile()
        {
            // Pre-create an empty quotes file so EnsureUserQuotesFile skips copying bundled content
            File.WriteAllText(Path.Combine(_tempDir, "quotes.txt"), "");
            PushAnsiInput("I'll be back");

            Actions.AddQuote("");

            var content = File.ReadAllText(Path.Combine(_tempDir, "quotes.txt"));
            Assert.Contains("I'll be back", content);
        }

        [Fact]
        public void AddQuote_AppendsOnNewLine_WhenFileHasExistingContent()
        {
            var quotesPath = Path.Combine(_tempDir, "quotes.txt");
            File.WriteAllText(quotesPath, "I'm gonna make him an offer he can't refuse");
            PushAnsiInput("Here's looking at you, kid");

            Actions.AddQuote("");

            var lines = File.ReadAllLines(quotesPath)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();
            Assert.Equal(2, lines.Length);
            Assert.Equal("Here's looking at you, kid", lines[1]);
        }

        // ── SetConfigPath ─────────────────────────────────────────────────────────

        [Fact]
        public void SetConfigPath_SavesPath_ToApplicationVars_InputConfigs()
        {
            var userConfigPath = Path.Combine(_tempDir, "my-shortcuts.json");
            File.WriteAllText(userConfigPath, "[]");
            WriteAppSettings(new());
            PushAnsiInput(userConfigPath);

            Actions.SetConfigPath("");

            var jobj = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(_appSettingsPath));
            var configs = jobj["ApplicationVars"]?["InputConfigs"]?.ToObject<string[]>();
            Assert.NotNull(configs);
            Assert.Contains(userConfigPath, configs);
        }

        [Fact]
        public void SetConfigPath_DoesNotDuplicate_WhenPathAlreadyRegistered()
        {
            var userConfigPath = Path.Combine(_tempDir, "my-shortcuts.json");
            File.WriteAllText(userConfigPath, "[]");
            File.WriteAllText(_appSettingsPath, JsonConvert.SerializeObject(new
            {
                ApplicationVars = new { InputConfigs = new[] { userConfigPath } }
            }));
            PushAnsiInput(userConfigPath);

            Actions.SetConfigPath("");

            var jobj = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(_appSettingsPath));
            var configs = jobj["ApplicationVars"]?["InputConfigs"]?.ToObject<string[]>();
            Assert.NotNull(configs);
            Assert.Single(configs);
        }

        // ── GetEffectiveQuotesPath ────────────────────────────────────────────────

        [Fact]
        public void GetEffectiveQuotesPath_ReturnsUserPath_WhenUserFileExists()
        {
            var userQuotesPath = Path.Combine(_tempDir, "quotes.txt");
            File.WriteAllText(userQuotesPath, "test quote");

            var result = Loader.GetEffectiveQuotesPath();

            Assert.Equal(userQuotesPath, result);
        }

        [Fact]
        public void GetEffectiveQuotesPath_ReturnsBundledPath_WhenUserFileAbsent()
        {
            // No quotes.txt in _tempDir — should fall back to bundled file

            var result = Loader.GetEffectiveQuotesPath();

            Assert.Contains("movie-quotes.txt", result);
            Assert.True(File.Exists(result), "Bundled quotes file should exist in test output");
        }

        // ── LoadActionsDictionary with user config ────────────────────────────────

        [Fact]
        public void LoadActionsDictionary_LoadsCustomAliases_FromUserConfig()
        {
            var userConfigPath = Path.Combine(_tempDir, "my-shortcuts.json");
            File.WriteAllText(userConfigPath, JsonConvert.SerializeObject(new[]
            {
                new { AdditionalNames = new[] { "mysite", "ms" }, action = "OpenWebPage", parameter = "https://example.com" }
            }));

            var dict = Loader.LoadActionsDictionary(new[] { userConfigPath });

            Assert.True(dict.ContainsKey("mysite"));
            Assert.True(dict.ContainsKey("ms"));
        }

        [Fact]
        public void LoadActionsDictionary_MergesUserAndSystemConfig_BothAliasesPresent()
        {
            var systemConfigPath = Path.Combine(Loader.ASSEMBLY_LOCATION, "Data", "Configs", "system-config.json");
            var userConfigPath = Path.Combine(_tempDir, "my-shortcuts.json");
            File.WriteAllText(userConfigPath, JsonConvert.SerializeObject(new[]
            {
                new { AdditionalNames = new[] { "customalias" }, action = "OpenWebPage", parameter = "https://example.com" }
            }));

            var dict = Loader.LoadActionsDictionary(new[] { systemConfigPath, userConfigPath });

            Assert.True(dict.ContainsKey("customalias"), "user config alias should be present");
            Assert.True(dict.ContainsKey("wl"), "system config alias should be present");
            Assert.True(dict.ContainsKey("list"), "system config alias should be present");
        }

        // ── DisplayRandomQuote ────────────────────────────────────────────────────

        [Fact]
        public void DisplayRandomQuote_ReturnsEarly_WhenQuotesDisabled()
        {
            Loader.QUOTES_ENABLED = false;
            // No quotes file exists — if the guard didn't fire, it would throw or return empty

            var ex = Record.Exception(() => Actions.DisplayRandomQuote());

            Assert.Null(ex);
        }

        [Fact]
        public void DisplayRandomQuote_CompletesWithoutError_WhenValidQuotesFilePresent()
        {
            Loader.QUOTES_ENABLED = true;
            File.WriteAllLines(Path.Combine(_tempDir, "quotes.txt"),
                new[] { "I'll be back", "Here's looking at you, kid", "You can't handle the truth" });

            var ex = Record.Exception(() => Actions.DisplayRandomQuote());

            Assert.Null(ex);
        }
    }
}
