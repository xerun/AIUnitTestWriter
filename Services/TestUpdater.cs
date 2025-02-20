using AIUnitTestWriter.Extensions;
using AIUnitTestWriter.Models;
using AIUnitTestWriter.Services.Interfaces;

namespace AIUnitTestWriter.Services
{
    public class TestUpdater : ITestUpdater
    {
        private readonly IAIApiService _aiService;
        private readonly ICodeAnalyzer _codeAnalyzer;

        private const int SourceLineThreshold = 500;
        private const int TestLineThreshold = 500;

        public TestUpdater(
            IAIApiService aiService,
            ICodeAnalyzer codeAnalyzer)
        {
            _aiService = aiService;
            _codeAnalyzer = codeAnalyzer;
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
                var sourceCode = File.ReadAllText(filePath);
                if (sourceCode.Contains("interface"))
                {
                    "Skipped interface file.".WriteColored(ConsoleColor.DarkGray);
                    return null;
                }

                // Analyze the source file for public methods.
                var publicMethods = _codeAnalyzer.GetPublicMethodNames(sourceCode);
                if (publicMethods.Count == 0)
                {
                    "No public methods found; skipping test generation.".WriteColored(ConsoleColor.DarkGray);
                    return null;
                }

                // Optionally, extract just the changed method.
                var methodCodeToSend = sourceCode;
                string? changedMethodName = null;
                int sourceLineCount = sourceCode.Split('\n').Length;

                if (sourceLineCount > SourceLineThreshold)
                {
                    "The source file is very long.".WriteColored(ConsoleColor.Yellow);
                    "Please enter the name of the changed method:".WriteColored(ConsoleColor.Yellow);
                    changedMethodName = Console.ReadLine()?.Trim();

                    var extractedMethod = _codeAnalyzer.GetMethodCode(sourceCode, changedMethodName);
                    if (!string.IsNullOrWhiteSpace(extractedMethod))
                    {
                        methodCodeToSend = extractedMethod;
                    }
                    else
                    {
                        "Method not found; using full source.".WriteColored(ConsoleColor.Red);
                    }
                }

                // Compute the test file path.
                var relativePath = Path.GetRelativePath(srcFolder, filePath);
                var testFilePath = Path.Combine(testsFolder, relativePath);
                testFilePath = Path.Combine(Path.GetDirectoryName(testFilePath),
                    Path.GetFileNameWithoutExtension(testFilePath) + "Tests" + Path.GetExtension(testFilePath));

                // Read any existing tests.
                var existingTests = File.Exists(testFilePath) ? File.ReadAllText(testFilePath) : string.Empty;

                // Build the prompt for the AI.
                var prompt = GeneratePrompt(methodCodeToSend, existingTests, sampleUnitTest);

                "Sending code to AI for test generation...".WriteColored(ConsoleColor.Blue);
                var aiResponse = await _aiService.GenerateTestsAsync(prompt);
                if (string.IsNullOrWhiteSpace(aiResponse))
                {
                    "AI returned an empty response.".WriteColored(ConsoleColor.Yellow);
                    return null;
                }

                // Write the generated tests to a temporary file.
                var tempFileName = Path.GetFileNameWithoutExtension(testFilePath) + "_temp" + Path.GetExtension(testFilePath);
                var tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);
                File.WriteAllText(tempFilePath, aiResponse);
                $"Generated test file saved to temporary file: {tempFilePath}".WriteColored(ConsoleColor.Green);

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
                    ("Unable to open the temporary file automatically: " + ex.Message).WriteColored(ConsoleColor.Red);
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
                    $"Test file auto-updated at: {testFilePath}".WriteColored(ConsoleColor.Green);
                    "Auto-detect mode activated. Monitoring code changes.".WriteColored(ConsoleColor.Blue);
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
                ex.Message.WriteColored(ConsoleColor.Red);
                return null;
            }
        }

        /// <summary>
        /// Finalizes the test update by writing the generated code to the test file.
        /// </summary>
        public void FinalizeTestUpdate(TestGenerationResultModel result)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(result.TestFilePath));
            File.WriteAllText(result.TestFilePath, result.GeneratedTestCode);
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
