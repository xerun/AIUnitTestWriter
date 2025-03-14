using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.Models;
using AIUnitTestWriter.Services.Git;
using AIUnitTestWriter.SettingOptions;
using AIUnitTestWriter.Wrappers;
using Microsoft.Extensions.Options;
using Moq;
using Octokit;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class GitMonitorServiceTests
    {
        private readonly Mock<IGitProcessService> _gitProcessServiceMock;
        private readonly Mock<ITestUpdaterService> _testUpdaterMock;
        private readonly Mock<IConsoleService> _consoleServiceMock;
        private readonly Mock<IGitHubClientWrapper> _mockGitHubClientWrapper;
        private readonly Mock<IDelayService> _delayServiceMock;
        private readonly Mock<ISkippedFilesManager> _mockSkippedFilesManager;
        private readonly IOptions<GitSettings> _gitSettings;
        private readonly ProjectConfigModel _projectConfig;
        private readonly GitMonitorService _gitService;

        public GitMonitorServiceTests()
        {
            _gitProcessServiceMock = new Mock<IGitProcessService>();
            _testUpdaterMock = new Mock<ITestUpdaterService>();
            _consoleServiceMock = new Mock<IConsoleService>();
            _mockGitHubClientWrapper = new Mock<IGitHubClientWrapper>();
            _mockSkippedFilesManager = new Mock<ISkippedFilesManager>();
            _delayServiceMock = new Mock<IDelayService>();

            _gitSettings = Options.Create(new GitSettings
            {
                LocalRepositoryPath = "/mock/repo",
                BranchPrefix = "feature/",
                GitHubToken = "mock-token",
                GitMainBranch = "main"
            });

            _projectConfig = new ProjectConfigModel
            {
                IsGitRepository = true,
                GitRepositoryUrl = "https://github.com/mockuser/mockrepo.git",
                SrcFolder = "/mock/src",
                TestsFolder = "/mock/tests",
                SampleUnitTestContent = "Sample Test Content"
            };

            _gitService = new GitMonitorService(
                _gitSettings,
                _projectConfig,
                _gitProcessServiceMock.Object,
                _mockGitHubClientWrapper.Object,
                _testUpdaterMock.Object,
                _consoleServiceMock.Object,
                _delayServiceMock.Object,
                _mockSkippedFilesManager.Object
            );
        }

        [Fact]
        public void Constructor_ShouldThrowException_WhenNull()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Assert.Throws<ArgumentNullException>(() => new GitMonitorService(null, _projectConfig, _gitProcessServiceMock.Object, _mockGitHubClientWrapper.Object, _testUpdaterMock.Object, _consoleServiceMock.Object, _delayServiceMock.Object, _mockSkippedFilesManager.Object));
            Assert.Throws<ArgumentNullException>(() => new GitMonitorService(_gitSettings, null, _gitProcessServiceMock.Object, _mockGitHubClientWrapper.Object, _testUpdaterMock.Object, _consoleServiceMock.Object, _delayServiceMock.Object, _mockSkippedFilesManager.Object));
            Assert.Throws<ArgumentNullException>(() => new GitMonitorService(_gitSettings, _projectConfig, null, _mockGitHubClientWrapper.Object, _testUpdaterMock.Object, _consoleServiceMock.Object, _delayServiceMock.Object, _mockSkippedFilesManager.Object));
            Assert.Throws<ArgumentNullException>(() => new GitMonitorService(_gitSettings, _projectConfig, _gitProcessServiceMock.Object, null, _testUpdaterMock.Object, _consoleServiceMock.Object, _delayServiceMock.Object, _mockSkippedFilesManager.Object));
            Assert.Throws<ArgumentNullException>(() => new GitMonitorService(_gitSettings, _projectConfig, _gitProcessServiceMock.Object, _mockGitHubClientWrapper.Object, null, _consoleServiceMock.Object, _delayServiceMock.Object, _mockSkippedFilesManager.Object));
            Assert.Throws<ArgumentNullException>(() => new GitMonitorService(_gitSettings, _projectConfig, _gitProcessServiceMock.Object, _mockGitHubClientWrapper.Object, _testUpdaterMock.Object, null, _delayServiceMock.Object, _mockSkippedFilesManager.Object));
            Assert.Throws<ArgumentNullException>(() => new GitMonitorService(_gitSettings, _projectConfig, _gitProcessServiceMock.Object, _mockGitHubClientWrapper.Object, _testUpdaterMock.Object, _consoleServiceMock.Object, null, _mockSkippedFilesManager.Object));
            Assert.Throws<ArgumentNullException>(() => new GitMonitorService(_gitSettings, _projectConfig, _gitProcessServiceMock.Object, _mockGitHubClientWrapper.Object, _testUpdaterMock.Object, _consoleServiceMock.Object, _delayServiceMock.Object, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public async Task MonitorAndTriggerAsync_ShouldCallGitCommands()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var testGenerationResultModel = new TestGenerationResultModel
            {
                TestFilePath = @"C:\tests\path\to\fileTests.cs",
                GeneratedTestCode = "public class MyTest { public void TestMethod() {} }"
            };
            var fetchCommandResponse = "Fetched";
            var pullCommandResponse = "Already up to date.";

            // Set up the mocks for Git commands
            _gitProcessServiceMock.Setup(x => x.RunCommand("fetch origin", It.IsAny<string>())).Returns(fetchCommandResponse);
            _gitProcessServiceMock.Setup(x => x.RunCommand("pull origin main", It.IsAny<string>())).Returns(pullCommandResponse);

            // Set up a mock for the GetChangedFiles method to simulate a file change if needed
            var changedFiles = "/path/test.cs\n"; // Simulate changed .cs file
            _gitProcessServiceMock.Setup(x => x.RunCommand("diff --name-only HEAD@{1}", It.IsAny<string>())).Returns(changedFiles);

            // Simulate the ProcessFileChange method to not actually run the test updater
            _testUpdaterMock.Setup(x => x.ProcessFileChange(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                            .ReturnsAsync(testGenerationResultModel);

            _delayServiceMock.Setup(ds => ds.DelayAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

            // Act - Run the MonitorAndTriggerAsync method but only for a single iteration of the loop
            var monitorTask = Task.Run(() => _gitService.MonitorAndTriggerAsync(cancellationTokenSource.Token));

            // Allow the loop to execute once
            await Task.Delay(1500); // Increase delay to ensure the loop runs at least once

            cancellationTokenSource.Cancel();  // Cancel after first iteration

            // Assert - Verify that the Git commands are executed
            _gitProcessServiceMock.Verify(x => x.RunCommand("fetch origin", It.IsAny<string>()), Times.AtLeastOnce);
            _gitProcessServiceMock.Verify(x => x.RunCommand("pull origin main", It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task CreatePullRequestAsync_ShouldCallGitHubApi()
        {
            // Arrange
            var pullRequest = new PullRequest();  // Mock pull request response from GitHub API

            // Mock the Git commands that will be called in the method
            _gitProcessServiceMock.Setup(x => x.RunCommand(It.IsAny<string>(), It.IsAny<string>())).Returns("Success");

            // Mock the GitHub API client
            var prMock = new Mock<IPullRequestsClient>();
            prMock.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NewPullRequest>()))
                  .ReturnsAsync(pullRequest);

            // Set up the mock for GitHub client wrapper (the one being used in the GitService)
            _mockGitHubClientWrapper.Setup(x => x.CreatePullRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NewPullRequest>()))
                                    .ReturnsAsync(pullRequest);
            _consoleServiceMock.Setup(x => x.WriteColored(It.IsAny<string>(), It.IsAny<ConsoleColor>()));

            // Ensure that the generated branch name will be in the correct format
            var expectedBranchName = $"feature/{DateTime.UtcNow:yyyyMMddHHmmss}";  // Create an expected branch name
            var newPr = new NewPullRequest("Automated Test Update", expectedBranchName, _gitSettings.Value.GitMainBranch)
            {
                Body = "This PR was generated automatically to add/update unit tests for modified files."
            };

            // Act
            await _gitService.CreatePullRequestAsync();

            // Assert
            // Verify that the correct Git commands are called with the expected arguments
            _gitProcessServiceMock.Verify(x => x.RunCommand($"checkout -b {expectedBranchName}", It.IsAny<string>()), Times.Once);
            _gitProcessServiceMock.Verify(x => x.RunCommand("add .", It.IsAny<string>()), Times.Once);
            _gitProcessServiceMock.Verify(x => x.RunCommand("commit -m \"Automated test update via AI\"", It.IsAny<string>()), Times.Once);
            _gitProcessServiceMock.Verify(x => x.RunCommand($"push origin {expectedBranchName}", It.IsAny<string>()), Times.Once);

            // Verify the creation of the pull request through the GitHub client mock
            _mockGitHubClientWrapper.Verify(x => x.CreatePullRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.Is<NewPullRequest>(pr => pr.Body == newPr.Body && pr.Head == expectedBranchName)), Times.Once);
        }

        [Fact]
        public void EnsureRepoCloned_ShouldCloneRepo_WhenNotFound()
        {
            _gitProcessServiceMock.Setup(x => x.RunCommand(It.IsAny<string>(), It.IsAny<string>())).Returns("Cloned");

            if (Directory.Exists("/mock/repo"))
                Directory.Delete("/mock/repo", true);

            _gitService.GetType().GetMethod("EnsureRepoCloned", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                      .Invoke(_gitService, null);
        }

        [Fact]
        public void GetChangedFiles_ShouldExcludeSkippedFiles()
        {
            // Arrange
            _gitProcessServiceMock.Setup(x => x.RunCommand("diff --name-only HEAD@{1}", "/mock/repo"))
                                  .Returns("file1.cs\nfile2.cs\nfile3.cs");

            // Simulate `ShouldSkip` returning true for file2.cs
            _mockSkippedFilesManager.Setup(x => x.ShouldSkip("file2.cs")).Returns(true);
            _mockSkippedFilesManager.Setup(x => x.ShouldSkip("file1.cs")).Returns(false);
            _mockSkippedFilesManager.Setup(x => x.ShouldSkip("file3.cs")).Returns(false);

            // Act
            var changedFiles = _gitService.GetType()
                                           .GetMethod("GetChangedFiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                           .Invoke(_gitService, null) as IEnumerable<string>;

            // Assert
            Assert.Contains("file1.cs", changedFiles);
            Assert.Contains("file3.cs", changedFiles);
            Assert.DoesNotContain("file2.cs", changedFiles); // Ensure skipped file is excluded
        }

        [Fact]
        public async Task MonitorAndTriggerAsync_ShouldSkipFiles_WhenShouldSkipReturnsTrue()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var pullRequest = new PullRequest();

            // Git command responses
            _gitProcessServiceMock.Setup(x => x.RunCommand("fetch origin", It.IsAny<string>())).Returns("Fetched");
            _gitProcessServiceMock.Setup(x => x.RunCommand("pull origin main", It.IsAny<string>())).Returns("Pulled");

            // Changed files
            _gitProcessServiceMock.Setup(x => x.RunCommand("diff --name-only HEAD@{1}", It.IsAny<string>()))
                                  .Returns("file1.cs\nfile2.cs\nfile3.cs");

            var prMock = new Mock<IPullRequestsClient>();
            prMock.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NewPullRequest>()))
                  .ReturnsAsync(pullRequest);

            // Set up the mock for GitHub client wrapper (the one being used in the GitService)
            _mockGitHubClientWrapper.Setup(x => x.CreatePullRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NewPullRequest>()))
                                    .ReturnsAsync(pullRequest);
            _consoleServiceMock.Setup(x => x.WriteColored(It.IsAny<string>(), It.IsAny<ConsoleColor>()));

            // Simulate `ShouldSkip` returning true for file2.cs
            _mockSkippedFilesManager.Setup(x => x.ShouldSkip("file2.cs")).Returns(true);
            _mockSkippedFilesManager.Setup(x => x.ShouldSkip("file1.cs")).Returns(false);
            _mockSkippedFilesManager.Setup(x => x.ShouldSkip("file3.cs")).Returns(false);

            var testGenerationResultModel = new TestGenerationResultModel
            {
                TestFilePath = @"C:\tests\path\file1Tests.cs",
                GeneratedTestCode = "public class MyTest { public void TestMethod() {} }"
            };

            // TestUpdater mock setup
            _testUpdaterMock.Setup(x => x.ProcessFileChange(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                            .ReturnsAsync(testGenerationResultModel);            
            _delayServiceMock.Setup(ds => ds.DelayAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

            // Act
            var monitorTask = Task.Run(() => _gitService.MonitorAndTriggerAsync(cancellationTokenSource.Token));
            await Task.Delay(1500);
            cancellationTokenSource.Cancel();

            // Assert
            _testUpdaterMock.Verify(x => x.ProcessFileChange(It.IsAny<string>(), It.IsAny<string>(), "file2.cs", It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void GetChangedFiles_ShouldReturnEmpty_WhenAllFilesAreSkipped()
        {
            // Arrange
            _gitProcessServiceMock.Setup(x => x.RunCommand("diff --name-only HEAD@{1}", "/mock/repo"))
                                  .Returns("file1.cs\nfile2.cs\nfile3.cs");

            // Simulate all files being skipped
            _mockSkippedFilesManager.Setup(x => x.ShouldSkip("file1.cs")).Returns(true);
            _mockSkippedFilesManager.Setup(x => x.ShouldSkip("file2.cs")).Returns(true);
            _mockSkippedFilesManager.Setup(x => x.ShouldSkip("file3.cs")).Returns(true);

            // Act
            var changedFiles = _gitService.GetType()
                                           .GetMethod("GetChangedFiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                           .Invoke(_gitService, null) as IEnumerable<string>;

            // Assert
            Assert.Empty(changedFiles);
        }

        [Fact]
        public void RunGitCommand_ShouldCallGitProcessService()
        {
            _gitProcessServiceMock.Setup(x => x.RunCommand("status", "/mock/repo")).Returns("On branch main");

            var result = _gitService.GetType()
                                    .GetMethod("RunGitCommand", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                    .Invoke(_gitService, new object[] { "status" }) as string;

            Assert.Equal("On branch main", result);
            _gitProcessServiceMock.Verify(x => x.RunCommand("status", "/mock/repo"), Times.Once);
        }
    }
}
