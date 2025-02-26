using AIUnitTestWriter.Services;
using System.Diagnostics;
using System.Text;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class GitProcessServiceTests
    {
        [Fact]
        public void RunCommand_ShouldReturnOutput_WhenGitCommandSucceeds()
        {
            // Arrange
            var expectedOutput = "Success output";
            var gitProcessService = new GitProcessService();
            var command = "status";
            var workingDirectory = "/fake/path";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                }
            };

            var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(expectedOutput));
            var errorStream = new MemoryStream();

            var outputReader = new StreamReader(outputStream);
            var errorReader = new StreamReader(errorStream);

            typeof(Process).GetProperty("StandardOutput")!.SetValue(process, outputReader);
            typeof(Process).GetProperty("StandardError")!.SetValue(process, errorReader);

            // Act
            var result = gitProcessService.RunCommand(command, workingDirectory);

            // Assert
            Assert.Equal(expectedOutput, result);
        }

        [Fact]
        public void RunCommand_ShouldReturnErrorOutput_WhenGitCommandFails()
        {
            // Arrange
            var expectedError = "Error: Something went wrong";
            var gitProcessService = new GitProcessService();
            var command = "checkout invalid-branch";
            var workingDirectory = "/fake/path";

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                }
            };

            var outputStream = new MemoryStream();
            var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(expectedError));

            var outputReader = new StreamReader(outputStream);
            var errorReader = new StreamReader(errorStream);

            typeof(Process).GetProperty("StandardOutput")!.SetValue(process, outputReader);
            typeof(Process).GetProperty("StandardError")!.SetValue(process, errorReader);

            // Act
            var result = gitProcessService.RunCommand(command, workingDirectory);

            // Assert
            Assert.Equal(expectedError, result);
        }
    }
}
