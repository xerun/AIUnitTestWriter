using AIUnitTestWriter.DTOs;
using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.Models;
using AIUnitTestWriter.SettingOptions;
using Microsoft.Extensions.Options;
using Octokit;
using System.IO.Abstractions;

namespace AIUnitTestWriter.Services.Git
{
    public class GitMonitorService : IGitMonitorService
    {
        private readonly string _repositoryOwner;
        private readonly string _repositoryName;
        private readonly string _branchPrefix;
        private readonly int _pollInterval;
        private readonly IGitHubClientWrapper _gitHubClient;
        private readonly ProjectSettings _projectSettings;
        private readonly GitSettings _gitSettings;
        private readonly ITestUpdaterService _testUpdater;
        private readonly IConsoleService _consoleService;
        private readonly ISkippedFilesManager _skippedFilesManager;
        private readonly IFileSystem _fileSystem;
        private readonly ProjectConfigModel _projectConfig;
        private readonly IDelayService _delayService;
        private readonly string _codeFileExtension;
        private string? _lastCommitSha = string.Empty;

        public GitMonitorService(IOptions<ProjectSettings> projectSettings,
            IOptions<GitSettings> gitSetting,
            ProjectConfigModel projectConfig,
            IGitHubClientWrapper gitHubClientWrapper,
            ITestUpdaterService testUpdater,
            IConsoleService consoleService,
            IDelayService delayService,
            ISkippedFilesManager skippedFilesManager,
            IFileSystem fileSystem
        )
        {
            _projectConfig = projectConfig ?? throw new ArgumentNullException(nameof(projectConfig));
            _testUpdater = testUpdater ?? throw new ArgumentNullException(nameof(testUpdater));
            _projectSettings = projectSettings?.Value ?? throw new ArgumentNullException(nameof(projectSettings));
            _gitSettings = gitSetting?.Value ?? throw new ArgumentNullException(nameof(gitSetting));
            _consoleService = consoleService ?? throw new ArgumentNullException(nameof(consoleService));
            _gitHubClient = gitHubClientWrapper ?? throw new ArgumentNullException(nameof(gitHubClientWrapper));
            _delayService = delayService ?? throw new ArgumentNullException(nameof(delayService));
            _skippedFilesManager = skippedFilesManager ?? throw new ArgumentNullException(nameof(skippedFilesManager));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _codeFileExtension = _projectSettings.CodeFileExtension ?? throw new ArgumentNullException(nameof(_projectSettings.CodeFileExtension));
            _branchPrefix = _gitSettings.BranchPrefix;
            _pollInterval = _gitSettings.PollInterval;

            if (_projectConfig.IsGitRepository)
            {
                var uri = new Uri(_projectConfig.GitRepositoryUrl);
                var pathSegments = uri.AbsolutePath.Split('/');

                if (pathSegments.Length < 3)
                {
                    throw new ArgumentException("Invalid GitHub repository URL.");
                }

                _repositoryOwner = pathSegments[1];
                _repositoryName = pathSegments[2].Replace(".git", string.Empty);
            }
            else
            {
                _repositoryOwner = string.Empty;
                _repositoryName = string.Empty;
            }
        }

