using AIUnitTestWriter.DTOs;
using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.Models;
using AIUnitTestWriter.SettingOptions;
using Microsoft.Extensions.Options;
using Octokit;
using System.IO.Abstractions;

namespace AIUnitTestWriter.Services
{
    public class TestUpdaterService : ITestUpdaterService
    {
        private readonly IAIApiService _aiService;
        private readonly ICodeAnalyzer _codeAnalyzer;
        private readonly IConsoleService _consoleService;
        private readonly IFileSystem _fileSystem;
        private readonly ISkippedFilesManager _skippedFilesManager;
        private readonly AISettings _aiSettings;

        private const int SourceLineThreshold = 300;
        private const int TestLineThreshold = 300;

        public TestUpdaterService(
            IAIApiService aiService,
            ICodeAnalyzer codeAnalyzer,
            IConsoleService consoleService,
            IFileSystem fileSystem,
            ISkippedFilesManager skippedFilesManager,
            IOptions<AISettings> aiSettings)
        {
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _codeAnalyzer = codeAnalyzer ?? throw new ArgumentNullException(nameof(codeAnalyzer));
            _consoleService = consoleService ?? throw new ArgumentNullException(nameof(consoleService));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _skippedFilesManager = skippedFilesManager ?? throw new ArgumentNullException(nameof(skippedFilesManager));
            _aiSettings = aiSettings?.Value ?? throw new ArgumentNullException(nameof(aiSettings));
        }

        /// <summary>
        /// Processes the changed file. If the file is very long, it prompts for the changed method,
        /// then only that method (and its dependencies, if desired) is fed to the AI.
        /// In manual mode, returns a TestGenerationResultModel for approval.
        /// In auto mode, finalizes immediately and returns null.
        /// </summary>
        public async Task<TestGenerationResultModel?> ProcessFileChangeAsync(FileChangeProcessingDto dto, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if the file is in the predefined skip list
                if (_skippedFilesManager.ShouldSkip(dto.FilePath))
                {
                    _consoleService.WriteColored($"Skipped file (in predefined list): {dto.FilePath}", ConsoleColor.DarkGray);
                    return null;
                }

                // Read the source file.
                var sourceCode = !string.IsNullOrWhiteSpace(dto.NewContent) ? dto.NewContent : _fileSystem.File.ReadAllText(dto.FilePath);
                if (sourceCode.Contains("interface"))
                {
                    _consoleService.WriteColored("Skipped interface file.", ConsoleColor.DarkGray);
                    return null;
                }

                // Analyze the source file for public methods.
                var publicMethods = _codeAnalyzer.GetPublicMethodNames(sourceCode, dto.CodeExtension);
                if (publicMethods?.Count == 0)
                {
                    _consoleService.WriteColored("No public methods found; skipping test generation.", ConsoleColor.DarkGray);
                    return null;
                }


                var methodCodeToSend = sourceCode;
                var codeLines = sourceCode.Split('\n').Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("//")).ToList();
                int effectiveLineCount = codeLines.Count;

                // Extract just the changed method, if lines of code too large.
                if (effectiveLineCount > SourceLineThreshold && (!string.IsNullOrWhiteSpace(dto.OldContent) && !string.IsNullOrWhiteSpace(dto.NewContent)))
                {
                    var changedLines = _codeAnalyzer.GetChangedLines(dto.OldContent, dto.NewContent);
                    var affectedMethods = _codeAnalyzer.GetMethodsAroundLines(sourceCode, changedLines, dto.CodeExtension);
                    if (affectedMethods.Count == 0)
                    {
                        _consoleService.WriteColored("No affected methods found; skipping test generation.", ConsoleColor.DarkGray);
                        return null;
                    }
                    _consoleService.WriteColored("Large file detected. Automatically extracting changed method(s).", ConsoleColor.Yellow);
                    methodCodeToSend = _codeAnalyzer.GetMethodWithDependencies(sourceCode, affectedMethods, dto.CodeExtension);
                }

                // Compute the test file path.
                var testFilePath = TestFilePath(dto);

                // Read any existing tests.
                var existingTests = !string.IsNullOrWhiteSpace(dto.ExistingUnitTest) ? dto.ExistingUnitTest : (_fileSystem.File.Exists(testFilePath) ? _fileSystem.File.ReadAllText(testFilePath) : string.Empty);

                // Build the prompt for the AI.
                var prompt = GeneratePrompt(methodCodeToSend, existingTests, dto.SampleUnitTest);

                _consoleService.WriteColored("Sending code to AI for test generation...", ConsoleColor.Blue);

                var aiResponse = await _aiService.GenerateTestsAsync(prompt, cancellationToken);
                if (string.IsNullOrWhiteSpace(aiResponse))
                {
                    _consoleService.WriteColored("AI returned an empty response.", ConsoleColor.Yellow);
                    return null;
                }
                var tempFilePath = string.Empty;
                if (_aiSettings.PreviewResult)
                {
                    // Write the generated tests to a temporary file.
                    var tempFileName = _fileSystem.Path.GetFileNameWithoutExtension(testFilePath) + "_temp" + _fileSystem.Path.GetExtension(testFilePath);
                    tempFilePath = _fileSystem.Path.Combine(_fileSystem.Path.GetTempPath(), tempFileName);
                    _fileSystem.File.WriteAllText(tempFilePath, aiResponse);
                    _consoleService.WriteColored($"Generated test file saved to temporary file: {tempFilePath}", ConsoleColor.Green);

                    _consoleService.WriteColored("Previewing the generated test file...", ConsoleColor.Blue);
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
                }

                if (!dto.PromptUser)
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
            var prompt = $@"{_aiSettings.Prompt}";

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

        private string TestFilePath(FileChangeProcessingDto dto)
        {
            var testFilePath = string.Empty;
            if (!dto.PromptUser) {
                var srcPath = $"{dto.ProjectFolder}/{dto.SrcFolder}";
                var testPath = $"{dto.ProjectFolder}/{dto.TestsFolder}";
                var relativePath = _fileSystem.Path.GetRelativePath(srcPath, dto.FilePath);
                testFilePath = _fileSystem.Path.Combine(testPath, relativePath);
                testFilePath = _fileSystem.Path.Combine(_fileSystem.Path.GetDirectoryName(testFilePath),
                _fileSystem.Path.GetFileNameWithoutExtension(testFilePath) + "Tests" + _fileSystem.Path.GetExtension(testFilePath));
            } else {
                var relativePath = _fileSystem.Path.GetRelativePath(dto.SrcFolder, dto.FilePath);
                var relativeTestPath = relativePath.Replace(dto.SrcFolder + Path.DirectorySeparatorChar, dto.TestsFolder + Path.DirectorySeparatorChar);
                var testFileName = _fileSystem.Path.GetFileNameWithoutExtension(relativeTestPath) + "Tests" + _fileSystem.Path.GetExtension(relativeTestPath);
                testFilePath = _fileSystem.Path.Combine(dto.TestsFolder, _fileSystem.Path.GetDirectoryName(relativeTestPath), testFileName);
            }

            return testFilePath;
        }
    }
}
