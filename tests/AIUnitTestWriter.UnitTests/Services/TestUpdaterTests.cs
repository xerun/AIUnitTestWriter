using AIUnitTestWriter.Models;
using AIUnitTestWriter.Services;
using AIUnitTestWriter.Services.Interfaces;
using Moq;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class TestUpdaterTests
    {
        private readonly Mock<IAIApiService> _mockAiService;
        private readonly Mock<ICodeAnalyzer> _mockCodeAnalyzer;
        private readonly Mock<IConsoleService> _mockConsoleService;
        private readonly TestUpdater _testUpdater;

        public TestUpdaterTests()
        {
            _mockAiService = new Mock<IAIApiService>();
            _mockCodeAnalyzer = new Mock<ICodeAnalyzer>();
            _mockConsoleService = new Mock<IConsoleService>();

            _testUpdater = new TestUpdater(_mockAiService.Object, _mockCodeAnalyzer.Object, _mockConsoleService.Object);
        }

        [Fact]
        public async Task ProcessFileChange_SkipsInterfaceFiles_ReturnsNull()
        {
            // Arrange
            string filePath = "test.cs";
            File.WriteAllText(filePath, "public interface ITest { void Method(); }");

            // Act
            var result = await _testUpdater.ProcessFileChange("src", "tests", filePath);

            // Assert
            Assert.Null(result);
            _mockConsoleService.Verify(m => m.WriteColored("Skipped interface file.", ConsoleColor.DarkGray), Times.Once);

            // Cleanup
            File.Delete(filePath);
        }

        [Fact]
        public async Task ProcessFileChange_NoPublicMethods_ReturnsNull()
        {
            // Arrange
            string filePath = "test.cs";
            File.WriteAllText(filePath, "class TestClass { private void Method() {} }");

            _mockCodeAnalyzer.Setup(m => m.GetPublicMethodNames(It.IsAny<string>())).Returns(new List<string>());

            // Act
            var result = await _testUpdater.ProcessFileChange("src", "tests", filePath);

            // Assert
            Assert.Null(result);
            _mockConsoleService.Verify(m => m.WriteColored("No public methods found; skipping test generation.", ConsoleColor.DarkGray), Times.Once);

            // Cleanup
            File.Delete(filePath);
        }

        [Fact]
        public async Task ProcessFileChange_GeneratesTestFile_ReturnsResultModel()
        {
            // Arrange
            string filePath = "src/TestClass.cs";
            File.WriteAllText(filePath, "public class TestClass { public void Method() {} }");

            _mockCodeAnalyzer.Setup(m => m.GetPublicMethodNames(It.IsAny<string>())).Returns(new List<string> { "Method" });
            _mockAiService.Setup(m => m.GenerateTestsAsync(It.IsAny<string>())).ReturnsAsync("Generated Test Code");

            // Act
            var result = await _testUpdater.ProcessFileChange("src", "tests", filePath, promptUser: true);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("_temp", result.TempFilePath);
            Assert.Contains("Generated Test Code", result.GeneratedTestCode);

            // Cleanup
            File.Delete(filePath);
            File.Delete(result.TempFilePath);
        }

        [Fact]
        public async Task ProcessFileChange_LongFilePromptsForMethod_ReturnsResultModel()
        {
            // Arrange
            string filePath = "src/LongTestClass.cs";
            var longCode = new string('\n', 600) + "public class LongTestClass { public void LongMethod() {} }";
            File.WriteAllText(filePath, longCode);

            _mockCodeAnalyzer.Setup(m => m.GetPublicMethodNames(It.IsAny<string>())).Returns(new List<string> { "LongMethod" });
            _mockCodeAnalyzer.Setup(m => m.GetMethodCode(It.IsAny<string>(), "LongMethod")).Returns("public void LongMethod() {}");
            _mockConsoleService.Setup(m => m.Prompt(It.IsAny<string>(), ConsoleColor.Yellow)).Returns("LongMethod");
            _mockAiService.Setup(m => m.GenerateTestsAsync(It.IsAny<string>())).ReturnsAsync("Generated Test Code");

            // Act
            var result = await _testUpdater.ProcessFileChange("src", "tests", filePath, promptUser: true);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Generated Test Code", result.GeneratedTestCode);

            // Cleanup
            File.Delete(filePath);
            File.Delete(result.TempFilePath);
        }

        [Fact]
        public async Task ProcessFileChange_AIResponseEmpty_ReturnsNull()
        {
            // Arrange
            string filePath = "src/TestClass.cs";
            File.WriteAllText(filePath, "public class TestClass { public void Method() {} }");

            _mockCodeAnalyzer.Setup(m => m.GetPublicMethodNames(It.IsAny<string>())).Returns(new List<string> { "Method" });
            _mockAiService.Setup(m => m.GenerateTestsAsync(It.IsAny<string>())).ReturnsAsync("");

            // Act
            var result = await _testUpdater.ProcessFileChange("src", "tests", filePath);

            // Assert
            Assert.Null(result);
            _mockConsoleService.Verify(m => m.WriteColored("AI returned an empty response.", ConsoleColor.Yellow), Times.Once);

            // Cleanup
            File.Delete(filePath);
        }

        [Fact]
        public void FinalizeTestUpdate_WritesTestFile()
        {
            // Arrange
            string testFilePath = Path.Combine(Path.GetTempPath(), "FinalizedTest.cs");
            var result = new TestGenerationResultModel
            {
                TestFilePath = testFilePath,
                GeneratedTestCode = "Finalized Test Code"
            };

            // Act
            _testUpdater.FinalizeTestUpdate(result);

            // Assert
            Assert.True(File.Exists(testFilePath));
            Assert.Equal("Finalized Test Code", File.ReadAllText(testFilePath));

            // Cleanup
            File.Delete(testFilePath);
        }

        [Fact]
        public async Task ProcessFileChange_ExceptionHandling_ReturnsNull()
        {
            // Arrange
            string filePath = "src/TestClass.cs";
            File.WriteAllText(filePath, "public class TestClass { public void Method() {} }");

            _mockCodeAnalyzer.Setup(m => m.GetPublicMethodNames(It.IsAny<string>())).Throws(new Exception("Error in code analysis"));

            // Act
            var result = await _testUpdater.ProcessFileChange("src", "tests", filePath);

            // Assert
            Assert.Null(result);
            _mockConsoleService.Verify(m => m.WriteColored("Error in code analysis", ConsoleColor.Red), Times.Once);

            // Cleanup
            File.Delete(filePath);
        }
    }
}
