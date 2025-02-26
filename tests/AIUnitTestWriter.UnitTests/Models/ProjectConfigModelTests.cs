using AIUnitTestWriter.Models;

namespace AIUnitTestWriter.UnitTests.Models
{
    public class ProjectConfigModelTests
    {
        [Fact]
        public void ProjectConfigModel_ShouldSetAndGetPropertiesCorrectly()
        {
            // Arrange
            var config = new ProjectConfigModel
            {
                ProjectPath = "/home/user/project",
                SrcFolder = "src",
                TestsFolder = "tests",
                SampleUnitTestContent = "Sample test content",
                IsGitRepository = true,
                GitRepositoryUrl = "https://github.com/user/repo.git"
            };

            // Act & Assert
            Assert.Equal("/home/user/project", config.ProjectPath);
            Assert.Equal("src", config.SrcFolder);
            Assert.Equal("tests", config.TestsFolder);
            Assert.Equal("Sample test content", config.SampleUnitTestContent);
            Assert.True(config.IsGitRepository);
            Assert.Equal("https://github.com/user/repo.git", config.GitRepositoryUrl);
        }
    }
}
