using AIUnitTestWriter.Wrappers.Git;
using Octokit;

namespace AIUnitTestWriter.Interfaces
{
    public interface IGitHubClientWrapper
    {
        Task<GitHubCommit?> GetLatestCommitAsync(string owner, string repo, string branch, CancellationToken cancellationToken = default);
        Task<IEnumerable<GitCommitChange>> GetCommitChangesAsync(string owner, string repo, string commitSha, CancellationToken cancellationToken = default);
        Task<string> GetFileContentAsync(string owner, string repo, string filePath, string commitSha, CancellationToken cancellationToken = default);
        Task CommitFileAsync(string owner, string repo, string branch, string filePath, string fileContent, string commitMessage, CancellationToken cancellationToken);
        Task CreateBranchAsync(string owner, string repo, string branchName, string baseBranch, CancellationToken cancellationToken = default);
        Task<PullRequest> CreatePullRequestAsync(string owner, string repo, NewPullRequest pr, CancellationToken cancellationToken = default);
    }
}
