namespace AIUnitTestWriter.Interfaces
{
    public interface ICodeMonitor
    {
        /// <summary>
        /// Starts monitoring the source folder for file changes.
        /// </summary>
        Task StartAsync(string projectPath, string srcFolder, string testsFolder, string sampleUnitTest = "", bool promptUser = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops monitoring and cleans up resources.
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken = default);
    }
}
