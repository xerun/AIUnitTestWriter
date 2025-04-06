using AIUnitTestWriter.DTOs;
using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.Models;
using AIUnitTestWriter.SettingOptions;
using Microsoft.Extensions.Options;

namespace AIUnitTestWriter.Services
{
    public class ModeRunnerService : IModeRunner
    {
        private readonly ITestUpdaterService _testUpdater;
        private readonly ICodeMonitor _codeMonitor;
        private readonly IConsoleService _consoleService;
        private readonly ProjectConfigModel _projectConfig;
        private readonly ProjectSettings _projectSettings;

        public ModeRunnerService(ITestUpdaterService testUpdater, ICodeMonitor codeMonitor, IConsoleService consoleService, ProjectConfigModel projectConfig, IOptions<ProjectSettings> projectSettings)
        {
            _testUpdater = testUpdater ?? throw new ArgumentNullException(nameof(testUpdater));
            _codeMonitor = codeMonitor ?? throw new ArgumentNullException(nameof(codeMonitor));
            _consoleService = consoleService ?? throw new ArgumentNullException(nameof(consoleService));
            _projectConfig = projectConfig ?? throw new ArgumentNullException(nameof(projectConfig));
            _projectSettings = projectSettings?.Value ?? throw new ArgumentNullException(nameof(projectSettings));
        }

        /// <inheritdoc/>
        public async Task RunAutoModeAsync(CancellationToken cancellationToken = default)
        {
            _consoleService.WriteColored($"Monitoring source folder: {_projectConfig.SrcFolder}", ConsoleColor.Green);
            _consoleService.WriteColored($"Tests will be updated in: {_projectConfig.TestsFolder}", ConsoleColor.Green);

            await _codeMonitor.StartAsync(_projectConfig.FilePath, _projectConfig.SrcFolder, _projectConfig.TestsFolder, _projectConfig.SampleUnitTestContent, promptUser: false, cancellationToken);

            _consoleService.WriteColored("Auto-detect mode activated. Monitoring code changes, press any key to exit.", ConsoleColor.Blue);
            _consoleService.ReadLine();
        }

        /// <inheritdoc/>
        public async Task RunManualModeAsync(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                string filePath = _consoleService.Prompt("Enter the full path to the source file for which you want to generate/update tests (or type 'exit' to quit):", ConsoleColor.Yellow);
                if (filePath?.ToLower() == "exit")
                {
                    break;
                }
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    _consoleService.WriteColored("Invalid file path. Please try again.", ConsoleColor.Red);
                    continue;
                }

                var fileChangeProcessingDto = new FileChangeProcessingDto(
                    filePath,
                    oldContent: "",
                    newContent: "",
                    codeExtension: _projectSettings.CodeFileExtension,
                    sampleUnitTest: _projectConfig.SampleUnitTestContent,
                    promptUser: true,
                    projectFolder: _projectConfig.FilePath,
                    srcFolder: _projectConfig.SrcFolder,
                    testsFolder: _projectConfig.TestsFolder
                );

                var result = await _testUpdater.ProcessFileChangeAsync(fileChangeProcessingDto, cancellationToken);
                if (result == null)
                {
                    continue;
                }

                _consoleService.WriteColored($"Please review the generated test code in the temporary file: {result.TempFilePath}", ConsoleColor.DarkBlue);
                string userInput = _consoleService.Prompt("Do you want to approve and update the test file? (Y/N)", ConsoleColor.Yellow);
                if (userInput.Equals("y", StringComparison.OrdinalIgnoreCase) || userInput.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    _testUpdater.FinalizeTestUpdate(result);
                    _consoleService.WriteColored($"Test file updated at: {result.TestFilePath}", ConsoleColor.Green);
                }
                else
                {
                    _consoleService.WriteColored("Changes were not applied. You may manually copy any necessary code from the temporary file.", ConsoleColor.Red);
                }
                _consoleService.WriteColored("Processing completed for this file.", ConsoleColor.Green);
            }
        }
    }
}
