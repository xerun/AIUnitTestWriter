using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.Models;
using AIUnitTestWriter.Services;
using Moq;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class ModeRunnerServiceTests
    {
        private readonly CancellationToken _cancellationToken = CancellationToken.None;
        private readonly Mock<ITestUpdaterService> _mockTestUpdater;
        private readonly Mock<ICodeMonitor> _mockCodeMonitor;
        private readonly Mock<IConsoleService> _mockConsoleService;
        private readonly ProjectConfigModel _projectConfig;
        private readonly ModeRunnerService _modeRunnerService;

        public ModeRunnerServiceTests()
        {
            _mockTestUpdater = new Mock<ITestUpdaterService>();
            _mockCodeMonitor = new Mock<ICodeMonitor>();
            _mockConsoleService = new Mock<IConsoleService>();

            _projectConfig = new ProjectConfigModel
            {
                SrcFolder = "src",
                TestsFolder = "tests",
                SampleUnitTestContent = "Sample test content"
            };

            _modeRunnerService = new ModeRunnerService(
                _mockTestUpdater.Object,
                _mockCodeMonitor.Object,
                _mockConsoleService.Object,
                _projectConfig
            );
        }

        [Fact]
        public async Task RunAutoModeAsync_Should_StartCodeMonitor_And_PrintMessages()
        {
            // Act
            var task = _modeRunnerService.RunAutoModeAsync();
            await Task.Delay(100); // Simulate brief async execution

            // Assert
            _mockConsoleService.Verify(m => m.WriteColored($"Monitoring source folder: {_projectConfig.SrcFolder}", ConsoleColor.Green), Times.Once);
            _mockConsoleService.Verify(m => m.WriteColored($"Tests will be updated in: {_projectConfig.TestsFolder}", ConsoleColor.Green), Times.Once);
            _mockConsoleService.Verify(m => m.WriteColored("Auto-detect mode activated. Monitoring code changes, press any key to exit.", ConsoleColor.Blue), Times.Once);
            _mockCodeMonitor.Verify(m => m.StartAsync(_projectConfig.SrcFolder, _projectConfig.TestsFolder, _projectConfig.SampleUnitTestContent, false, _cancellationToken), Times.Once);
        }

        [Fact]
        public async Task RunManualModeAsync_Should_Exit_When_User_Types_Exit()
        {
            // Arrange
            _mockConsoleService.SetupSequence(m => m.Prompt(It.IsAny<string>(), ConsoleColor.Yellow))
                               .Returns("exit"); // User immediately exits

            // Act
            await _modeRunnerService.RunManualModeAsync();

            // Assert
            _mockConsoleService.Verify(m => m.Prompt(It.IsAny<string>(), ConsoleColor.Yellow), Times.Once);
        }

        [Fact]
        public async Task RunManualModeAsync_Should_Continue_On_Invalid_FilePath()
        {
            // Arrange
            _mockConsoleService.SetupSequence(m => m.Prompt(It.IsAny<string>(), ConsoleColor.Yellow))
                               .Returns("invalid-path")
                               .Returns("exit");

            _mockConsoleService.Setup(m => m.WriteColored("Invalid file path. Please try again.", ConsoleColor.Red));

            // Act
            await _modeRunnerService.RunManualModeAsync();

            // Assert
            _mockConsoleService.Verify(m => m.WriteColored("Invalid file path. Please try again.", ConsoleColor.Red), Times.Once);
        }

        [Fact]
        public async Task RunManualModeAsync_Should_Process_File_Change_And_Approve_Update()
        {
            // Arrange
            string validFilePath = Path.GetTempFileName();
            var testResult = new TestGenerationResultModel { TempFilePath = "temp-test.cs", TestFilePath = "final-test.cs" };

            _mockConsoleService.SetupSequence(m => m.Prompt(It.IsAny<string>(), ConsoleColor.Yellow))
                               .Returns(validFilePath) // Valid file path
                               .Returns("y") // Approve test update
                               .Returns("exit"); // Exit loop

            _mockTestUpdater.Setup(m => m.ProcessFileChangeAsync(_projectConfig.SrcFolder, _projectConfig.TestsFolder, validFilePath, _projectConfig.SampleUnitTestContent, true, _cancellationToken))
                            .ReturnsAsync(testResult);

            // Act
            await _modeRunnerService.RunManualModeAsync();

            // Assert
            _mockTestUpdater.Verify(m => m.FinalizeTestUpdate(testResult), Times.Once);
            _mockConsoleService.Verify(m => m.WriteColored($"Test file updated at: {testResult.TestFilePath}", ConsoleColor.Green), Times.Once);
        }

        [Fact]
        public async Task RunManualModeAsync_Should_Not_Apply_Changes_If_Not_Approved()
        {
            // Arrange
            string validFilePath = Path.GetTempFileName();
            var testResult = new TestGenerationResultModel { TempFilePath = "temp-test.cs", TestFilePath = "final-test.cs" };

            _mockConsoleService.SetupSequence(m => m.Prompt(It.IsAny<string>(), ConsoleColor.Yellow))
                               .Returns(validFilePath) // Valid file path
                               .Returns("n") // Reject update
                               .Returns("exit"); // Exit loop

            _mockTestUpdater.Setup(m => m.ProcessFileChangeAsync(_projectConfig.SrcFolder, _projectConfig.TestsFolder, validFilePath, _projectConfig.SampleUnitTestContent, true, _cancellationToken))
                            .ReturnsAsync(testResult);

            // Act
            await _modeRunnerService.RunManualModeAsync();

            // Assert
            _mockTestUpdater.Verify(m => m.FinalizeTestUpdate(It.IsAny<TestGenerationResultModel>()), Times.Never);
            _mockConsoleService.Verify(m => m.WriteColored("Changes were not applied. You may manually copy any necessary code from the temporary file.", ConsoleColor.Red), Times.Once);
        }
    }
}
