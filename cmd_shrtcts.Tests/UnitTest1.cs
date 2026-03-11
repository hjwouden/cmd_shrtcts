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
    }
}
