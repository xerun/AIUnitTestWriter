using AIUnitTestWriter.SettingOptions;
using Microsoft.Extensions.Options;
using Octokit;

namespace AIUnitTestWriter.Wrappers
{
    public class GitHubClientWrapper : IGitHubClientWrapper
    {
        private readonly GitHubClient _gitHubClient;
        private readonly GitSettings _gitSettings;

        public GitHubClientWrapper(IOptions<GitSettings> gitSetting)
        {
            _gitSettings = gitSetting?.Value ?? throw new ArgumentNullException(nameof(gitSetting));
            _gitHubClient = new GitHubClient(new ProductHeaderValue("AIUnitTestWriter"))
            {
                Credentials = new Credentials(_gitSettings.GitHubToken)
            };
        }

        public async Task<PullRequest> CreatePullRequestAsync(string owner, string repo, NewPullRequest pr, CancellationToken cancellationToken = default)
        {
            return await _gitHubClient.PullRequest.Create(owner, repo, pr);
        }
    }
}
