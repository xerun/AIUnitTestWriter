using AIUnitTestWriter.Services;
using AIUnitTestWriter.Services.Interfaces;
using AIUnitTestWriter.SettingOptions;
using Microsoft.Extensions.Options;
using Moq;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class ProjectInitializerServiceTests
    {
        private readonly Mock<IConsoleService> _mockConsoleService;
        private readonly ProjectSettings _projectSettings;
        private readonly GitSettings _gitSettings;
        private readonly ProjectInitializerService _projectInitializerService;

        public ProjectInitializerServiceTests()
        {
            _mockConsoleService = new Mock<IConsoleService>();

            _projectSettings = new ProjectSettings
            {
                SourceFolder = "src",
                TestsFolder = "tests"
            };

            _gitSettings = new GitSettings
            {
                LocalRepositoryPath = Path.Combine(Path.GetTempPath(), "TestRepo")
            };

            var projectSettingsOptions = Options.Create(_projectSettings);
            var gitSettingsOptions = Options.Create(_gitSettings);

            _projectInitializerService = new ProjectInitializerService(projectSettingsOptions, gitSettingsOptions, _mockConsoleService.Object);
        }

        [Fact]
        public void Initialize_Should_SetupLocalProject_When_ValidLocalPathProvided()
        {
            // Arrange
            string localProjectPath = Path.Combine(Path.GetTempPath(), "LocalProject");
            Directory.CreateDirectory(localProjectPath); // Ensure the directory exists

            _mockConsoleService.Setup(m => m.Prompt(It.IsAny<string>(), ConsoleColor.Cyan))
                               .Returns(localProjectPath);

            _mockConsoleService.Setup(m => m.Prompt(It.IsAny<string>(), ConsoleColor.DarkYellow))
                               .Returns("n"); // No sample test file

            // Act
            var config = _projectInitializerService.Initialize();

            // Assert
            Assert.Equal(localProjectPath, config.ProjectPath);
            Assert.False(config.IsGitRepository);
            Assert.Equal(Path.Combine(localProjectPath, _projectSettings.SourceFolder), config.SrcFolder);
            Assert.Equal(Path.Combine(localProjectPath, _projectSettings.TestsFolder), config.TestsFolder);
            Assert.Empty(config.SampleUnitTestContent);
        }

        [Fact]
        public void Initialize_Should_SetupGitRepository_When_ValidGitUrlProvided()
        {
            // Arrange
            string gitRepoUrl = "https://github.com/test/repo.git";

            _mockConsoleService.Setup(m => m.Prompt(It.IsAny<string>(), ConsoleColor.Cyan))
                               .Returns(gitRepoUrl);

            _mockConsoleService.Setup(m => m.Prompt(It.IsAny<string>(), ConsoleColor.DarkYellow))
                               .Returns("n"); // No sample test file

            // Act
            var config = _projectInitializerService.Initialize();

            // Assert
            Assert.True(config.IsGitRepository);
            Assert.Equal(gitRepoUrl, config.GitRepositoryUrl);
            Assert.Equal(_gitSettings.LocalRepositoryPath, config.ProjectPath);
            Assert.Equal(Path.Combine(_gitSettings.LocalRepositoryPath, _projectSettings.SourceFolder), config.SrcFolder);
            Assert.Equal(Path.Combine(_gitSettings.LocalRepositoryPath, _projectSettings.TestsFolder), config.TestsFolder);
            Assert.Empty(config.SampleUnitTestContent);
        }

        [Fact]
        public void Initialize_Should_CreateGitRepositoryDirectory_If_NotExists()
        {
            // Arrange
            string gitRepoUrl = "https://github.com/test/repo.git";

            if (Directory.Exists(_gitSettings.LocalRepositoryPath))
                Directory.Delete(_gitSettings.LocalRepositoryPath, true); // Ensure fresh test

            _mockConsoleService.Setup(m => m.Prompt(It.IsAny<string>(), ConsoleColor.Cyan))
                               .Returns(gitRepoUrl);

            _mockConsoleService.Setup(m => m.Prompt(It.IsAny<string>(), ConsoleColor.DarkYellow))
                               .Returns("n"); // No sample test file

            // Act
            var config = _projectInitializerService.Initialize();

            // Assert
            Assert.True(Directory.Exists(_gitSettings.LocalRepositoryPath));
            Assert.True(config.IsGitRepository);
        }

        [Fact]
        public void Initialize_Should_Reprompt_On_InvalidInput()
        {
            // Arrange
            _mockConsoleService.SetupSequence(m => m.Prompt(It.IsAny<string>(), ConsoleColor.Cyan))
                               .Returns("invalid-path") // Invalid input
                               .Returns("https://github.com/test/repo.git"); // Valid Git URL

            _mockConsoleService.Setup(m => m.WriteColored("Invalid input. Please enter a valid local path or a Git repository URL.", ConsoleColor.Red));

            _mockConsoleService.Setup(m => m.Prompt(It.IsAny<string>(), ConsoleColor.DarkYellow))
                               .Returns("n"); // No sample test file

            // Act
            var config = _projectInitializerService.Initialize();

            // Assert
            _mockConsoleService.Verify(m => m.WriteColored("Invalid input. Please enter a valid local path or a Git repository URL.", ConsoleColor.Red), Times.Once);
            Assert.True(config.IsGitRepository);
        }

        [Fact]
        public void Initialize_Should_Set_SampleUnitTestContent_When_ValidFileProvided()
        {
            // Arrange
            string localProjectPath = Path.Combine(Path.GetTempPath(), "LocalProject");
            Directory.CreateDirectory(localProjectPath);

            string sampleTestFilePath = Path.Combine(Path.GetTempPath(), "SampleTest.cs");
            File.WriteAllText(sampleTestFilePath, "Sample Unit Test Content");

            _mockConsoleService.Setup(m => m.Prompt(It.IsAny<string>(), ConsoleColor.Cyan))
                               .Returns(localProjectPath);

            _mockConsoleService.Setup(m => m.Prompt(It.IsAny<string>(), ConsoleColor.DarkYellow))
                               .Returns("y"); // Wants to provide a sample file

            _mockConsoleService.Setup(m => m.Prompt(It.IsAny<string>(), ConsoleColor.Yellow))
                               .Returns(sampleTestFilePath);

            // Act
            var config = _projectInitializerService.Initialize();

            // Assert
            Assert.Equal("Sample Unit Test Content", config.SampleUnitTestContent);
        }

        [Fact]
        public void Initialize_Should_Reprompt_When_InvalidSampleFilePathProvided()
        {
            // Arrange
            string localProjectPath = Path.Combine(Path.GetTempPath(), "LocalProject");
            Directory.CreateDirectory(localProjectPath);

            string invalidSampleTestFilePath = Path.Combine(Path.GetTempPath(), "InvalidSampleTest.cs");

            _mockConsoleService.Setup(m => m.Prompt(It.IsAny<string>(), ConsoleColor.Cyan))
                               .Returns(localProjectPath);

            _mockConsoleService.Setup(m => m.Prompt(It.IsAny<string>(), ConsoleColor.DarkYellow))
                               .Returns("y"); // Wants to provide a sample file

            _mockConsoleService.SetupSequence(m => m.Prompt(It.IsAny<string>(), ConsoleColor.Yellow))
                               .Returns(invalidSampleTestFilePath) // Invalid path
                               .Returns(invalidSampleTestFilePath) // Invalid again
                               .Returns("valid-test.cs"); // Finally valid (but not used in test)

            _mockConsoleService.Setup(m => m.WriteColored("Invalid file path. Please enter a valid sample unit test file path:", ConsoleColor.Red));

            // Act
            var config = _projectInitializerService.Initialize();

            // Assert
            _mockConsoleService.Verify(m => m.WriteColored("Invalid file path. Please enter a valid sample unit test file path:", ConsoleColor.Red), Times.Exactly(3));
        }

        [Fact]
        public void Initialize_Should_ThrowException_When_MissingProjectSettings()
        {
            // Arrange
            var invalidSettings = new ProjectSettings();
            var invalidProjectSettingsOptions = Options.Create(invalidSettings);
            var service = new ProjectInitializerService(invalidProjectSettingsOptions, Options.Create(_gitSettings), _mockConsoleService.Object);

            _mockConsoleService.Setup(m => m.Prompt(It.IsAny<string>(), ConsoleColor.Cyan))
                               .Returns("https://github.com/test/repo.git");

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => service.Initialize());
            Assert.Equal(nameof(invalidSettings.SourceFolder), ex.Message);
        }
    }
}
