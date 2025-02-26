using AIUnitTestWriter.Services;
using AIUnitTestWriter.Services.Interfaces;
using Moq;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class CodeMonitorTests
    {
        private readonly Mock<ITestUpdater> _testUpdaterMock;
        private readonly CodeMonitor _codeMonitor;

        public CodeMonitorTests()
        {
            _testUpdaterMock = new Mock<ITestUpdater>();
            _codeMonitor = new CodeMonitor(_testUpdaterMock.Object);
        }

        [Fact]
        public void Start_ShouldInitializeFileSystemWatcher()
        {
            // Act
            _codeMonitor.Start("src", "tests");

            // Assert
            Assert.NotNull(GetPrivateField<FileSystemWatcher>(_codeMonitor, "_watcher"));
        }

        [Fact]
        public void Stop_ShouldDisposeFileSystemWatcher()
        {
            // Arrange
            _codeMonitor.Start("src", "tests");

            // Act
            _codeMonitor.Stop();

            // Assert
            Assert.Null(GetPrivateField<FileSystemWatcher>(_codeMonitor, "_watcher"));
        }

        [Fact]
        public void OnChanged_ShouldTriggerDebounceAndProcessFileChange()
        {
            // Arrange
            _codeMonitor.Start("src", "tests");
            string testFilePath = "src/TestFile.cs";

            // Act
            TriggerFileSystemEvent("_watcher", new FileSystemEventArgs(WatcherChangeTypes.Changed, "src", "TestFile.cs"));

            // Wait for debounce timer
            Thread.Sleep(1500);

            // Assert: Ensure `ProcessFileChange` was called once
            _testUpdaterMock.Verify(x => x.ProcessFileChange("src", "tests", testFilePath, "", true), Times.Once);
        }

        [Fact]
        public void OnRenamed_ShouldTriggerDebounceAndProcessFileChange()
        {
            // Arrange
            _codeMonitor.Start("src", "tests");
            string testFilePath = "src/TestFileRenamed.cs";

            // Act
            TriggerFileSystemEvent("_watcher", new RenamedEventArgs(WatcherChangeTypes.Renamed, "src", "TestFileRenamed.cs", "TestFile.cs"));

            // Wait for debounce timer
            Thread.Sleep(1500);

            // Assert: Ensure `ProcessFileChange` was called once
            _testUpdaterMock.Verify(x => x.ProcessFileChange("src", "tests", testFilePath, "", true), Times.Once);
        }

        [Fact]
        public void Debounce_ShouldLimitMultipleRapidCallsToSingleExecution()
        {
            // Arrange
            _codeMonitor.Start("src", "tests");
            string testFilePath = "src/TestFile.cs";

            // Trigger multiple rapid events
            TriggerFileSystemEvent("_watcher", new FileSystemEventArgs(WatcherChangeTypes.Changed, "src", "TestFile.cs"));
            TriggerFileSystemEvent("_watcher", new FileSystemEventArgs(WatcherChangeTypes.Changed, "src", "TestFile.cs"));
            TriggerFileSystemEvent("_watcher", new FileSystemEventArgs(WatcherChangeTypes.Changed, "src", "TestFile.cs"));

            // Wait for debounce timer
            Thread.Sleep(1500);

            // Assert: Ensure `ProcessFileChange` was called only once
            _testUpdaterMock.Verify(x => x.ProcessFileChange("src", "tests", testFilePath, "", true), Times.Once);
        }

        private void TriggerFileSystemEvent(string watcherField, FileSystemEventArgs args)
        {
            var watcher = GetPrivateField<FileSystemWatcher>(_codeMonitor, watcherField);
            if (watcher == null) return;

            // Use reflection to get the event field
            var eventField = typeof(FileSystemWatcher)
                .GetField(args.ChangeType.ToString(),
                          System.Reflection.BindingFlags.Instance |
                          System.Reflection.BindingFlags.NonPublic);

            var eventDelegate = eventField?.GetValue(watcher) as MulticastDelegate;

            // Invoke the event if it exists
            eventDelegate?.Method.Invoke(eventDelegate.Target, new object[] { watcher, args });
        }

        private T GetPrivateField<T>(object obj, string fieldName)
        {
            return (T)obj.GetType()
                         .GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                         .GetValue(obj);
        }
    }
}
