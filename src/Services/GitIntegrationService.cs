using AIUnitTestWriter.Models;
using AIUnitTestWriter.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Octokit;
using System.Diagnostics;

namespace AIUnitTestWriter.Services
{
    public class GitIntegrationService : IGitIntegrationService
    {
        private readonly string _repositoryPath;
        private readonly string _repositoryOwner;
        private readonly string _repositoryName;
        private readonly string _branchPrefix;
        private readonly GitHubClient _gitHubClient;
        private readonly IGitProcessService _gitProcessService;
        private readonly ITestUpdater _testUpdater;
        private readonly IConsoleService _consoleService;

        public GitIntegrationService(IConfiguration configuration, IGitProcessService gitProcessService, ITestUpdater testUpdater, IConsoleService consoleService)
        {
            _repositoryPath = configuration["Git:RepositoryPath"]
                              ?? throw new ArgumentException("Git:RepositoryPath not configured");
            _repositoryOwner = configuration["Git:RepositoryOwner"]
                              ?? throw new ArgumentException("Git:RepositoryOwner not configured");
            _repositoryName = configuration["Git:RepositoryName"]
                              ?? throw new ArgumentException("Git:RepositoryName not configured");
            _branchPrefix = configuration["Git:BranchPrefix"] ?? "auto/test-update-";
            _testUpdater = testUpdater;
            var token = configuration["Git:GitHubToken"]
                        ?? throw new ArgumentException("Git:GitHubToken not configured");
            _gitHubClient = new GitHubClient(new Octokit.ProductHeaderValue("AIUnitTestWriter"))
            {
                Credentials = new Credentials(token)
            };
            _gitProcessService = gitProcessService ?? throw new ArgumentException(nameof(gitProcessService));
            _consoleService = consoleService ?? throw new ArgumentException(nameof(consoleService));
        }

        /// <inheritdoc/>
        public async Task MonitorAndTriggerAsync(ProjectConfigModel config)
        {
            var changedFiles = GetChangedFiles();
            // Filter for .cs files.
            if (changedFiles.Any(f => f.EndsWith(".cs")))
            {
                _consoleService.WriteColored($"Detected changes in .cs files. Triggering test generation...", ConsoleColor.Green);
                foreach (var file in changedFiles.Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
                {
                    await _testUpdater.ProcessFileChange(config.SrcFolder, config.TestsFolder, file, config.SampleUnitTestContent, false);
                }
            }
            else
            {
                _consoleService.WriteColored($"No changes in .cs files detected.", ConsoleColor.Yellow);
            }
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task CreatePullRequestAsync(string branchName, string prTitle, string prBody)
        {
            // Create a new branch (e.g., using git checkout -b).
            RunGitCommand($"checkout -b {branchName}");

            // Stage and commit changes.
            RunGitCommand("add .");
            RunGitCommand("commit -m \"Automated test update via AI\"");

            // Push the new branch.
            RunGitCommand($"push origin {branchName}");

            // Create a pull request using Octokit.
            var newPr = new NewPullRequest(prTitle, branchName, "main")
            {
                Body = prBody
            };

            var pr = await _gitHubClient.PullRequest.Create(_repositoryOwner, _repositoryName, newPr);
            _consoleService.WriteColored($"Pull request created: {pr.HtmlUrl}", ConsoleColor.Green);
        }

        private IEnumerable<string> GetChangedFiles()
        {
            var output = _gitProcessService.RunCommand("status --porcelain", _repositoryPath);
            // Each line starts with a status code followed by the file path.
            return output.Split('\n')
                         .Select(line => line.Trim())
                         .Where(line => !string.IsNullOrWhiteSpace(line))
                         .Select(line => line.Substring(3)); // Remove status code and space.
        }

        private void RunGitCommand(string arguments)
        {
            var output = _gitProcessService.RunCommand(arguments, _repositoryPath);
            if (!string.IsNullOrWhiteSpace(output))
                Console.WriteLine(output);
        }
    }
}
