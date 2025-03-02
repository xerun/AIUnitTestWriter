using AIUnitTestWriter.Services.Interfaces;

namespace AIUnitTestWriter.Services
{
    public class CodeMonitor : ICodeMonitor
    {
        private readonly ITestUpdater _testUpdater;
        private readonly IFileWatcherWrapper _fileWatcher;
        private readonly Dictionary<string, Timer> _debounceTimers = new();
        private readonly object _timerLock = new();
        private readonly TimeSpan _debounceInterval = TimeSpan.FromSeconds(1);

        private string _srcFolder = string.Empty;
        private string _testsFolder = string.Empty;
        private string _sampleUnitTest = string.Empty;
        private bool _promptUser = true;

        public CodeMonitor(ITestUpdater testUpdater, IFileWatcherWrapper fileWatcher)
        {
            _testUpdater = testUpdater ?? throw new ArgumentNullException(nameof(testUpdater));
            _fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));
        }

        public void Start(string srcFolder, string testsFolder, string sampleUnitTest = "", bool promptUser = true)
        {
            _srcFolder = srcFolder;
            _testsFolder = testsFolder;
            _sampleUnitTest = sampleUnitTest;
            _promptUser = promptUser;

            _fileWatcher.Changed += OnChanged;
            _fileWatcher.Created += OnChanged;
            _fileWatcher.Renamed += OnRenamed;

            _fileWatcher.Start(_srcFolder, "*.cs");
            _fileWatcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            _fileWatcher.EnableRaisingEvents = false;
            _fileWatcher.Stop();
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            Debounce(e.FullPath, "change");
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            Debounce(e.FullPath, "rename");
        }

        private void Debounce(string filePath, string eventType)
        {
            lock (_timerLock)
            {
                if (_debounceTimers.TryGetValue(filePath, out var existingTimer))
                {
                    existingTimer.Change(_debounceInterval, Timeout.InfiniteTimeSpan);
                }
                else
                {
                    var timer = new Timer(_ =>
                    {
                        try
                        {
                            Console.WriteLine($"Processing {eventType} for file: {filePath}");
                            _testUpdater.ProcessFileChange(_srcFolder, _testsFolder, filePath, _sampleUnitTest, _promptUser);
                        }
                        finally
                        {
                            lock (_timerLock) { _debounceTimers.Remove(filePath); }
                        }
                    }, null, _debounceInterval, Timeout.InfiniteTimeSpan);

                    _debounceTimers[filePath] = timer;
                }
            }
        }
    }
}
