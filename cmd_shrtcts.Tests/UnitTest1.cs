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
