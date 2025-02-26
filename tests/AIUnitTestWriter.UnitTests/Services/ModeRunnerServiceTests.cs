using AIUnitTestWriter.Models;
using AIUnitTestWriter.Services;
using AIUnitTestWriter.Services.Interfaces;
using Moq;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class ModeRunnerServiceTests
    {
        private readonly Mock<ITestUpdater> _mockTestUpdater;
        private readonly Mock<ICodeMonitor> _mockCodeMonitor;
        private readonly Mock<IConsoleService> _mockConsoleService;
        private readonly ModeRunnerService _modeRunnerService;

        public ModeRunnerServiceTests()
        {
            _mockTestUpdater = new Mock<ITestUpdater>();
            _mockCodeMonitor = new Mock<ICodeMonitor>();
            _mockConsoleService = new Mock<IConsoleService>();

            _modeRunnerService = new ModeRunnerService(
                _mockTestUpdater.Object,
                _mockCodeMonitor.Object,
                _mockConsoleService.Object
            );
        }

        [Fact]
        public async Task RunAutoModeAsync_ShouldStartMonitoringAndPrintMessages()
        {
            // Arrange
            var config = new ProjectConfigModel
            {
                SrcFolder = "src",
                TestsFolder = "tests",
                SampleUnitTestContent = "sample content"
            };

            // Act
            await _modeRunnerService.RunAutoModeAsync(config);

            // Assert
            _mockConsoleService.Verify(cs => cs.WriteColored($"Monitoring source folder: {config.SrcFolder}", ConsoleColor.Green), Times.Once);
            _mockConsoleService.Verify(cs => cs.WriteColored($"Tests will be updated in: {config.TestsFolder}", ConsoleColor.Green), Times.Once);
            _mockConsoleService.Verify(cs => cs.WriteColored("Auto-detect mode activated. Monitoring code changes.", ConsoleColor.Blue), Times.Once);
            _mockCodeMonitor.Verify(cm => cm.Start(config.SrcFolder, config.TestsFolder, config.SampleUnitTestContent, false), Times.Once);
        }

        [Fact]
        public async Task RunManualModeAsync_ShouldExitOnExitCommand()
        {
            // Arrange
            var config = new ProjectConfigModel
            {
                SrcFolder = "src",
                TestsFolder = "tests",
                SampleUnitTestContent = "sample content"
            };

            _mockConsoleService.SetupSequence(cs => cs.Prompt(It.IsAny<string>(), It.IsAny<ConsoleColor>()))
                               .Returns("exit"); // Simulate user entering "exit"

            // Act
            await _modeRunnerService.RunManualModeAsync(config);

            // Assert
            _mockConsoleService.Verify(cs => cs.Prompt(It.IsAny<string>(), ConsoleColor.Yellow), Times.Once);
        }

        [Fact]
        public async Task RunManualModeAsync_ShouldHandleInvalidFilePath()
        {
            // Arrange
            var config = new ProjectConfigModel
            {
                SrcFolder = "src",
                TestsFolder = "tests",
                SampleUnitTestContent = "sample content"
            };

            _mockConsoleService.SetupSequence(cs => cs.Prompt(It.IsAny<string>(), It.IsAny<ConsoleColor>()))
                               .Returns("invalidPath") // Invalid file path
                               .Returns("exit"); // Exit after one iteration

            // Act
            await _modeRunnerService.RunManualModeAsync(config);

            // Assert
            _mockConsoleService.Verify(cs => cs.WriteColored("Invalid file path. Please try again.", ConsoleColor.Red), Times.Once);
        }

        [Fact]
        public async Task RunManualModeAsync_ShouldProcessFileChange()
        {
            // Arrange
            var config = new ProjectConfigModel
            {
                SrcFolder = "src",
                TestsFolder = "tests",
                SampleUnitTestContent = "sample content"
            };

            string validFilePath = "validFilePath.cs";
            File.WriteAllText(validFilePath, "public class Test {}"); // Create a temporary valid file

            var testResult = new TestGenerationResultModel
            {
                TempFilePath = "tempTestFile.cs",
                TestFilePath = "finalTestFile.cs",
                GeneratedTestCode = "public class Test {}"
            };

            _mockConsoleService.SetupSequence(cs => cs.Prompt(It.IsAny<string>(), It.IsAny<ConsoleColor>()))
                               .Returns(validFilePath) // First input: valid file
                               .Returns("yes") // Second input: approval
                               .Returns("exit"); // Third input: exit

            _mockTestUpdater.Setup(tu => tu.ProcessFileChange(config.SrcFolder, config.TestsFolder, validFilePath, config.SampleUnitTestContent, true))
                            .ReturnsAsync(testResult);

            // Act
            await _modeRunnerService.RunManualModeAsync(config);

            // Assert
            _mockConsoleService.Verify(cs => cs.WriteColored($"Please review the generated test code in the temporary file: {testResult.TempFilePath}", ConsoleColor.DarkBlue), Times.Once);
            _mockConsoleService.Verify(cs => cs.WriteColored($"Test file updated at: {testResult.TestFilePath}", ConsoleColor.Green), Times.Once);
            _mockTestUpdater.Verify(tu => tu.FinalizeTestUpdate(testResult), Times.Once);
        }

        [Fact]
        public async Task RunManualModeAsync_ShouldNotApplyChangesOnUserRejection()
        {
            // Arrange
            var config = new ProjectConfigModel
            {
                SrcFolder = "src",
                TestsFolder = "tests",
                SampleUnitTestContent = "sample content"
            };

            string validFilePath = "validFilePath.cs";
            File.WriteAllText(validFilePath, "public class Test {}"); // Create a temporary valid file

            var testResult = new TestGenerationResultModel
            {
                TempFilePath = "tempTestFile.cs",
                TestFilePath = "finalTestFile.cs",
                GeneratedTestCode = "public class Test {}"
            };

            _mockConsoleService.SetupSequence(cs => cs.Prompt(It.IsAny<string>(), It.IsAny<ConsoleColor>()))
                               .Returns(validFilePath) // First input: valid file
                               .Returns("no") // Second input: reject update
                               .Returns("exit"); // Third input: exit

            _mockTestUpdater.Setup(tu => tu.ProcessFileChange(config.SrcFolder, config.TestsFolder, validFilePath, config.SampleUnitTestContent, true))
                            .ReturnsAsync(testResult);

            // Act
            await _modeRunnerService.RunManualModeAsync(config);

            // Assert
            _mockConsoleService.Verify(cs => cs.WriteColored("Changes were not applied. You may manually copy any necessary code from the temporary file.", ConsoleColor.Red), Times.Once);
            _mockTestUpdater.Verify(tu => tu.FinalizeTestUpdate(It.IsAny<TestGenerationResultModel>()), Times.Never);
        }
    }
}
