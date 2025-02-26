using AIUnitTestWriter.Models;
using AIUnitTestWriter.Services.Interfaces;

namespace AIUnitTestWriter.Services
{
    public class ModeRunnerService : IModeRunner
    {
        private readonly ITestUpdater _testUpdater;
        private readonly ICodeMonitor _codeMonitor;
        private readonly IConsoleService _consoleService;

        public ModeRunnerService(ITestUpdater testUpdater, ICodeMonitor codeMonitor, IConsoleService consoleService)
        {
            _testUpdater = testUpdater;
            _codeMonitor = codeMonitor;
            _consoleService = consoleService;
        }

        /// <inheritdoc/>
        public async Task RunAutoModeAsync(ProjectConfigModel config)
        {
            _consoleService.WriteColored($"Monitoring source folder: {config.SrcFolder}", ConsoleColor.Green);
            _consoleService.WriteColored($"Tests will be updated in: {config.TestsFolder}", ConsoleColor.Green);

            _codeMonitor.Start(config.SrcFolder, config.TestsFolder, config.SampleUnitTestContent, promptUser: false);

            _consoleService.WriteColored("Auto-detect mode activated. Monitoring code changes.", ConsoleColor.Blue);
        }

        /// <inheritdoc/>
        public async Task RunManualModeAsync(ProjectConfigModel config)
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

                var result = await _testUpdater.ProcessFileChange(config.SrcFolder, config.TestsFolder, filePath, config.SampleUnitTestContent, promptUser: true);
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
