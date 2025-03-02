using AIUnitTestWriter.Models;
using AIUnitTestWriter.Services.Interfaces;
using System.IO.Abstractions;

namespace AIUnitTestWriter.Services
{
    public class TestUpdater : ITestUpdater
    {
        private readonly IAIApiService _aiService;
        private readonly ICodeAnalyzer _codeAnalyzer;
        private readonly IConsoleService _consoleService;
        private readonly IFileSystem _fileSystem;

        private const int SourceLineThreshold = 500;
        private const int TestLineThreshold = 500;

        public TestUpdater(
            IAIApiService aiService,
            ICodeAnalyzer codeAnalyzer,
            IConsoleService consoleService,
            IFileSystem fileSystem)
        {
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _codeAnalyzer = codeAnalyzer ?? throw new ArgumentNullException(nameof(codeAnalyzer));
            _consoleService = consoleService ?? throw new ArgumentNullException(nameof(consoleService));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        /// <summary>
        /// Processes the changed file. If the file is very long, it prompts for the changed method,
        /// then only that method (and its dependencies, if desired) is fed to the AI.
        /// In manual mode, returns a TestGenerationResultModel for approval.
        /// In auto mode, finalizes immediately and returns null.
        /// </summary>
        public async Task<TestGenerationResultModel?> ProcessFileChange(string srcFolder, string testsFolder, string filePath, string sampleUnitTest = "", bool promptUser = true)
        {
            try
            {
                // Read the source file.
                var sourceCode = _fileSystem.File.ReadAllText(filePath);
                if (sourceCode.Contains("interface"))
                {
                    _consoleService.WriteColored("Skipped interface file.", ConsoleColor.DarkGray);
                    return null;
                }

                // Analyze the source file for public methods.
                var publicMethods = _codeAnalyzer.GetPublicMethodNames(sourceCode);
                if (publicMethods?.Count == 0)
                {
                    _consoleService.WriteColored("No public methods found; skipping test generation.", ConsoleColor.DarkGray);
                    return null;
                }

                // Optionally, extract just the changed method.
                var methodCodeToSend = sourceCode;
                string? changedMethodName = null;
                int sourceLineCount = sourceCode.Split('\n').Length;

                if (sourceLineCount > SourceLineThreshold)
                {
                    _consoleService.WriteColored("The source file is very long.", ConsoleColor.Yellow);
                    changedMethodName = _consoleService.Prompt("Please enter the name of the changed method:", ConsoleColor.Yellow);

                    var extractedMethod = _codeAnalyzer.GetMethodCode(sourceCode, changedMethodName);
                    if (!string.IsNullOrWhiteSpace(extractedMethod))
                    {
                        methodCodeToSend = extractedMethod;
                    }
                    else
                    {
                        _consoleService.WriteColored("Method not found; using full source.", ConsoleColor.Red);
                    }
                }

                // Compute the test file path.
                var relativePath = _fileSystem.Path.GetRelativePath(srcFolder, filePath);
                var testFilePath = _fileSystem.Path.Combine(testsFolder, relativePath);
                testFilePath = _fileSystem.Path.Combine(_fileSystem.Path.GetDirectoryName(testFilePath),
                    _fileSystem.Path.GetFileNameWithoutExtension(testFilePath) + "Tests" + _fileSystem.Path.GetExtension(testFilePath));

                // Read any existing tests.
                var existingTests = _fileSystem.File.Exists(testFilePath) ? _fileSystem.File.ReadAllText(testFilePath) : string.Empty;

                // Build the prompt for the AI.
                var prompt = GeneratePrompt(methodCodeToSend, existingTests, sampleUnitTest);

                _consoleService.WriteColored("Sending code to AI for test generation...", ConsoleColor.Blue);
                var aiResponse = await _aiService.GenerateTestsAsync(prompt);
                if (string.IsNullOrWhiteSpace(aiResponse))
                {
                    _consoleService.WriteColored("AI returned an empty response.", ConsoleColor.Yellow);
                    return null;
                }

                // Write the generated tests to a temporary file.
                var tempFileName = _fileSystem.Path.GetFileNameWithoutExtension(testFilePath) + "_temp" + _fileSystem.Path.GetExtension(testFilePath);
                var tempFilePath = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), tempFileName);
                _fileSystem.File.WriteAllText(tempFilePath, aiResponse);
                _consoleService.WriteColored($"Generated test file saved to temporary file: {tempFilePath}", ConsoleColor.Green);

                // Optionally, try to open the temporary file.
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = tempFilePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    _consoleService.WriteColored(("Unable to open the temporary file automatically: " + ex.Message), ConsoleColor.Red);
                }

                if (!promptUser)
                {
                    var result = new TestGenerationResultModel
                    {
                        TempFilePath = tempFilePath,
                        TestFilePath = testFilePath,
                        GeneratedTestCode = aiResponse
                    };
                    // Auto mode: finalize immediately.
                    FinalizeTestUpdate(result);
                    _consoleService.WriteColored($"Test file auto-updated at: {testFilePath}", ConsoleColor.Green);
                    _consoleService.WriteColored("Auto-detect mode activated. Monitoring code changes.", ConsoleColor.Blue);
                    return null;
                }
                else
                {
                    return new TestGenerationResultModel
                    {
                        TempFilePath = tempFilePath,
                        TestFilePath = testFilePath,
                        GeneratedTestCode = aiResponse
                    };
                }
            }
            catch (Exception ex)
            {
                _consoleService.WriteColored(ex.Message, ConsoleColor.Red);
                return null;
            }
        }

        /// <summary>
        /// Finalizes the test update by writing the generated code to the test file.
        /// </summary>
        public void FinalizeTestUpdate(TestGenerationResultModel result)
        {
            _fileSystem.Directory.CreateDirectory(_fileSystem.Path.GetDirectoryName(result.TestFilePath));
            _fileSystem.File.WriteAllText(result.TestFilePath, result.GeneratedTestCode);
        }

        private string GeneratePrompt(string methodCode, string existingTests, string sampleUnitTest)
        {
            var prompt = $@"Act as an expert C# developer. Generate xUnit tests for the following code with 100% code coverage.
The tests should follow the naming convention: MethodName_WhenCondition_ReturnsExpectedResult.
Only generate tests for public methods and constructors. Do not create tests for interfaces or private methods.
If existing tests are provided, update only the tests related to the method.";

            if (!string.IsNullOrWhiteSpace(sampleUnitTest))
            {
                prompt += $@"

Use the following sample unit test as a reference for the structure and style:
{sampleUnitTest}";
            }

            prompt += $@"

Method Code:
{methodCode}

Existing Tests:
{existingTests}

Provide the complete updated test file content as output.";

            return prompt;
        }
    }
}
