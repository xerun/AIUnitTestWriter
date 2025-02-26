using AIUnitTestWriter.Models;
using AIUnitTestWriter.Services.Interfaces;
using Moq;

namespace AIUnitTestWriter.UnitTests
{
    public class AppStarterTests
    {
        private readonly Mock<IProjectInitializer> _mockProjectInitializer;
        private readonly Mock<IModeRunner> _mockModeRunner;
        private readonly Mock<IGitIntegrationService> _mockGitIntegrationService;
        private readonly Mock<IConsoleService> _mockConsoleService;
        private readonly AppStarter _appStarter;

        public AppStarterTests()
        {
            _mockProjectInitializer = new Mock<IProjectInitializer>();
            _mockModeRunner = new Mock<IModeRunner>();
            _mockGitIntegrationService = new Mock<IGitIntegrationService>();
            _mockConsoleService = new Mock<IConsoleService>();

            _appStarter = new AppStarter(
                _mockGitIntegrationService.Object,
                _mockProjectInitializer.Object,
                _mockModeRunner.Object,
                _mockConsoleService.Object
            );
        }

        [Fact]
        public async Task RunAsync_ShouldCallGitIntegrationService_WhenProjectIsGitRepo()
        {
            // Arrange
            var projectConfig = new ProjectConfigModel { IsGitRepository = true };
            _mockProjectInitializer.Setup(p => p.Initialize()).Returns(projectConfig);

            // Act
            await _appStarter.RunAsync();

            // Assert
            _mockGitIntegrationService.Verify(g => g.MonitorAndTriggerAsync(projectConfig), Times.Once);
        }

        [Fact]
        public async Task RunAsync_ShouldRunAutoMode_WhenUserChoosesAuto()
        {
            // Arrange
            var projectConfig = new ProjectConfigModel { IsGitRepository = false };
            _mockProjectInitializer.Setup(p => p.Initialize()).Returns(projectConfig);
            _mockConsoleService.Setup(c => c.Prompt(It.IsAny<string>(), ConsoleColor.Cyan)).Returns("a");

            // Act
            await _appStarter.RunAsync();

            // Assert
            _mockModeRunner.Verify(m => m.RunAutoModeAsync(projectConfig), Times.Once);
        }

        [Fact]
        public async Task RunAsync_ShouldRunManualMode_WhenUserChoosesManual()
        {
            // Arrange
            var projectConfig = new ProjectConfigModel { IsGitRepository = false };
            _mockProjectInitializer.Setup(p => p.Initialize()).Returns(projectConfig);
            _mockConsoleService.Setup(c => c.Prompt(It.IsAny<string>(), ConsoleColor.Cyan)).Returns("m");

            // Act
            await _appStarter.RunAsync();

            // Assert
            _mockModeRunner.Verify(m => m.RunManualModeAsync(projectConfig), Times.Once);
        }

        [Fact]
        public async Task RunAsync_ShouldExit_WhenUserChoosesInvalidOption()
        {
            // Arrange
            var projectConfig = new ProjectConfigModel { IsGitRepository = false };
            _mockProjectInitializer.Setup(p => p.Initialize()).Returns(projectConfig);
            _mockConsoleService.Setup(c => c.Prompt(It.IsAny<string>(), ConsoleColor.Cyan)).Returns("invalid");

            // Act
            await _appStarter.RunAsync();

            // Assert
            _mockModeRunner.Verify(m => m.RunAutoModeAsync(It.IsAny<ProjectConfigModel>()), Times.Never);
            _mockModeRunner.Verify(m => m.RunManualModeAsync(It.IsAny<ProjectConfigModel>()), Times.Never);
        }
    }
}