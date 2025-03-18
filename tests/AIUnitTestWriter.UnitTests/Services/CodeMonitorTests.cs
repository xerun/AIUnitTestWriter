using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.Services;
using AIUnitTestWriter.SettingOptions;
using AIUnitTestWriter.Wrappers;
using Microsoft.Extensions.Options;
using Moq;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class CodeMonitorTests
    {
        private readonly CancellationToken _cancellationToken = CancellationToken.None;
        private readonly Mock<ITestUpdaterService> _mockTestUpdater;
        private readonly Mock<IFileWatcherWrapper> _mockFileWatcher;
        private readonly Mock<IConsoleService> _mockConsoleService;
        private readonly IOptions<ProjectSettings> _projectSettings;
        private readonly CodeMonitor _codeMonitor;

        public CodeMonitorTests()
        {
            _mockTestUpdater = new Mock<ITestUpdaterService>();
            _mockFileWatcher = new Mock<IFileWatcherWrapper>();
            _mockConsoleService = new Mock<IConsoleService>();
            _projectSettings = Options.Create(new ProjectSettings
            {
                CodeFileExtension = ".cs"
            });

            _codeMonitor = new CodeMonitor(_mockTestUpdater.Object, _mockFileWatcher.Object, _mockConsoleService.Object, _projectSettings);
        }

        [Fact]
        public void Start_ShouldEnableFileWatcher()
        {
            // Act
            _codeMonitor.StartAsync("srcPath", "testPath", cancellationToken: _cancellationToken);

            // Assert
            _mockFileWatcher.Verify(fw => fw.Start("srcPath", $"*.cs"), Times.Once);
            _mockFileWatcher.VerifySet(fw => fw.EnableRaisingEvents = true, Times.Once);
        }

        [Fact]
        public void Stop_ShouldDisableFileWatcher()
        {
            // Act
            _codeMonitor.StopAsync(_cancellationToken);

            // Assert
            _mockFileWatcher.VerifySet(fw => fw.EnableRaisingEvents = false, Times.Once);
            _mockFileWatcher.Verify(fw => fw.Stop(), Times.Once);
        }

        [Fact]
        public async Task FileChanged_ShouldTriggerProcessFileChange()
        {
            // Arrange
            await _codeMonitor.StartAsync("srcPath", "testPath", cancellationToken: _cancellationToken);
            var fileEvent = new FileSystemEventArgs(WatcherChangeTypes.Changed, "srcPath", "file.cs");

            _mockTestUpdater.Setup(tu => tu.ProcessFileChangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), _cancellationToken));

            // Act
            await _codeMonitor.OnChanged(fileEvent, _cancellationToken);

            // Wait for debounce interval
            await Task.Delay(1200);

            // Assert
            _mockTestUpdater.Verify(tu => tu.ProcessFileChangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), _cancellationToken), Times.Once);

            await _codeMonitor.StopAsync(_cancellationToken);
        }

        [Fact]
        public async Task FileRenamed_ShouldTriggerProcessFileChange()
        {
            // Arrange
            await _codeMonitor.StartAsync("srcPath", "testPath", cancellationToken: _cancellationToken);
            var renameEvent = new RenamedEventArgs(WatcherChangeTypes.Renamed, "srcPath", "newFile.cs", "oldFile.cs");

            _mockTestUpdater.Setup(tu => tu.ProcessFileChangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), _cancellationToken));

            // Act
            await _codeMonitor.OnRenamed(renameEvent, _cancellationToken);

            // Wait for debounce interval
            await Task.Delay(1200);

            // Assert
            _mockTestUpdater.Verify(tu => tu.ProcessFileChangeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), _cancellationToken), Times.Once);

            await _codeMonitor.StopAsync(_cancellationToken);
        }
    }
}
