namespace AIUnitTestWriter.Interfaces
{
    public interface ICodeMonitor
    {
        /// <summary>
        /// Starts monitoring the source folder for file changes.
        /// </summary>
        void Start(string srcFolder, string testsFolder, string sampleUnitTest = "", bool promptUser = true);

        /// <summary>
        /// Stops monitoring and cleans up resources.
        /// </summary>
        void Stop();
    }
}
