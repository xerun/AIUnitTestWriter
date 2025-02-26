using AIUnitTestWriter.Models;

namespace AIUnitTestWriter.Services.Interfaces
{
    public interface IGitIntegrationService
    {
        /// <summary>
        /// Monitors the repository for new changes in .cs files and triggers test generation.
        /// </summary>
        Task MonitorAndTriggerAsync(ProjectConfigModel config);

        /// <summary>
        /// Creates a pull request with the latest test updates.
        /// </summary>
        /// <param name="branchName">The branch name for the update.</param>
        /// <param name="prTitle">Title for the pull request.</param>
        /// <param name="prBody">Description for the pull request.</param>
        Task CreatePullRequestAsync(string branchName, string prTitle, string prBody);
    }
}
