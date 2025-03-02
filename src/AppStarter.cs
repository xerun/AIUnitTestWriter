using AIUnitTestWriter.Models;
using AIUnitTestWriter.Services.Interfaces;

namespace AIUnitTestWriter
{
    public class AppStarter
    {
        private readonly IModeRunner _modeRunner;
        private readonly IGitMonitorService _gitIntegrationService;
        private readonly IConsoleService _consoleService;
        private readonly ProjectConfigModel _projectConfig;

        public AppStarter(IGitMonitorService gitIntegrationService, IModeRunner modeRunner, IConsoleService consoleService, ProjectConfigModel projectConfig)
        {
            _modeRunner = modeRunner ?? throw new ArgumentNullException(nameof(modeRunner));
            _gitIntegrationService = gitIntegrationService ?? throw new ArgumentNullException(nameof(gitIntegrationService));
            _consoleService = consoleService ?? throw new ArgumentNullException(nameof(consoleService));
            _projectConfig = projectConfig ?? throw new ArgumentNullException(nameof(projectConfig));
        }

        public async Task RunAsync()
        {
            if (_projectConfig.IsGitRepository)
            {
                // If the project comes from a Git repository, use the Git integration service.
                _consoleService.WriteColored("Git repository mode detected.", ConsoleColor.Green);
                await _gitIntegrationService.MonitorAndTriggerAsync();
                _consoleService.WriteColored("Git monitoring and pull request creation activated, press any key to exit.", ConsoleColor.Blue);
                _consoleService.ReadLine();
            }
            else
            {
                // Ask the user for the mode.
                var mode = _consoleService.Prompt("Choose mode: Auto (A) for automatic detection of changes, Manual (M) for manual test creation:", ConsoleColor.Cyan)
                          .ToLower();

                if (mode == "a" || mode == "auto")
                {
                    await _modeRunner.RunAutoModeAsync();
                }
                else if (mode == "m" || mode == "manual")
                {
                    await _modeRunner.RunManualModeAsync();
                }
                else
                {
                    _consoleService.WriteColored("Invalid mode selected. Exiting program.", ConsoleColor.Red);
                }
            }
        }
    }
}
