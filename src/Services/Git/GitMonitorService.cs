﻿using AIUnitTestWriter.Models;
using AIUnitTestWriter.SettingOptions;
using Microsoft.Extensions.Options;
using Octokit;
using AIUnitTestWriter.Wrappers;
using AIUnitTestWriter.Interfaces;

namespace AIUnitTestWriter.Services.Git
{
    public class GitMonitorService : IGitMonitorService
    {
        private readonly string _repositoryPath;
        private readonly string _repositoryOwner;
        private readonly string _repositoryName;
        private readonly string _branchPrefix;
        private readonly int _pollInterval;
        private readonly IGitHubClientWrapper _gitHubClient;
        private readonly ProjectSettings _projectSettings;
        private readonly GitSettings _gitSettings;
        private readonly IGitProcessService _gitProcessService;
        private readonly ITestUpdaterService _testUpdater;
        private readonly IConsoleService _consoleService;
        private readonly ISkippedFilesManager _skippedFilesManager;
        private readonly ProjectConfigModel _projectConfig;
        private readonly IDelayService _delayService;
        private readonly string _codeFileextension;

        public GitMonitorService(IOptions<ProjectSettings> projectSettings, IOptions<GitSettings> gitSetting, ProjectConfigModel projectConfig, IGitProcessService gitProcessService, IGitHubClientWrapper gitHubClientWrapper, ITestUpdaterService testUpdater, IConsoleService consoleService, IDelayService delayService, ISkippedFilesManager skippedFilesManager)
        {
            _projectConfig = projectConfig ?? throw new ArgumentNullException(nameof(projectConfig));
            _testUpdater = testUpdater ?? throw new ArgumentNullException(nameof(testUpdater));
            _projectSettings = projectSettings?.Value ?? throw new ArgumentNullException(nameof(projectSettings));
            _gitSettings = gitSetting?.Value ?? throw new ArgumentNullException(nameof(gitSetting));
            _gitProcessService = gitProcessService ?? throw new ArgumentNullException(nameof(gitProcessService));
            _consoleService = consoleService ?? throw new ArgumentNullException(nameof(consoleService));
            _gitHubClient = gitHubClientWrapper ?? throw new ArgumentNullException(nameof(gitHubClientWrapper));
            _delayService = delayService ?? throw new ArgumentNullException(nameof(delayService));
            _skippedFilesManager = skippedFilesManager ?? throw new ArgumentNullException(nameof(skippedFilesManager));
            _codeFileextension = _projectSettings.CodeFileExtension ?? throw new ArgumentNullException(nameof(_projectSettings.CodeFileExtension));
            _repositoryPath = _gitSettings.LocalRepositoryPath;
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
            EnsureRepoCloned();
            _consoleService.WriteColored("Monitoring Git repository for changes... Press Ctrl+C to exit.", ConsoleColor.Cyan);

            while (!cancellationToken.IsCancellationRequested) // Infinite loop with cancellation check
            {
                _ = RunGitCommand("fetch origin"); // Fetch latest changes
                var response = RunGitCommand($"pull origin {_gitSettings.GitMainBranch}"); // Pull updates from main branch

                if (response != null && response.Contains("Already up to date."))
                {
                    await _delayService.DelayAsync(_pollInterval, cancellationToken);
                    continue;
                }

                var changedFiles = GetChangedFiles();

                if (changedFiles.Any(f => f.EndsWith(_codeFileextension, StringComparison.OrdinalIgnoreCase)))
                {
                    _consoleService.WriteColored($"Detected changes in {_codeFileextension} files. Generating unit tests...", ConsoleColor.Green);
                    foreach (var file in changedFiles)
                    {
                        await _testUpdater.ProcessFileChangeAsync(_projectConfig.SrcFolder, _projectConfig.TestsFolder, $"{_gitSettings.LocalRepositoryPath}/{file}", _projectConfig.SampleUnitTestContent, false, cancellationToken);
                    }

                    // Commit changes and create PR
                    await CreatePullRequestAsync(cancellationToken);
                }
                else
                {
                    _consoleService.WriteColored("No new changes detected.", ConsoleColor.Yellow);
                }

                await _delayService.DelayAsync(_pollInterval, cancellationToken);
            }
        }

        /// <summary>
        /// Ensures the repository is cloned locally.
        /// </summary>
        internal void EnsureRepoCloned()
        {
            if (!Directory.Exists(_repositoryPath) || !Directory.Exists(Path.Combine(_repositoryPath, ".git")))
            {
                _consoleService.WriteColored("Repository not found locally. Cloning...", ConsoleColor.Yellow);
                RunGitCommand($"clone {_projectConfig.GitRepositoryUrl} {_repositoryPath}");
            }
        }

        /// <inheritdoc/>
        public async Task CreatePullRequestAsync(CancellationToken cancellationToken = default)
        {
            string branchName = $"{_branchPrefix}{DateTime.UtcNow:yyyyMMddHHmmss}";
            RunGitCommand($"checkout -b {branchName}");

            RunGitCommand("add .");
            RunGitCommand("commit -m \"Automated test update via AI\"");
            RunGitCommand($"push origin {branchName}");

            var newPr = new NewPullRequest("Automated Test Update", branchName, _gitSettings.GitMainBranch)
            {
                Body = "This PR was generated automatically to add/update unit tests for modified files."
            };

            var pr = await _gitHubClient.CreatePullRequestAsync(_repositoryOwner, _repositoryName, newPr, cancellationToken);
            _consoleService.WriteColored($"Pull request created: {pr.HtmlUrl}", ConsoleColor.Green);
        }

        /// <summary>
        /// Gets changed files by running `diff --name-only HEAD@{1}`.
        /// </summary>
        internal IEnumerable<string> GetChangedFiles()
        {
            var output = _gitProcessService.RunCommand("diff --name-only HEAD@{1}", _repositoryPath);

            if (string.IsNullOrWhiteSpace(output))
            {
                return Enumerable.Empty<string>();
            }

            var files = output.Split('\n')
                      .Select(line => line.Trim())
                      .Where(line => !string.IsNullOrWhiteSpace(line) &&
                                     line.EndsWith(_codeFileextension, StringComparison.OrdinalIgnoreCase) &&
                                     !_skippedFilesManager.ShouldSkip(line)) // Combined conditions
                      .ToList();

            _consoleService.WriteColored($"Pulled changes detected: {string.Join(", ", files)}", ConsoleColor.Cyan);

            return files;
        }

        /// <summary>
        /// Runs a Git command and handles output/errors.
        /// </summary>
        internal string? RunGitCommand(string arguments)
        {
            var output = _gitProcessService.RunCommand(arguments, _repositoryPath);
            if (!string.IsNullOrWhiteSpace(output))
                _consoleService.WriteColored($"Git repository: {output}", ConsoleColor.Cyan);

            return output;
        }
    }
}
