namespace AIUnitTestWriter.Interfaces
{
    public interface IGitMonitorService
    {
        /// <summary>
        /// Clones the repository if it doesn't exist, pulls latest changes, and monitors for file modifications.
        /// </summary>
        Task MonitorAndTriggerAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a pull request after detecting and committing changes.
        /// </summary>
        Task CreatePullRequestAsync(string branchName, CancellationToken cancellationToken = default);
    }
}
