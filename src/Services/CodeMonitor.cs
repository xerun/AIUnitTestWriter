using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.SettingOptions;
using AIUnitTestWriter.Wrappers;
using Microsoft.Extensions.Options;

namespace AIUnitTestWriter.Services
{
    public class CodeMonitor : ICodeMonitor
    {
        private readonly ITestUpdaterService _testUpdater;
        private readonly IFileWatcherWrapper _fileWatcher;
        private readonly Dictionary<string, CancellationTokenSource> _debounceTokens = new();
        private readonly object _timerLock = new();
        private readonly TimeSpan _debounceInterval = TimeSpan.FromSeconds(1);
        private readonly IConsoleService _consoleService;
        private readonly ProjectSettings _projectSettings;

        private string _srcFolder = string.Empty;
        private string _testsFolder = string.Empty;
        private string _sampleUnitTest = string.Empty;
        private string _codeFileextension = string.Empty;
        private bool _promptUser = true;

        public CodeMonitor(ITestUpdaterService testUpdater, IFileWatcherWrapper fileWatcher, IConsoleService consoleService, IOptions<ProjectSettings> projectSettings)
        {
            _testUpdater = testUpdater ?? throw new ArgumentNullException(nameof(testUpdater));
            _fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));
            _consoleService = consoleService ?? throw new ArgumentNullException(nameof(consoleService));
            _projectSettings = projectSettings?.Value ?? throw new ArgumentNullException(nameof(projectSettings));
            _codeFileextension = _projectSettings.CodeFileExtension ?? throw new ArgumentNullException(nameof(_projectSettings.CodeFileExtension));
        }

        public Task StartAsync(
            string srcFolder,
            string testsFolder,
            string sampleUnitTest = "",
            bool promptUser = true,
            CancellationToken cancellationToken = default)
        {
            _srcFolder = srcFolder;
            _testsFolder = testsFolder;
            _sampleUnitTest = sampleUnitTest;
            _promptUser = promptUser;

            _fileWatcher.Changed += async (s, e) => await OnChanged(e, cancellationToken);
            _fileWatcher.Created += async (s, e) => await OnChanged(e, cancellationToken);
            _fileWatcher.Renamed += async (s, e) => await OnRenamed(e, cancellationToken);

            _fileWatcher.Start(_srcFolder, $"*{_codeFileextension}");
            _fileWatcher.EnableRaisingEvents = true;

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _fileWatcher.EnableRaisingEvents = false;
            _fileWatcher.Stop();

            // Cancel all ongoing tasks
            lock (_timerLock)
            {
                foreach (var tokenSource in _debounceTokens.Values)
                {
                    tokenSource.Cancel();
                    tokenSource.Dispose();
                }
                _debounceTokens.Clear();
            }

            return Task.CompletedTask;
        }

        internal async Task OnChanged(FileSystemEventArgs e, CancellationToken cancellationToken)
        {
            await DebounceAsync(e.FullPath, "change", cancellationToken);
        }

        internal async Task OnRenamed(RenamedEventArgs e, CancellationToken cancellationToken)
        {
            await DebounceAsync(e.FullPath, "rename", cancellationToken);
        }

        private async Task DebounceAsync(string filePath, string eventType, CancellationToken cancellationToken)
        {
            lock (_timerLock)
            {
                if (_debounceTokens.TryGetValue(filePath, out var existingTokenSource))
                {
                    existingTokenSource.Cancel();
                    existingTokenSource.Dispose();
                }

                var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _debounceTokens[filePath] = cts;
            }

            try
            {
                await Task.Delay(_debounceInterval, _debounceTokens[filePath].Token);

                lock (_timerLock)
                {
                    _debounceTokens.Remove(filePath);
                }

                _consoleService.WriteColored($"Processing {eventType} for file: {filePath}", ConsoleColor.Blue);

                await _testUpdater.ProcessFileChangeAsync(
                    _srcFolder,
                    _testsFolder,
                    filePath,
                    _sampleUnitTest,
                    _promptUser,
                    cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Expected if a new event comes in before the debounce interval ends
                _consoleService.WriteColored($"Debounced {eventType} for file: {filePath}", ConsoleColor.DarkGray);
            }
            catch (Exception ex)
            {
                _consoleService.WriteColored($"Error processing file change: {ex.Message}", ConsoleColor.Red);
            }
        }
    }
}
