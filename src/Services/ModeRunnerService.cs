using AIUnitTestWriter.Models;
using AIUnitTestWriter.Services.Interfaces;

namespace AIUnitTestWriter.Services
{
    public class ModeRunnerService : IModeRunner
    {
        private readonly ITestUpdaterService _testUpdater;
        private readonly ICodeMonitor _codeMonitor;
        private readonly IConsoleService _consoleService;
        private readonly ProjectConfigModel _projectConfig;

        public ModeRunnerService(ITestUpdaterService testUpdater, ICodeMonitor codeMonitor, IConsoleService consoleService, ProjectConfigModel projectConfig)
        {
            _testUpdater = testUpdater ?? throw new ArgumentNullException(nameof(testUpdater));
            _codeMonitor = codeMonitor ?? throw new ArgumentNullException(nameof(codeMonitor));
            _consoleService = consoleService ?? throw new ArgumentNullException(nameof(consoleService));
            _projectConfig = projectConfig ?? throw new ArgumentNullException(nameof(projectConfig));
        }

        /// <inheritdoc/>
        public async Task RunAutoModeAsync()
        {
            _consoleService.WriteColored($"Monitoring source folder: {_projectConfig.SrcFolder}", ConsoleColor.Green);
            _consoleService.WriteColored($"Tests will be updated in: {_projectConfig.TestsFolder}", ConsoleColor.Green);

            _codeMonitor.Start(_projectConfig.SrcFolder, _projectConfig.TestsFolder, _projectConfig.SampleUnitTestContent, promptUser: false);

            _consoleService.WriteColored("Auto-detect mode activated. Monitoring code changes, press any key to exit.", ConsoleColor.Blue);
            _consoleService.ReadLine();
        }

        /// <inheritdoc/>
        public async Task RunManualModeAsync()
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

                var result = await _testUpdater.ProcessFileChange(_projectConfig.SrcFolder, _projectConfig.TestsFolder, filePath, _projectConfig.SampleUnitTestContent, promptUser: true);
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
