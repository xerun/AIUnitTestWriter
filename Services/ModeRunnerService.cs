using AIUnitTestWriter.Extensions;
using AIUnitTestWriter.Models;
using AIUnitTestWriter.Services.Interfaces;

namespace AIUnitTestWriter.Services
{
    public class ModeRunnerService : IModeRunner
    {
        private readonly IAIApiService _aiService;
        private readonly ITestUpdater _testUpdater;
        private readonly ICodeMonitor _codeMonitor;

        public ModeRunnerService(IAIApiService aiService, ITestUpdater testUpdater, ICodeMonitor codeMonitor)
        {
            _aiService = aiService;
            _testUpdater = testUpdater;
            _codeMonitor = codeMonitor;
        }

        public async Task RunAutoModeAsync(ProjectConfigModel config)
        {
            ConsoleExtensions.WriteColored($"Monitoring source folder: {config.SrcFolder}", ConsoleColor.Green);
            ConsoleExtensions.WriteColored($"Tests will be updated in: {config.TestsFolder}", ConsoleColor.Green);

            _codeMonitor.Start(config.SrcFolder, config.TestsFolder, config.SampleUnitTestContent, promptUser: false);

            "Auto-detect mode activated. Monitoring code changes. Press Enter to exit.".WriteColored(ConsoleColor.Blue);
            Console.ReadLine();
        }

        public async Task RunManualModeAsync(ProjectConfigModel config)
        {
            while (true)
            {
                string filePath = ConsoleExtensions.Prompt("Enter the full path to the source file for which you want to generate/update tests (or type 'exit' to quit):", ConsoleColor.Yellow);
                if (filePath?.ToLower() == "exit")
                {
                    break;
                }
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    ConsoleExtensions.WriteColored("Invalid file path. Please try again.", ConsoleColor.Red);
                    continue;
                }

                var result = await _testUpdater.ProcessFileChange(config.SrcFolder, config.TestsFolder, filePath, config.SampleUnitTestContent, promptUser: true);
                if (result == null)
                {
                    continue;
                }

                ConsoleExtensions.WriteColored($"Please review the generated test code in the temporary file: {result.TempFilePath}", ConsoleColor.DarkBlue);
                string userInput = ConsoleExtensions.Prompt("Do you want to approve and update the test file? (Y/N)", ConsoleColor.Yellow);
                if (userInput.Equals("y", StringComparison.OrdinalIgnoreCase) || userInput.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    _testUpdater.FinalizeTestUpdate(result);
                    ConsoleExtensions.WriteColored($"Test file updated at: {result.TestFilePath}", ConsoleColor.Green);
                }
                else
                {
                    ConsoleExtensions.WriteColored("Changes were not applied. You may manually copy any necessary code from the temporary file.", ConsoleColor.Red);
                }
                ConsoleExtensions.WriteColored("Processing completed for this file.", ConsoleColor.Green);
            }
        }
    }
}
