using AIUnitTestWriter.Services;
using AIUnitTestWriter.Services.Interfaces;
using Moq;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class CodeMonitorTests
    {
        private readonly Mock<ITestUpdater> _mockTestUpdater;
        private readonly Mock<IFileWatcherWrapper> _mockFileWatcher;
        private readonly CodeMonitor _codeMonitor;

        public CodeMonitorTests()
        {
            _mockTestUpdater = new Mock<ITestUpdater>();
            _mockFileWatcher = new Mock<IFileWatcherWrapper>();
            _codeMonitor = new CodeMonitor(_mockTestUpdater.Object, _mockFileWatcher.Object);
        }

        [Fact]
        public void Start_ShouldEnableFileWatcher()
        {
            // Act
            _codeMonitor.Start("srcPath", "testPath");

            // Assert
            _mockFileWatcher.Verify(fw => fw.Start("srcPath", "*.cs"), Times.Once);
            _mockFileWatcher.VerifySet(fw => fw.EnableRaisingEvents = true, Times.Once);
        }

        [Fact]
        public void Stop_ShouldDisableFileWatcher()
        {
            // Act
            _codeMonitor.Stop();

            // Assert
            _mockFileWatcher.VerifySet(fw => fw.EnableRaisingEvents = false, Times.Once);
            _mockFileWatcher.Verify(fw => fw.Stop(), Times.Once);
        }

        [Fact]
        public async Task FileChanged_ShouldTriggerProcessFileChange()
        {
            // Arrange
            _codeMonitor.Start("srcPath", "testPath");
            var fileEvent = new FileSystemEventArgs(WatcherChangeTypes.Changed, "srcPath", "file.cs");

            _mockTestUpdater.Setup(tu => tu.ProcessFileChange(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()));

            // Act
            Task.Run(() => _codeMonitor.GetType().GetMethod("OnChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_codeMonitor, new object[] { this, fileEvent }));

            // Wait for debounce interval
            await Task.Delay(1200);

            // Assert
            _mockTestUpdater.Verify(tu => tu.ProcessFileChange(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);

            _codeMonitor.Stop();
        }

        [Fact]
        public async Task FileRenamed_ShouldTriggerProcessFileChange()
        {
            // Arrange
            _codeMonitor.Start("srcPath", "testPath");
            var renameEvent = new RenamedEventArgs(WatcherChangeTypes.Renamed, "srcPath", "newFile.cs", "oldFile.cs");

            _mockTestUpdater.Setup(tu => tu.ProcessFileChange(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()));

            // Act
            Task.Run(() => _codeMonitor.GetType().GetMethod("OnRenamed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_codeMonitor, new object[] { this, renameEvent }));

            // Wait for debounce interval
            await Task.Delay(1200);

            // Assert
            _mockTestUpdater.Verify(tu => tu.ProcessFileChange(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);

            _codeMonitor.Stop();
        }
    }
}
