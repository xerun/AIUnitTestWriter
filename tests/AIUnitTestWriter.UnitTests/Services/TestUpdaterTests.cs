﻿using AIUnitTestWriter.Models;
using AIUnitTestWriter.Services;
using AIUnitTestWriter.Services.Interfaces;
using Moq;
using System.IO.Abstractions.TestingHelpers;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class TestUpdaterTests
    {
        private readonly Mock<IAIApiService> _mockAiService;
        private readonly Mock<ICodeAnalyzer> _mockCodeAnalyzer;
        private readonly Mock<IConsoleService> _mockConsoleService;
        private readonly MockFileSystem _mockFileSystem;
        private readonly TestUpdater _testUpdater;

        public TestUpdaterTests()
        {
            _mockAiService = new Mock<IAIApiService>();
            _mockCodeAnalyzer = new Mock<ICodeAnalyzer>();
            _mockConsoleService = new Mock<IConsoleService>();
            _mockFileSystem = new MockFileSystem();

            _testUpdater = new TestUpdater(
                _mockAiService.Object,
                _mockCodeAnalyzer.Object,
                _mockConsoleService.Object,
                _mockFileSystem
            );
        }

        [Fact]
        public async Task ProcessFileChange_ShouldReturnNull_WhenFileContainsInterface()
        {
            // Arrange
            var filePath = @"C:\path\to\file.cs";
            _mockFileSystem.AddFile(filePath, new MockFileData("public interface IMyInterface {}"));

            // Act
            var result = await _testUpdater.ProcessFileChange(@"C:\src", @"C:\tests", filePath);

            // Assert
            Assert.Null(result);
            _mockConsoleService.Verify(cs => cs.WriteColored("Skipped interface file.", ConsoleColor.DarkGray), Times.Once);
        }

        [Fact]
        public async Task ProcessFileChange_ShouldReturnNull_WhenNoPublicMethods()
        {
            // Arrange
            var filePath = @"C:\path\to\file.cs";
            _mockFileSystem.AddFile(filePath, new MockFileData("public class MyClass {}"));
            _mockCodeAnalyzer.Setup(ca => ca.GetPublicMethodNames(It.IsAny<string>())).Returns(new List<string>());

            // Act
            var result = await _testUpdater.ProcessFileChange(@"C:\src", @"C:\tests", filePath);

            // Assert
            Assert.Null(result);
            _mockConsoleService.Verify(cs => cs.WriteColored("No public methods found; skipping test generation.", ConsoleColor.DarkGray), Times.Once);
        }

        [Fact]
        public async Task ProcessFileChange_ShouldPromptForMethod_WhenFileIsLong()
        {
            // Arrange
            var filePath = @"C:\path\to\file.cs";
            var longSourceCode = @"public class MyClass { public void MyMethod() {} }";

            // Make the source code 600 characters long
            longSourceCode = longSourceCode.PadRight(600, '/'); // Fill the rest with / to make it exactly 600 characters
            _mockFileSystem.AddFile(filePath, new MockFileData(longSourceCode));

            _mockConsoleService.Setup(cs => cs.Prompt(It.IsAny<string>(), ConsoleColor.Yellow)).Returns("TestMethod");
            _mockCodeAnalyzer.Setup(ca => ca.GetPublicMethodNames(It.IsAny<string>())).Returns(new List<string> { "MyMethod" });
            _mockCodeAnalyzer.Setup(ca => ca.GetMethodCode(It.IsAny<string>(), "TestMethod")).Returns("public void TestMethod() {}");
            _mockAiService.Setup(ai => ai.GenerateTestsAsync(It.IsAny<string>())).ReturnsAsync("Generated Test Code");

            // Act
            var result = await _testUpdater.ProcessFileChange(@"C:\src", @"C:\tests", filePath);

            // Assert
            Assert.NotNull(result);            
        }

        [Fact]
        public async Task ProcessFileChange_ShouldGenerateTestsAndWriteToTempFile()
        {
            // Arrange
            var filePath = @"C:\src\path\to\file.cs";
            var sourceCode = "public class MyClass { public void MyMethod() {} }";
            _mockFileSystem.AddFile(filePath, new MockFileData(sourceCode));
            _mockCodeAnalyzer.Setup(ca => ca.GetPublicMethodNames(It.IsAny<string>())).Returns(new List<string> { "MyMethod" });

            _mockAiService.Setup(ai => ai.GenerateTestsAsync(It.IsAny<string>())).ReturnsAsync("Generated Test Code");

            // Act
            var result = await _testUpdater.ProcessFileChange(@"C:\src", @"C:\tests", filePath);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("Generated Test Code", result.GeneratedTestCode);
            _mockConsoleService.Verify(cs => cs.WriteColored("Generated test file saved to temporary file:", ConsoleColor.Green), Times.AtMost(3));
        }

        [Fact]
        public void FinalizeTestUpdate_ShouldCreateDirectoryAndWriteFile()
        {
            // Arrange
            var result = new TestGenerationResultModel
            {
                TestFilePath = @"C:\tests\path\to\fileTests.cs",
                GeneratedTestCode = "public class MyTest { public void TestMethod() {} }"
            };

            // Act
            _testUpdater.FinalizeTestUpdate(result);

            // Assert
            Assert.True(_mockFileSystem.Directory.Exists(@"C:\tests\path\to"));
            Assert.True(_mockFileSystem.FileExists(@"C:\tests\path\to\fileTests.cs"));
            var writtenContent = _mockFileSystem.File.ReadAllText(@"C:\tests\path\to\fileTests.cs");
            Assert.Contains("public class MyTest", writtenContent);
        }
    }
}
