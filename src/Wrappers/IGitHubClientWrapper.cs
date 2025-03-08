using Octokit;

namespace AIUnitTestWriter.Wrappers
{
    public interface IGitHubClientWrapper
    {
        Task<PullRequest> CreatePullRequestAsync(string owner, string repo, NewPullRequest pr);
    }
}
