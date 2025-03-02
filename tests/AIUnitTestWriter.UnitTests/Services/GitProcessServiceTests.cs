using AIUnitTestWriter.Services.Git;
using AIUnitTestWriter.Services.Interfaces;
using Moq;
using System.Diagnostics;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class GitProcessServiceTests
    {
        private readonly Mock<IGitProcessFactory> _processFactory;
        private readonly Mock<IProcessWrapper> _mockProcess;
        private readonly GitProcessService _gitProcessService;

        public GitProcessServiceTests()
        {
            _processFactory = new Mock<IGitProcessFactory>();
            _mockProcess = new Mock<IProcessWrapper>();
            _gitProcessService = new GitProcessService(_processFactory.Object);
        }

        [Fact]
        public void RunCommand_ShouldReturnOutput_WhenCommandSucceeds()
        {
            // Arrange
            var command = "status";
            var workingDirectory = @"C:\repo";
            var expectedOutput = "On branch main\nnothing to commit, working tree clean";

            _mockProcess.Setup(p => p.StandardOutput).Returns(new StreamReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(expectedOutput))));
            _mockProcess.Setup(p => p.StandardError).Returns(new StreamReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(string.Empty))));
            _mockProcess.Setup(p => p.ExitCode).Returns(0);
            _mockProcess.Setup(p => p.WaitForExit());

            _processFactory.Setup(f => f.StartProcess(It.IsAny<ProcessStartInfo>())).Returns(_mockProcess.Object);

            // Act
            var result = _gitProcessService.RunCommand(command, workingDirectory);

            // Assert
            Assert.Equal(expectedOutput, result);
        }

        [Fact]
        public void RunCommand_ShouldThrowException_WhenProcessCannotStart()
        {
            // Arrange
            var command = "status";
            var workingDirectory = @"C:\repo";

            _processFactory.Setup(f => f.StartProcess(It.IsAny<ProcessStartInfo>())).Returns((IProcessWrapper)null);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _gitProcessService.RunCommand(command, workingDirectory));
        }

        [Fact]
        public void RunCommand_ShouldThrowException_WhenCommandFails()
        {
            // Arrange
            var command = "status";
            var workingDirectory = @"C:\repo";
            var errorMessage = "fatal: Not a git repository (or any of the parent directories): .git";

            _mockProcess.Setup(p => p.StandardOutput).Returns(new StreamReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(string.Empty))));
            _mockProcess.Setup(p => p.StandardError).Returns(new StreamReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(errorMessage))));
            _mockProcess.Setup(p => p.ExitCode).Returns(1);
            _mockProcess.Setup(p => p.WaitForExit());

            _processFactory.Setup(f => f.StartProcess(It.IsAny<ProcessStartInfo>())).Returns(_mockProcess.Object);

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => _gitProcessService.RunCommand(command, workingDirectory));
            Assert.Equal($"Git Error: {errorMessage}", exception.Message);
        }
    }
}
