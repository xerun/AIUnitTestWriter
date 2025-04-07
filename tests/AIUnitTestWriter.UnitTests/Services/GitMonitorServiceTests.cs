using AIUnitTestWriter.DTOs;
using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.Models;
using AIUnitTestWriter.Services.Git;
using AIUnitTestWriter.SettingOptions;
using AIUnitTestWriter.Wrappers.Git;
using Microsoft.Extensions.Options;
using Moq;
using Octokit;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class GitMonitorServiceTests
    {
        private readonly CancellationToken _cancellationToken = CancellationToken.None;
        private readonly MockFileSystem _fileSystemMock;
        private readonly Mock<ITestUpdaterService> _testUpdaterMock;
        private readonly Mock<IConsoleService> _consoleServiceMock;
        private readonly Mock<IGitHubClientWrapper> _mockGitHubClientWrapper;
        private readonly Mock<IDelayService> _delayServiceMock;
        private readonly Mock<ISkippedFilesManager> _mockSkippedFilesManager;
        private readonly IOptions<ProjectSettings> _projectSettings;
        private readonly IOptions<GitSettings> _gitSettings;
        private readonly ProjectConfigModel _projectConfig;
        private readonly GitMonitorService _gitMonitorService;

        public GitMonitorServiceTests()
        {   
            _testUpdaterMock = new Mock<ITestUpdaterService>();
            _consoleServiceMock = new Mock<IConsoleService>();
            _mockGitHubClientWrapper = new Mock<IGitHubClientWrapper>();
            _mockSkippedFilesManager = new Mock<ISkippedFilesManager>();
            _fileSystemMock = new MockFileSystem();
            _delayServiceMock = new Mock<IDelayService>();

            _projectSettings = Options.Create(new ProjectSettings
            {
                CodeFileExtension = ".cs"
            });

            _gitSettings = Options.Create(new GitSettings
            {
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

            _gitMonitorService = new GitMonitorService(
                _projectSettings,
                _gitSettings,
                _projectConfig,
                _mockGitHubClientWrapper.Object,
                _testUpdaterMock.Object,
                _consoleServiceMock.Object,
                _delayServiceMock.Object,
                _mockSkippedFilesManager.Object,
                _fileSystemMock
            );
        }

        [Fact]
        public void Constructor_ShouldThrowException_WhenNull()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Assert.Throws<ArgumentNullException>(() => new GitMonitorService(null, _gitSettings, _projectConfig, _mockGitHubClientWrapper.Object, _testUpdaterMock.Object, _consoleServiceMock.Object, _delayServiceMock.Object, _mockSkippedFilesManager.Object, _fileSystemMock));
            Assert.Throws<ArgumentNullException>(() => new GitMonitorService(_projectSettings, null, _projectConfig, _mockGitHubClientWrapper.Object, _testUpdaterMock.Object, _consoleServiceMock.Object, _delayServiceMock.Object, _mockSkippedFilesManager.Object, _fileSystemMock));
            Assert.Throws<ArgumentNullException>(() => new GitMonitorService(_projectSettings, _gitSettings, null, _mockGitHubClientWrapper.Object, _testUpdaterMock.Object, _consoleServiceMock.Object, _delayServiceMock.Object, _mockSkippedFilesManager.Object, _fileSystemMock));
            Assert.Throws<ArgumentNullException>(() => new GitMonitorService(_projectSettings, _gitSettings, _projectConfig, null, _testUpdaterMock.Object, _consoleServiceMock.Object, _delayServiceMock.Object, _mockSkippedFilesManager.Object, _fileSystemMock));
            Assert.Throws<ArgumentNullException>(() => new GitMonitorService(_projectSettings, _gitSettings, _projectConfig, _mockGitHubClientWrapper.Object, null, _consoleServiceMock.Object, _delayServiceMock.Object, _mockSkippedFilesManager.Object, _fileSystemMock));
            Assert.Throws<ArgumentNullException>(() => new GitMonitorService(_projectSettings, _gitSettings, _projectConfig, _mockGitHubClientWrapper.Object, _testUpdaterMock.Object, null, _delayServiceMock.Object, _mockSkippedFilesManager.Object, _fileSystemMock));
            Assert.Throws<ArgumentNullException>(() => new GitMonitorService(_projectSettings, _gitSettings, _projectConfig, _mockGitHubClientWrapper.Object, _testUpdaterMock.Object, _consoleServiceMock.Object, null, _mockSkippedFilesManager.Object, _fileSystemMock));
            Assert.Throws<ArgumentNullException>(() => new GitMonitorService(_projectSettings, _gitSettings, _projectConfig, _mockGitHubClientWrapper.Object, _testUpdaterMock.Object, _consoleServiceMock.Object, _delayServiceMock.Object, null, _fileSystemMock));
            Assert.Throws<ArgumentNullException>(() => new GitMonitorService(_projectSettings, _gitSettings, _projectConfig, _mockGitHubClientWrapper.Object, _testUpdaterMock.Object, _consoleServiceMock.Object, _delayServiceMock.Object, _mockSkippedFilesManager.Object, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Fact]
        public async Task MonitorAndTriggerAsync_ShouldDetectNewCommitAndGenerateTests()
        {
            // Arrange
            var filePath = @"C:\path\to\file.cs";
            var commitSha = "123abc";
            var latestCommit = new GitHubCommit("nodeId", "url", "label", "ref", commitSha, null, null, null, "commentsUrl", null, null, "htmlUrl", null, new List<GitReference>(), new List<GitHubCommitFile>());
            var gitCommitChange = new GitCommitChange()
            {
                FilePath = "src/SomeFile.cs",
                Status = "update",
                Additions = 1,
                Deletions = 0
            };

            _mockGitHubClientWrapper.Setup(x => x.GetLatestCommitAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(latestCommit);
            _mockGitHubClientWrapper.Setup(x => x.GetCommitChangesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                    .ReturnsAsync(new List<GitCommitChange> { gitCommitChange });
            _mockGitHubClientWrapper.Setup(x => x.CreatePullRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NewPullRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(new PullRequest());

            _fileSystemMock.AddFile(filePath, new MockFileData("public interface IMyInterface {}"));

            var testGenerationResultModel = new TestGenerationResultModel()
            {
                TempFilePath = "test/SomeClassTests.cs",
                GeneratedTestCode = "generated test code"
            };

            _testUpdaterMock.Setup(x => x.ProcessFileChangeAsync(It.IsAny<FileChangeProcessingDto>(), It.IsAny<CancellationToken>()))
                            .ReturnsAsync(testGenerationResultModel);

            // Use a CancellationToken that cancels after a short delay
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            // Act
            await _gitMonitorService.MonitorAndTriggerAsync(cts.Token);

            // Assert
            _mockGitHubClientWrapper.Verify(x => x.CreateBranchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockGitHubClientWrapper.Verify(x => x.CommitFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockGitHubClientWrapper.Verify(x => x.CreatePullRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NewPullRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            _consoleServiceMock.Verify(x => x.WriteColored(It.IsAny<string>(), It.IsAny<ConsoleColor>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task MonitorAndTriggerAsync_ShouldNotGenerateTests_WhenNoCodeFilesChanged()
        {
            // Arrange
            var commitSha = "123abc";
            var latestCommit = new GitHubCommit("nodeId", "url", "label", "ref", commitSha, null, null, null, "commentsUrl", null, null, "htmlUrl", null, new List<GitReference>(), new List<GitHubCommitFile>());
            var gitCommitChange = new GitCommitChange()
            {
                FilePath = "src/SomeFile.txt",
                Status = "update",
                Additions = 1,
                Deletions = 0
            };
            _mockGitHubClientWrapper.Setup(x => x.GetLatestCommitAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(latestCommit);
            _mockGitHubClientWrapper.Setup(x => x.GetCommitChangesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<GitCommitChange>
            {
                gitCommitChange
            });

            // Use a CancellationToken that cancels after a short delay
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            // Act
            await _gitMonitorService.MonitorAndTriggerAsync(cts.Token);

            // Assert
            _mockGitHubClientWrapper.Verify(x => x.CreateBranchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockGitHubClientWrapper.Verify(x => x.CommitFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreatePullRequestAsync_ShouldCreatePullRequest()
        {
            // Arrange
            var branchName = "feature/20250406080000";
            var pullRequest = new PullRequest();

            _mockGitHubClientWrapper.Setup(x => x.CreatePullRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NewPullRequest>(), _cancellationToken)).ReturnsAsync(pullRequest);

            // Act
            await _gitMonitorService.CreatePullRequestAsync(branchName, CancellationToken.None);

            // Assert
            _mockGitHubClientWrapper.Verify(x => x.CreatePullRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NewPullRequest>(), _cancellationToken), Times.Once);
            _consoleServiceMock.Verify(x => x.WriteColored(It.IsAny<string>(), It.IsAny<ConsoleColor>()), Times.Once);
        }

        [Fact]
        public async Task GetChangedFilesAsync_ShouldReturnCorrectFiles()
        {
            // Arrange
            var commitSha = "commit-sha-123";
            var files = new List<GitCommitChange>
            {
                new GitCommitChange()
                {
                    FilePath = "src/SomeFile.cs",
                    Status = "update",
                    Additions = 1,
                    Deletions = 0
                }
            };
            _mockGitHubClientWrapper.Setup(x => x.GetCommitChangesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), _cancellationToken))
                .ReturnsAsync(files);

            // Act
            var changedFiles = await _gitMonitorService.GetChangedFilesAsync(commitSha, CancellationToken.None);

            // Assert
            Assert.Single(changedFiles);
        }

        [Fact]
        public async Task GetExistingUnitTestAsync_ShouldReturnEmptyWhenFileNotFound()
        {
            // Arrange
            var testFilePath = "test/file1Tests.cs";
            _mockGitHubClientWrapper.Setup(x => x.GetFileContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), _cancellationToken))
                .Throws(new FileNotFoundException());

            // Act
            var result = await _gitMonitorService.GetExistingUnitTestAsync(testFilePath, "commit-sha-123", CancellationToken.None);

            // Assert
            Assert.Empty(result);
        }
    }
}
