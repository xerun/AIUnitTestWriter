using AIUnitTestWriter.Models;
using AIUnitTestWriter.Services;
using AIUnitTestWriter.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using Octokit;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class GitIntegrationServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ITestUpdater> _mockTestUpdater;
        private readonly Mock<IConsoleService> _mockConsoleService;
        private readonly Mock<IGitProcessService> _mockProcessService;
        private readonly Mock<IGitHubClient> _mockGitHubClient;
        private readonly IGitIntegrationService _gitIntegrationService;

        public GitIntegrationServiceTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockTestUpdater = new Mock<ITestUpdater>();
            _mockConsoleService = new Mock<IConsoleService>();
            _mockProcessService = new Mock<IGitProcessService>();
            _mockGitHubClient = new Mock<IGitHubClient>();

            _mockConfig.Setup(c => c["Git:RepositoryPath"]).Returns("/test/repo");
            _mockConfig.Setup(c => c["Git:RepositoryOwner"]).Returns("test-owner");
            _mockConfig.Setup(c => c["Git:RepositoryName"]).Returns("test-repo");
            _mockConfig.Setup(c => c["Git:BranchPrefix"]).Returns("auto/test-update-");
            _mockConfig.Setup(c => c["Git:GitHubToken"]).Returns("fake-token");

            _mockProcessService.Setup(p => p.RunCommand(It.IsAny<string>(), It.IsAny<string>())).Returns("");

            _gitIntegrationService = new GitIntegrationService(
                _mockConfig.Object,
                _mockProcessService.Object,
                _mockTestUpdater.Object,
                _mockConsoleService.Object
            );
        }

        [Fact]
        public async Task MonitorAndTriggerAsync_ShouldTrigger_WhenCsFileChangesDetected()
        {
            _mockProcessService.Setup(p => p.RunCommand("status --porcelain", "/test/repo"))
                               .Returns(" M SomeFile.cs");

            var projectConfig = new ProjectConfigModel { SrcFolder = "src", TestsFolder = "tests", SampleUnitTestContent = "test content" };

            await _gitIntegrationService.MonitorAndTriggerAsync(projectConfig);

            _mockTestUpdater.Verify(t => t.ProcessFileChange("src", "tests", "SomeFile.cs", "test content", false), Times.Once);
            _mockConsoleService.Verify(c => c.WriteColored(It.IsAny<string>(), ConsoleColor.Green), Times.Once);
        }

        [Fact]
        public async Task CreatePullRequestAsync_ShouldCreatePR()
        {
            var pr = new PullRequest();
            var prMock = new Mock<IPullRequestsClient>();
            prMock.Setup(p => p.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NewPullRequest>()))
                  .ReturnsAsync(pr);
            _mockGitHubClient.Setup(g => g.PullRequest).Returns(prMock.Object);

            await _gitIntegrationService.CreatePullRequestAsync("test-branch", "Test PR", "PR Body");

            _mockProcessService.Verify(p => p.RunCommand(It.IsAny<string>(), "/test/repo"), Times.Exactly(4));
            _mockConsoleService.Verify(c => c.WriteColored("Pull request created: http://github.com/test/pr", ConsoleColor.Green), Times.Once);
        }
    }
}
