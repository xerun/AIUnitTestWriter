using AIUnitTestWriter.DTOs;
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
        private readonly IConcurrentDictionaryWrapper<string, CancellationTokenSource> _debounceTokens;
        private readonly IConcurrentDictionaryWrapper<string, string> _fileCache;
        private readonly object _timerLock = new();
        private readonly TimeSpan _debounceInterval = TimeSpan.FromSeconds(1);
        private readonly IConsoleService _consoleService;
        private readonly ProjectSettings _projectSettings;

        private string _projectPath = string.Empty;
        private string _srcFolder = string.Empty;
        private string _testsFolder = string.Empty;
        private string _sampleUnitTest = string.Empty;
        private string _codeFileExtension = string.Empty;
        private bool _promptUser = true;

        public CodeMonitor(
            ITestUpdaterService testUpdater,
            IFileWatcherWrapper fileWatcher,
            IConsoleService consoleService,
            IOptions<ProjectSettings> projectSettings)
        {
            _testUpdater = testUpdater ?? throw new ArgumentNullException(nameof(testUpdater));
            _fileWatcher = fileWatcher ?? throw new ArgumentNullException(nameof(fileWatcher));
            _consoleService = consoleService ?? throw new ArgumentNullException(nameof(consoleService));
            _projectSettings = projectSettings?.Value ?? throw new ArgumentNullException(nameof(projectSettings));
            _codeFileExtension = _projectSettings.CodeFileExtension ?? throw new ArgumentNullException(nameof(_projectSettings.CodeFileExtension));
            _debounceTokens = new ConcurrentDictionaryWrapper<string, CancellationTokenSource>();
            _fileCache = new ConcurrentDictionaryWrapper<string, string>();
        }

        public Task StartAsync(
            string projectPath,
            string srcFolder,
            string testsFolder,
            string sampleUnitTest = "",
            bool promptUser = true,
            CancellationToken cancellationToken = default)
        {
            _projectPath = projectPath;
            _srcFolder = srcFolder;
            _testsFolder = testsFolder;
            _sampleUnitTest = sampleUnitTest;
            _promptUser = promptUser;

            _fileWatcher.Changed += async (s, e) => await OnChanged(e, cancellationToken);
            _fileWatcher.Created += async (s, e) => await OnChanged(e, cancellationToken);
            _fileWatcher.Renamed += async (s, e) => await OnRenamed(e, cancellationToken);

            _fileWatcher.Start(_projectPath, _srcFolder, $"*{_codeFileExtension}");
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
                foreach (var kvp in _debounceTokens.GetAll())
                {
                    kvp.Value.Cancel();
                    kvp.Value.Dispose();
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
            if (_debounceTokens.TryGetValue(filePath, out var existingTokenSource))
            {
                existingTokenSource.Cancel();
                existingTokenSource.Dispose();
            }

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _debounceTokens[filePath] = cts;

            try
            {
                await Task.Delay(_debounceInterval, cts.Token);
                _debounceTokens.TryRemove(filePath, out var _);

                _consoleService.WriteColored($"Processing {eventType} for file: {filePath}", ConsoleColor.Blue);

                var oldContent = _fileCache.TryGetValue(filePath, out var cached) ? cached : string.Empty;
                var newContent = File.ReadAllText(filePath);
                _fileCache[filePath] = newContent;

                var fileChangeProcessingDto = new FileChangeProcessingDto(
                    filePath, oldContent, newContent, _codeFileExtension, _sampleUnitTest, string.Empty, _promptUser, _projectPath, _srcFolder, _testsFolder
                );

                await _testUpdater.ProcessFileChangeAsync(fileChangeProcessingDto, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                _consoleService.WriteColored($"Debounced {eventType} for file: {filePath}", ConsoleColor.DarkGray);
            }
            catch (Exception ex)
            {
                _consoleService.WriteColored($"Error processing file change: {ex.Message}", ConsoleColor.Red);
            }
        }
    }
}
