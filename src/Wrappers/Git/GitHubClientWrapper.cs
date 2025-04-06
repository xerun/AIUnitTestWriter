using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.Services;
using AIUnitTestWriter.SettingOptions;
using Microsoft.Extensions.Options;
using Octokit;

namespace AIUnitTestWriter.Wrappers.Git
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

        public async Task<GitHubCommit?> GetLatestCommitAsync(string owner, string repo, string branch, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var commits = await _gitHubClient.Repository.Commit.GetAll(owner, repo, new CommitRequest { Sha = branch });
            cancellationToken.ThrowIfCancellationRequested();
            return commits.FirstOrDefault();
        }

        public async Task<IEnumerable<GitCommitChange>> GetCommitChangesAsync(string owner, string repo, string commitSha, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var commit = await _gitHubClient.Repository.Commit.Get(owner, repo, commitSha);
            cancellationToken.ThrowIfCancellationRequested();
            return commit.Files.Select(f => new GitCommitChange
            {
                FilePath = f.Filename,
                Status = f.Status,
                Additions = f.Additions,
                Deletions = f.Deletions
            });
        }

        public async Task<string> GetFileContentAsync(string owner, string repo, string filePath, string commitSha, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var content = await _gitHubClient.Repository.Content.GetAllContentsByRef(owner, repo, filePath, commitSha);
                cancellationToken.ThrowIfCancellationRequested();
                return content.FirstOrDefault()?.Content ?? string.Empty;
            }
            catch (Octokit.NotFoundException)
            {
                // Return an empty string if the file is not found at the given commit
                Console.WriteLine($"File '{filePath}' not found at commit '{commitSha}'", ConsoleColor.Yellow);
                return string.Empty;
            }
        }

        public async Task CommitFileAsync(string owner, string repo, string branch, string filePath, string fileContent, string commitMessage, CancellationToken cancellationToken)
        {
            var reference = await _gitHubClient.Git.Reference.Get(owner, repo, $"heads/{branch}");

            var latestCommit = await _gitHubClient.Git.Commit.Get(owner, repo, reference.Object.Sha);

            var blob = new NewBlob
            {
                Content = fileContent,
                Encoding = EncodingType.Utf8
            };
            var blobRef = await _gitHubClient.Git.Blob.Create(owner, repo, blob);

            var treeItem = new NewTreeItem
            {
                Path = filePath.Replace("\\", "/"), // Normalize path
                Mode = "100644", // normal file
                Type = TreeType.Blob,
                Sha = blobRef.Sha
            };

            var newTree = new NewTree { BaseTree = latestCommit.Tree.Sha };
            newTree.Tree.Add(treeItem);
            var createdTree = await _gitHubClient.Git.Tree.Create(owner, repo, newTree);

            var newCommit = new NewCommit(commitMessage, createdTree.Sha, latestCommit.Sha);
            var createdCommit = await _gitHubClient.Git.Commit.Create(owner, repo, newCommit);

            await _gitHubClient.Git.Reference.Update(owner, repo, $"heads/{branch}", new ReferenceUpdate(createdCommit.Sha));
        }

        public async Task CreateBranchAsync(string owner, string repo, string branchName, string baseBranch, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var reference = await _gitHubClient.Git.Reference.Get(owner, repo, $"refs/heads/{baseBranch}");
            cancellationToken.ThrowIfCancellationRequested();
            var newReference = new NewReference($"refs/heads/{branchName}", reference.Object.Sha);
            await _gitHubClient.Git.Reference.Create(owner, repo, newReference);
        }

        public async Task<PullRequest> CreatePullRequestAsync(string owner, string repo, NewPullRequest pr, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _gitHubClient.PullRequest.Create(owner, repo, pr);
        }
    }
}
