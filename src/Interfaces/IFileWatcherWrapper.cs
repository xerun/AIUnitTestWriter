namespace AIUnitTestWriter.Interfaces
{
    public interface IFileWatcherWrapper
    {
        event FileSystemEventHandler Changed;
        event FileSystemEventHandler Created;
        event RenamedEventHandler Renamed;

        bool EnableRaisingEvents { get; set; }

        void Start(string projectPath, string srcFolder, string filter);
        void Stop();
    }
}