        /// <inheritdoc/>
        public async Task MonitorAndTriggerAsync(CancellationToken cancellationToken = default)
        {
            _consoleService.WriteColored("Monitoring Git repository for changes... Press Ctrl+C to exit.", ConsoleColor.Cyan);

            while (!cancellationToken.IsCancellationRequested)
            {
                // Get the latest commit from the repository
                var latestCommit = await _gitHubClient.GetLatestCommitAsync(_repositoryOwner, _repositoryName, _gitSettings.GitMainBranch, cancellationToken);

                // If no new commit (still on the same commit), continue polling
                if (_lastCommitSha == latestCommit?.Sha)
                {
                    await _delayService.DelayAsync(_pollInterval, cancellationToken);
                    continue;
                }

                _lastCommitSha = latestCommit?.Sha;
                var changedFiles = await GetChangedFilesAsync(latestCommit?.Sha, cancellationToken);

                if (changedFiles.Any(f => f.FilePath.EndsWith(_codeFileExtension, StringComparison.OrdinalIgnoreCase)))
                {
                    _consoleService.WriteColored($"Detected changes in {_codeFileExtension} files. Generating unit tests...", ConsoleColor.Green);
                    string branchName = $"{_branchPrefix}{DateTime.UtcNow:yyyyMMddHHmmss}";

                    await _gitHubClient.CreateBranchAsync(
                        owner: _repositoryOwner,
                        repo: _repositoryName,
                        branchName: branchName,
                        baseBranch: _gitSettings.GitMainBranch,
                        cancellationToken: cancellationToken
                    );

                    foreach (var file in changedFiles)
                    {
                        string testFilePath = GetTestFilePath(file.FilePath);

                        string existingUnitTest = await GetExistingUnitTestAsync(testFilePath, latestCommit?.Sha, cancellationToken);

                        var fileChangeProcessingDto = new FileChangeProcessingDto(
                            filePath: file.FilePath,
                            oldContent: file.OldContent,
                            newContent: file.NewContent,
                            codeExtension: _codeFileExtension,
                            sampleUnitTest: _projectConfig.SampleUnitTestContent,
                            existingUnitTest,
                            promptUser: true,
                            projectFolder: _projectConfig.ProjectPath,
                            srcFolder: _projectConfig.SrcFolder,
                            testsFolder: _projectConfig.TestsFolder
                        );

                        var generatedContent = await _testUpdater.ProcessFileChangeAsync(fileChangeProcessingDto, cancellationToken);

                        // Commit the generated content to GitHub
                        await _gitHubClient.CommitFileAsync(
                            owner: _repositoryOwner,
                            repo: _repositoryName,
                            branch: branchName,
                            filePath: generatedContent.TestFilePath,
                            fileContent: generatedContent.GeneratedTestCode,
                            commitMessage: $"Add/update test for {file.FilePath}",
                            cancellationToken: cancellationToken
                        );
                    }

                    // Commit changes and create PR
                    await CreatePullRequestAsync(branchName, cancellationToken);
                }
                else
                {
                    _consoleService.WriteColored("No new changes detected.", ConsoleColor.Yellow);
                }

                await _delayService.DelayAsync(_pollInterval, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async Task CreatePullRequestAsync(string branchName, CancellationToken cancellationToken = default)
        {
            var newPr = new NewPullRequest("Automated Test Update", branchName, _gitSettings.GitMainBranch)
            {
                Body = "This PR was generated automatically to add/update unit tests for modified files."
            };

            var pr = await _gitHubClient.CreatePullRequestAsync(_repositoryOwner, _repositoryName, newPr, cancellationToken);
            _consoleService.WriteColored($"Pull request created: {pr.HtmlUrl}", ConsoleColor.Green);
        }

        internal async Task<IEnumerable<GitFileChange>> GetChangedFilesAsync(string commitSha, CancellationToken cancellationToken = default)
        {
            var changes = await _gitHubClient.GetCommitChangesAsync(_repositoryOwner, _repositoryName, commitSha, cancellationToken);
            var changedFiles = changes
                .Where(c => !string.IsNullOrWhiteSpace(c.FilePath) &&
                            c.FilePath.EndsWith(_codeFileExtension, StringComparison.OrdinalIgnoreCase) &&
                            !_skippedFilesManager.ShouldSkip(c.FilePath) && 
                            !c.FilePath.Contains(_projectConfig.TestsFolder))
                .ToList();

            var result = new List<GitFileChange>();

            // Fetch the parent commit SHA
            var commit = await _gitHubClient.GetLatestCommitAsync(_repositoryOwner, _repositoryName, commitSha, cancellationToken);
            var parentSha = commit?.Parents?.FirstOrDefault()?.Sha;

            foreach (var file in changedFiles)
            {
                string oldContent = string.Empty;
                string newContent = string.Empty;

                if (!string.IsNullOrEmpty(parentSha))
                {
                    try
                    {
                        oldContent = await _gitHubClient.GetFileContentAsync(_repositoryOwner, _repositoryName, file.FilePath, parentSha, cancellationToken);
                    }
                    catch
                    {
                        oldContent = string.Empty; // File might be newly added
                    }
                }

                try
                {
                    newContent = await _gitHubClient.GetFileContentAsync(_repositoryOwner, _repositoryName, file.FilePath, commitSha, cancellationToken);
                }
                catch
                {
                    newContent = string.Empty; // File might have been deleted
                }

                result.Add(new GitFileChange(file.FilePath, oldContent, newContent));
            }

            return result;
        }

        internal async Task<string> GetExistingUnitTestAsync(string testFilePath, string commitSha, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if the test file exists in the repository
                var fileContent = await _gitHubClient.GetFileContentAsync(
                    _repositoryOwner,
                    _repositoryName,
                    testFilePath,
                    commitSha,
                    cancellationToken
                );

                // Return the content of the existing test file
                return fileContent;
            }
            catch (FileNotFoundException)
            {
                // Return empty if the file doesn't exist
                return string.Empty;
            }
        }

        private string GetTestFilePath(string srcFilePath)
        {
            var relativePath = _fileSystem.Path.GetRelativePath(_projectConfig.SrcFolder, srcFilePath);
            var testFilePath = _fileSystem.Path.Combine(_projectConfig.TestsFolder, relativePath);
            var testFileName = _fileSystem.Path.GetFileNameWithoutExtension(srcFilePath) + "Tests" + _fileSystem.Path.GetExtension(testFilePath);
            return _fileSystem.Path.Combine(_fileSystem.Path.GetDirectoryName(testFilePath), testFileName);
        }
    }
}
