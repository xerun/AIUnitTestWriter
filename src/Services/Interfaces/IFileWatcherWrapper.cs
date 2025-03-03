namespace AIUnitTestWriter.Services.Interfaces
{
    public interface IFileWatcherWrapper
    {
        event FileSystemEventHandler Changed;
        event FileSystemEventHandler Created;
        event RenamedEventHandler Renamed;

        bool EnableRaisingEvents { get; set; }

        void Start(string path, string filter);
        void Stop();
    }
}
