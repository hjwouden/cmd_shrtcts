namespace cmd_shrtcts.Tests
{
    public class LoaderTests
    {
        [Fact]
        public void GetUserDataDirectory_ReturnsValidPath()
        {
            // Act
            var result = Loader.GetUserDataDirectory();

            // Assert
            Assert.NotNull(result);
            Assert.Contains("cmd_shrtcts", result);
        }

        [Fact]
        public void GetUserAppSettingsPath_ReturnsJsonFile()
        {
            // Act
            var result = Loader.GetUserAppSettingsPath();

            // Assert
            Assert.NotNull(result);
            Assert.EndsWith("appsettings.json", result);
        }

        [Theory]
        [InlineData("OpenWebPage")]
        [InlineData("OpenCMD")]
        [InlineData("list")]
        [InlineData("Menu")]
        [InlineData("AddNote")]
        public void TryGetActionDelegate_ValidActions_ReturnsTrue(string actionName)
        {
            // Act
            var result = Loader.TryGetActionDelegate(actionName, out var action);

            // Assert
            Assert.True(result);
            Assert.NotNull(action);
        }

        [Fact]
        public void TryGetActionDelegate_InvalidAction_ReturnsFalse()
        {
            // Act
            var result = Loader.TryGetActionDelegate("InvalidActionXYZ", out var action);

            // Assert
            Assert.False(result);
            Assert.Null(action);
        }

        [Theory]
        [InlineData("SetConfigPath")]
        [InlineData("RemoveConfigPath")]
        [InlineData("RemoveFromConfig")]
        [InlineData("ConfigureNote")]
        [InlineData("AddToConfig")]
        [InlineData("ConfigureSuccessSound")]
        [InlineData("ConfigureErrorSound")]
        [InlineData("EditConfig")]
        [InlineData("AddQuote")]
        [InlineData("RemoveQuote")]
        [InlineData("EditQuote")]
        [InlineData("ToggleQuotes")]
        [InlineData("AddWorkLog")]
        [InlineData("ConfigureWorkLog")]
        [InlineData("StartTimer")]
        [InlineData("ViewTimerHistory")]
        [InlineData("ConfigureTimer")]
        [InlineData("ShowHelp")]
        [InlineData("ShowDocs")]
        public void AllConfigActions_HaveDelegates_Defined(string actionName)
        {
            // Arrange & Act
            var result = Loader.TryGetActionDelegate(actionName, out var action);

            // Assert
            Assert.True(result, $"Action '{actionName}' should be defined");
            Assert.NotNull(action);
        }

        [Fact]
        public void GetPackagedAppSettingsPath_ReturnsValidPath()
        {
            // Arrange
            Loader.ASSEMBLY_LOCATION = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";

            // Act
            var result = Loader.GetPackagedAppSettingsPath();

            // Assert
            Assert.NotNull(result);
            Assert.EndsWith("appsettings.json", result);
        }

        [Fact]
        public void ChangeFromLocalToDirectoryPath_ReturnsAbsolutePath()
        {
            // Arrange
            Loader.ASSEMBLY_LOCATION = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
            string relativePath = @".\Data\Configs";

            // Act
            var result = Loader.ChangeFromLocalToDirectoryPath(relativePath);

            // Assert
            Assert.NotNull(result);
            Assert.True(System.IO.Path.IsPathRooted(result), "Result should be absolute path");
            Assert.Contains("Data", result);
        }
    }

    public class ConfigFileTests
    {
        [Fact]
        public void SystemConfig_AllEntriesHaveRegisteredDelegates()
        {
            // Arrange
            Loader.ASSEMBLY_LOCATION = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
            Loader.OUTPUT_LOG_FILE_PATH = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cc-test-log.txt");
            Loader.ERROR_SOUND_FILE_PATH = null; // prevent sound playback on missing delegate

            var configPath = System.IO.Path.Combine(Loader.ASSEMBLY_LOCATION, "Data", "Configs", "system-config.json");

            // Act: load the actual JSON file to get every declared action name
            var json = System.IO.File.ReadAllText(configPath);
            var entries = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Loader.Root>>(json);

            // Assert: every action string in system-config.json must resolve to a delegate
            Assert.NotNull(entries);
            Assert.NotEmpty(entries);

            foreach (var entry in entries)
            {
                Assert.True(
                    Loader.TryGetActionDelegate(entry.action, out _),
                    $"system-config.json entry with action '{entry.action}' has no registered delegate in TryGetActionDelegate"
                );
            }
        }

        [Fact]
        public void SystemConfig_LoadActionsDictionary_LoadsAllAliases()
        {
            // Arrange
            Loader.ASSEMBLY_LOCATION = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "";
            Loader.OUTPUT_LOG_FILE_PATH = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cc-test-log.txt");
            Loader.ERROR_SOUND_FILE_PATH = null;

            var configPath = System.IO.Path.Combine(Loader.ASSEMBLY_LOCATION, "Data", "Configs", "system-config.json");

            // Count expected total aliases from the JSON
            var json = System.IO.File.ReadAllText(configPath);
            var entries = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Loader.Root>>(json)
                ?? new List<Loader.Root>();
            // Keys are lowercased on load, so count unique lowercase aliases to match dictionary behavior
            int expectedAliasCount = entries
                .SelectMany(e => e.AdditionalNames ?? new System.Collections.Generic.List<string>())
                .Select(a => a.ToLowerInvariant())
                .Distinct()
                .Count();

            // Act
            var dictionary = Loader.LoadActionsDictionary(new[] { configPath });

            // Assert: every alias loaded — no entries silently dropped due to missing delegates
            Assert.Equal(expectedAliasCount, dictionary.Count);
        }
    }

    public class ActionDelegateTests
    {
        [Theory]
        [InlineData("OpenWebPage")]
        [InlineData("OpenCMD")]
        [InlineData("OpenFile")]
        [InlineData("OpenCMDWithParams")]
        [InlineData("list")]
        [InlineData("Menu")]
        [InlineData("PutTextOnClipboard")]
        [InlineData("AddToConfig")]
        [InlineData("RemoveFromConfig")]
        [InlineData("SetConfigPath")]
        [InlineData("RemoveConfigPath")]
        [InlineData("AddNote")]
        [InlineData("ConfigureNote")]
        [InlineData("ConfigureSuccessSound")]
        [InlineData("ConfigureErrorSound")]
        [InlineData("EditConfig")]
        [InlineData("AddQuote")]
        [InlineData("RemoveQuote")]
        [InlineData("EditQuote")]
        [InlineData("ToggleQuotes")]
        [InlineData("AddWorkLog")]
        [InlineData("ConfigureWorkLog")]
        [InlineData("StartTimer")]
        [InlineData("ViewTimerHistory")]
        [InlineData("ConfigureTimer")]
        [InlineData("ShowHelp")]
        [InlineData("ShowDocs")]
        public void AllRegisteredActions_HaveDelegates(string actionName)
        {
            // Act
            var result = Loader.TryGetActionDelegate(actionName, out var action);

            // Assert
            Assert.True(result, $"Action '{actionName}' should have a delegate registered");
            Assert.NotNull(action);
        }
    }

    public class BrowserSelectionTests
    {
        [Theory]
        [InlineData("chrome", "chrome")]
        [InlineData("Chrome", "chrome")]
        [InlineData("  CHROME  ", "chrome")]
        [InlineData("Google Chrome", "chrome")]
        [InlineData("edge", "edge")]
        [InlineData("Microsoft Edge", "edge")]
        [InlineData("firefox", "firefox")]
        public void NormalizeBrowser_KnownBrowsers_ReturnsCanonicalToken(string input, string expected)
        {
            Assert.Equal(expected, Actions.NormalizeBrowser(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("default")]
        [InlineData("safari")]
        [InlineData("opera")]
        public void NormalizeBrowser_UnknownOrEmpty_ReturnsNullForOsDefault(string? input)
        {
            Assert.Null(Actions.NormalizeBrowser(input));
        }

        [Theory]
        [InlineData("chrome", "Google Chrome")]
        [InlineData("edge", "Microsoft Edge")]
        [InlineData("firefox", "Firefox")]
        public void MacBrowserApp_KnownBrowsers_MapToOpenAppName(string input, string expected)
        {
            Assert.Equal(expected, Actions.MacBrowserApp(input));
        }

        [Theory]
        [InlineData("chrome", "chrome")]
        [InlineData("edge", "msedge")]
        [InlineData("firefox", "firefox")]
        public void WindowsBrowserExecutable_KnownBrowsers_MapToExecutable(string input, string expected)
        {
            Assert.Equal(expected, Actions.WindowsBrowserExecutable(input));
        }

        [Fact]
        public void BrowserResolvers_UnknownBrowser_ReturnNullForOsDefault()
        {
            Assert.Null(Actions.MacBrowserApp("safari"));
            Assert.Null(Actions.WindowsBrowserExecutable(null));
            Assert.Null(Actions.LinuxBrowserCommand("default"));
        }

        [Fact]
        public void OpenWebPageEntry_WithBrowser_LoadsDelegateAndRoundTrips()
        {
            // Arrange: a config file containing an OpenWebPage entry with a per-item browser
            Loader.OUTPUT_LOG_FILE_PATH = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "cc-test-log.txt");
            Loader.ERROR_SOUND_FILE_PATH = null;

            var tempConfig = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), $"cc-browser-test-{System.Guid.NewGuid():N}.json");
            var entries = new List<Loader.Root>
            {
                new Loader.Root
                {
                    AdditionalNames = new List<string> { "Docs" },
                    action = "OpenWebPage",
                    parameter = "https://example.com",
                    browser = "edge"
                }
            };
            System.IO.File.WriteAllText(tempConfig,
                Newtonsoft.Json.JsonConvert.SerializeObject(entries));

            try
            {
                // Act: the entry loads into a runnable delegate (browser captured at load time)
                var dictionary = Loader.LoadActionsDictionary(new[] { tempConfig });

                // Assert: alias present with its browser preserved on the stored Root
                Assert.True(dictionary.ContainsKey("docs"));
                Assert.Equal("edge", dictionary["docs"].browser);
                Assert.True(Loader.actionsDictionary1!.ContainsKey("docs"));
            }
            finally
            {
                System.IO.File.Delete(tempConfig);
            }
        }
    }

    public class OsAwareParameterTests
    {
        [Fact]
        public void ResolveEffectiveParameter_UsesOsSpecificOverride_WhenPresent()
        {
            var root = new Loader.Root
            {
                parameter = "/generic/fallback",
                parameterWindows = "C:\\win\\path",
                parameterMac = "/mac/path",
                parameterLinux = "/linux/path"
            };

            var expected =
                System.OperatingSystem.IsWindows() ? "C:\\win\\path" :
                System.OperatingSystem.IsMacOS() ? "/mac/path" :
                "/linux/path";

            Assert.Equal(expected, Loader.ResolveEffectiveParameter(root));
        }

        [Fact]
        public void ResolveEffectiveParameter_FallsBackToGeneric_WhenNoOsOverride()
        {
            var root = new Loader.Root { parameter = "/generic/only" };
            Assert.Equal("/generic/only", Loader.ResolveEffectiveParameter(root));
        }

        [Fact]
        public void ResolveEffectiveParameter_GenericIsReturnedVerbatim_NotExpanded()
        {
            // The generic parameter is used by non-path actions (URLs, commands), so it must
            // not be path-expanded.
            var root = new Loader.Root { parameter = "~/not-a-path-token" };
            Assert.Equal("~/not-a-path-token", Loader.ResolveEffectiveParameter(root));
        }

        [Fact]
        public void ExpandPath_ExpandsLeadingTildeToHome()
        {
            var home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            Assert.Equal(home, Loader.ExpandPath("~"));
            var expanded = Loader.ExpandPath("~/Documents/notes");
            Assert.StartsWith(home, expanded);
            Assert.EndsWith("notes", expanded);
            Assert.DoesNotContain("~", expanded);
        }

        [Fact]
        public void ExpandPath_NonTildePathUnchanged()
        {
            Assert.Equal("/absolute/path", Loader.ExpandPath("/absolute/path"));
        }
    }

    public class NotesPathNormalizationTests
    {
        [Fact]
        public void NormalizeNotesPath_ExistingFolder_PlacesDefaultFileInside()
        {
            var dir = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), $"cc-notes-{System.Guid.NewGuid():N}");
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                var result = Actions.NormalizeNotesPath(dir);
                Assert.Equal(System.IO.Path.Combine(dir, "notes.md"), result);
            }
            finally
            {
                System.IO.Directory.Delete(dir);
            }
        }

        [Fact]
        public void NormalizeNotesPath_TrailingSeparator_TreatedAsFolder()
        {
            var dir = "/some/notes/folder" + System.IO.Path.DirectorySeparatorChar;
            var result = Actions.NormalizeNotesPath(dir);
            Assert.Equal(System.IO.Path.Combine("/some/notes/folder", "notes.md"), result);
        }

        [Fact]
        public void NormalizeNotesPath_UsesProvidedDefaultFileName()
        {
            var dir = "/some/folder" + System.IO.Path.DirectorySeparatorChar;
            var result = Actions.NormalizeNotesPath(dir, "inbox-todo.md");
            Assert.Equal(System.IO.Path.Combine("/some/folder", "inbox-todo.md"), result);
        }

        [Fact]
        public void NormalizeNotesPath_PathWithMdExtension_Unchanged()
        {
            Assert.Equal("/notes/todo.md", Actions.NormalizeNotesPath("/notes/todo.md"));
        }

        [Fact]
        public void NormalizeNotesPath_ExplicitNonMdExtension_Respected()
        {
            Assert.Equal("/notes/list.txt", Actions.NormalizeNotesPath("/notes/list.txt"));
        }

        [Fact]
        public void NormalizeNotesPath_NoExtensionAndNotAFolder_AppendsMd()
        {
            // A non-existent path with no extension is treated as a file and gets .md.
            var result = Actions.NormalizeNotesPath("/notes/brandnewfile");
            Assert.Equal("/notes/brandnewfile.md", result);
        }
    }
}
