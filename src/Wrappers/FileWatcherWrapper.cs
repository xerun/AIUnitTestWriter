using AIUnitTestWriter.Interfaces;

namespace AIUnitTestWriter.Wrappers
{
    public class FileWatcherWrapper : IFileWatcherWrapper
    {
        private FileSystemWatcher _watcher;

        public event FileSystemEventHandler Changed;
        public event FileSystemEventHandler Created;
        public event RenamedEventHandler Renamed;

        public bool EnableRaisingEvents
        {
            get => _watcher.EnableRaisingEvents;
            set => _watcher.EnableRaisingEvents = value;
        }

        public void Start(string projectPath, string srcFolder, string filter)
        {
            string path = Path.Combine(projectPath, srcFolder);
            _watcher = new FileSystemWatcher(path, filter)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
            };

            _watcher.Changed += (s, e) => Changed?.Invoke(s, e);
            _watcher.Created += (s, e) => Created?.Invoke(s, e);
            _watcher.Renamed += (s, e) => Renamed?.Invoke(s, e);
        }

        public void Stop()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }
        }
    }
}
