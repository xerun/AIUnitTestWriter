using AIUnitTestWriter.Services.Interfaces;

namespace AIUnitTestWriter.Services
{
    public class CodeMonitor : ICodeMonitor
    {
        private readonly ITestUpdater _testUpdater;
        private FileSystemWatcher? _watcher;

        // Dictionary to hold timers for each file path.
        private readonly Dictionary<string, Timer> _debounceTimers = new();

        // Debounce interval: adjust as needed.
        private readonly TimeSpan _debounceInterval = TimeSpan.FromSeconds(1);

        // Lock object for thread safety.
        private readonly object _timerLock = new();

        // Configuration variables set in Start()
        private string _srcFolder = string.Empty;
        private string _testsFolder = string.Empty;
        private string _sampleUnitTest = string.Empty;
        private bool _promptUser = true;

        public CodeMonitor(ITestUpdater testUpdater)
        {
            _testUpdater = testUpdater;
        }

        /// <inheritdoc/>
        public void Start(string srcFolder, string testsFolder, string sampleUnitTest = "", bool promptUser = true)
        {
            _srcFolder = srcFolder;
            _testsFolder = testsFolder;
            _sampleUnitTest = sampleUnitTest;
            _promptUser = promptUser;

            _watcher = new FileSystemWatcher(_srcFolder, "*.cs")
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
            };

            _watcher.Changed += OnChanged;
            _watcher.Created += OnChanged;
            _watcher.Renamed += OnRenamed;
            _watcher.EnableRaisingEvents = true;
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
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
                    // Reset the existing timer.
                    existingTimer.Change(_debounceInterval, Timeout.InfiniteTimeSpan);
                }
                else
                {
                    // Create a new timer that fires once after the debounce interval.
                    Timer timer = new Timer(_ =>
                    {
                        try
                        {
                            Console.WriteLine($"Processing {eventType} for file: {filePath}");
                            _testUpdater.ProcessFileChange(_srcFolder, _testsFolder, filePath, _sampleUnitTest, _promptUser);
                        }
                        finally
                        {
                            // Remove the timer after processing.
                            lock (_timerLock)
                            {
                                _debounceTimers.Remove(filePath);
                            }
                        }
                    }, null, _debounceInterval, Timeout.InfiniteTimeSpan);

                    _debounceTimers[filePath] = timer;
                }
            }
        }
    }
}
