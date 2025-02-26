using AIUnitTestWriter.Services;
using AIUnitTestWriter.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class ProjectInitializerServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IConsoleService> _mockConsoleService;
        private readonly ProjectInitializerService _projectInitializerService;

        public ProjectInitializerServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConsoleService = new Mock<IConsoleService>();

            _projectInitializerService = new ProjectInitializerService(
                _mockConfiguration.Object,
                _mockConsoleService.Object
            );
        }

        [Fact]
        public void Initialize_ShouldSetLocalProjectPath_WhenValidLocalPathIsEntered()
        {
            // Arrange
            string projectPath = "C:\\MyProject";
            _mockConsoleService.Setup(cs => cs.Prompt(It.IsAny<string>(), ConsoleColor.Cyan))
                               .Returns(projectPath);

            Directory.CreateDirectory(projectPath); // Simulate the directory existing

            _mockConfiguration.Setup(c => c["Project:SourceFolder"]).Returns("src");
            _mockConfiguration.Setup(c => c["Project:TestsFolder"]).Returns("tests");

            _mockConsoleService.Setup(cs => cs.Prompt(It.IsAny<string>(), ConsoleColor.DarkYellow))
                               .Returns("n"); // User does not provide a sample unit test file

            // Act
            var result = _projectInitializerService.Initialize();

            // Assert
            Assert.False(result.IsGitRepository);
            Assert.Equal(projectPath, result.ProjectPath);
            Assert.Equal(Path.Combine(projectPath, "src"), result.SrcFolder);
            Assert.Equal(Path.Combine(projectPath, "tests"), result.TestsFolder);
            Assert.Equal(string.Empty, result.SampleUnitTestContent);
        }

        [Fact]
        public void Initialize_ShouldSetGitRepository_WhenValidGitUrlIsEntered()
        {
            // Arrange
            string gitUrl = "https://github.com/example/repo.git";
            string localPath = "C:\\GitClone";

            _mockConsoleService.SetupSequence(cs => cs.Prompt(It.IsAny<string>(), ConsoleColor.Cyan))
                               .Returns(gitUrl) // First input: Git URL
                               .Returns(localPath); // Second input: local path

            _mockConfiguration.Setup(c => c["Project:SourceFolder"]).Returns("src");
            _mockConfiguration.Setup(c => c["Project:TestsFolder"]).Returns("tests");

            _mockConsoleService.Setup(cs => cs.Prompt(It.IsAny<string>(), ConsoleColor.DarkYellow))
                               .Returns("n"); // User does not provide a sample unit test file

            // Act
            var result = _projectInitializerService.Initialize();

            // Assert
            Assert.True(result.IsGitRepository);
            Assert.Equal(gitUrl, result.GitRepositoryUrl);
            Assert.Equal(localPath, result.ProjectPath);
            Assert.Equal(Path.Combine(localPath, "src"), result.SrcFolder);
            Assert.Equal(Path.Combine(localPath, "tests"), result.TestsFolder);
            Assert.Equal(string.Empty, result.SampleUnitTestContent);
        }

        [Fact]
        public void Initialize_ShouldRePromptOnInvalidInput()
        {
            // Arrange
            string invalidInput = "invalidPath";
            string validPath = "C:\\ValidProject";

            _mockConsoleService.SetupSequence(cs => cs.Prompt(It.IsAny<string>(), ConsoleColor.Cyan))
                               .Returns(invalidInput) // First input: invalid path
                               .Returns(validPath); // Second input: valid path

            _mockConsoleService.Setup(cs => cs.WriteColored("Invalid input. Please enter a valid local path or a Git repository URL.", ConsoleColor.Red));

            Directory.CreateDirectory(validPath); // Simulate valid directory existing

            _mockConfiguration.Setup(c => c["Project:SourceFolder"]).Returns("src");
            _mockConfiguration.Setup(c => c["Project:TestsFolder"]).Returns("tests");

            _mockConsoleService.Setup(cs => cs.Prompt(It.IsAny<string>(), ConsoleColor.DarkYellow))
                               .Returns("n"); // User does not provide a sample unit test file

            // Act
            var result = _projectInitializerService.Initialize();

            // Assert
            Assert.False(result.IsGitRepository);
            Assert.Equal(validPath, result.ProjectPath);
            Assert.Equal(Path.Combine(validPath, "src"), result.SrcFolder);
            Assert.Equal(Path.Combine(validPath, "tests"), result.TestsFolder);
            _mockConsoleService.Verify(cs => cs.WriteColored("Invalid input. Please enter a valid local path or a Git repository URL.", ConsoleColor.Red), Times.Once);
        }

        [Fact]
        public void Initialize_ShouldHandleSampleUnitTestFile()
        {
            // Arrange
            string projectPath = "C:\\MyProject";
            string sampleTestFile = "C:\\SampleTest.cs";
            string sampleContent = "public class SampleTest {}";

            _mockConsoleService.Setup(cs => cs.Prompt(It.IsAny<string>(), ConsoleColor.Cyan))
                               .Returns(projectPath);

            Directory.CreateDirectory(projectPath); // Simulate the directory existing
            File.WriteAllText(sampleTestFile, sampleContent); // Create a temporary sample test file

            _mockConfiguration.Setup(c => c["Project:SourceFolder"]).Returns("src");
            _mockConfiguration.Setup(c => c["Project:TestsFolder"]).Returns("tests");

            _mockConsoleService.Setup(cs => cs.Prompt(It.IsAny<string>(), ConsoleColor.DarkYellow))
                               .Returns("y"); // User wants to provide a sample unit test file

            _mockConsoleService.Setup(cs => cs.Prompt(It.IsAny<string>(), ConsoleColor.Yellow))
                               .Returns(sampleTestFile); // User provides sample test file path

            // Act
            var result = _projectInitializerService.Initialize();

            // Assert
            Assert.Equal(sampleContent, result.SampleUnitTestContent);
        }

        [Fact]
        public void Initialize_ShouldHandleInvalidSampleUnitTestFilePath()
        {
            // Arrange
            string projectPath = "C:\\MyProject";
            string invalidFilePath = "C:\\InvalidTest.cs";
            string validFilePath = "C:\\ValidTest.cs";
            string validFileContent = "public class ValidTest {}";

            _mockConsoleService.Setup(cs => cs.Prompt(It.IsAny<string>(), ConsoleColor.Cyan))
                               .Returns(projectPath);

            Directory.CreateDirectory(projectPath); // Simulate the directory existing
            File.WriteAllText(validFilePath, validFileContent); // Create a valid sample test file

            _mockConfiguration.Setup(c => c["Project:SourceFolder"]).Returns("src");
            _mockConfiguration.Setup(c => c["Project:TestsFolder"]).Returns("tests");

            _mockConsoleService.Setup(cs => cs.Prompt(It.IsAny<string>(), ConsoleColor.DarkYellow))
                               .Returns("y"); // User wants to provide a sample unit test file

            _mockConsoleService.SetupSequence(cs => cs.Prompt(It.IsAny<string>(), ConsoleColor.Yellow))
                               .Returns(invalidFilePath) // First attempt: invalid file path
                               .Returns(validFilePath); // Second attempt: valid file path

            _mockConsoleService.Setup(cs => cs.WriteColored("Invalid file path. Please enter a valid sample unit test file path:", ConsoleColor.Red));

            // Act
            var result = _projectInitializerService.Initialize();

            // Assert
            Assert.Equal(validFileContent, result.SampleUnitTestContent);
            _mockConsoleService.Verify(cs => cs.WriteColored("Invalid file path. Please enter a valid sample unit test file path:", ConsoleColor.Red), Times.Once);
        }
    }
}
