using AIUnitTestWriter.Models;
using AIUnitTestWriter.Services.Interfaces;
using Moq;

namespace AIUnitTestWriter.UnitTests
{
    public class AppStarterTests
    {
        private readonly Mock<IModeRunner> _modeRunnerMock;
        private readonly Mock<IGitMonitorService> _gitIntegrationServiceMock;
        private readonly Mock<IConsoleService> _consoleServiceMock;
        private readonly ProjectConfigModel _projectConfig;
        private readonly AppStarter _appStarter;

        public AppStarterTests()
        {
            _modeRunnerMock = new Mock<IModeRunner>();
            _gitIntegrationServiceMock = new Mock<IGitMonitorService>();
            _consoleServiceMock = new Mock<IConsoleService>();
            _projectConfig = new ProjectConfigModel();

            _appStarter = new AppStarter(
                _gitIntegrationServiceMock.Object,
                _modeRunnerMock.Object,
                _consoleServiceMock.Object,
                _projectConfig
            );
        }

        [Fact]
        public async Task RunAsync_Should_UseGitIntegrationService_When_GitRepository()
        {
            // Arrange
            _projectConfig.IsGitRepository = true;
            _consoleServiceMock.Setup(s => s.ReadLine()).Returns(string.Empty);
            // Act
            await _appStarter.RunAsync();

            // Assert
            _consoleServiceMock.Verify(c => c.WriteColored("Git repository mode detected.", ConsoleColor.Green), Times.Once);
            _gitIntegrationServiceMock.Verify(g => g.MonitorAndTriggerAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunAsync_Should_RunAutoMode_When_AutoModeSelected()
        {
            // Arrange
            _projectConfig.IsGitRepository = false;
            _consoleServiceMock.Setup(c => c.Prompt(It.IsAny<string>(), It.IsAny<ConsoleColor>()))
                .Returns("A");

            // Act
            await _appStarter.RunAsync();

            // Assert
            _modeRunnerMock.Verify(m => m.RunAutoModeAsync(), Times.Once);
        }

        [Fact]
        public async Task RunAsync_Should_RunManualMode_When_ManualModeSelected()
        {
            // Arrange
            _projectConfig.IsGitRepository = false;
            _consoleServiceMock.Setup(c => c.Prompt(It.IsAny<string>(), It.IsAny<ConsoleColor>()))
                .Returns("M");

            // Act
            await _appStarter.RunAsync();

            // Assert
            _modeRunnerMock.Verify(m => m.RunManualModeAsync(), Times.Once);
        }

        [Fact]
        public async Task RunAsync_Should_DisplayError_When_InvalidModeSelected()
        {
            // Arrange
            _projectConfig.IsGitRepository = false;
            _consoleServiceMock.Setup(c => c.Prompt(It.IsAny<string>(), It.IsAny<ConsoleColor>()))
                .Returns("invalid");

            // Act
            await _appStarter.RunAsync();

            // Assert
            _consoleServiceMock.Verify(c => c.WriteColored("Invalid mode selected. Exiting program.", ConsoleColor.Red), Times.Once);
        }
    }
}