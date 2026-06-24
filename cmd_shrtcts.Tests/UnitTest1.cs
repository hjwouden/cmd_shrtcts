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
            var entries = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Loader.Root>>(json);
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
        public void AllRegisteredActions_HaveDelegates(string actionName)
        {
            // Act
            var result = Loader.TryGetActionDelegate(actionName, out var action);

            // Assert
            Assert.True(result, $"Action '{actionName}' should have a delegate registered");
            Assert.NotNull(action);
        }
    }
}
