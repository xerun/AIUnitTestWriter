using Octokit;

namespace AIUnitTestWriter.Services.Interfaces
{
    public interface IGitHubClientWrapper
    {
        Task<PullRequest> CreatePullRequestAsync(string owner, string repo, NewPullRequest pr);
    }
}
