using AIUnitTestWriter.SettingOptions;
using Microsoft.Extensions.Options;
using Moq;

namespace AIUnitTestWriter.UnitTests
{
    public class SkippedFilesManagerTests
    {
        private readonly SkippedFilesManager _skippedFilesManager;
        private readonly Mock<IOptions<SkippedFilesSettings>> _mockOptions;

        public SkippedFilesManagerTests()
        {
            var skippedFilesSettings = new SkippedFilesSettings
            {
                SkippedFiles = new List<string>
            {
                "Program.cs",
                "Startup.cs",
                "GlobalUsings.cs"
            }
            };

            _mockOptions = new Mock<IOptions<SkippedFilesSettings>>();
            _mockOptions.Setup(x => x.Value).Returns(skippedFilesSettings);

            _skippedFilesManager = new SkippedFilesManager(_mockOptions.Object);
        }

        [Fact]
        public void ShouldSkip_FileInSkippedList_ReturnsTrue()
        {
            // Arrange
            var filePath = "Program.cs";

            // Act
            var result = _skippedFilesManager.ShouldSkip(filePath);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ShouldSkip_FileNotInSkippedList_ReturnsFalse()
        {
            // Arrange
            var filePath = "MyClass.cs";

            // Act
            var result = _skippedFilesManager.ShouldSkip(filePath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ShouldSkip_FilePathWithDifferentCase_ReturnsTrue()
        {
            // Arrange
            var filePath = "PROGRAM.cs"; // Case-insensitive check

            // Act
            var result = _skippedFilesManager.ShouldSkip(filePath);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ShouldSkip_FilePathWithFullDirectory_ReturnsTrue()
        {
            // Arrange
            var filePath = @"C:\Project\Program.cs"; // Path included

            // Act
            var result = _skippedFilesManager.ShouldSkip(filePath);

            // Assert
            Assert.True(result);
        }
    }
}
