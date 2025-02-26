using AIUnitTestWriter.Services.Interfaces;

namespace AIUnitTestWriter
{
    public class AppStarter
    {
        private readonly IProjectInitializer _projectInitializer;
        private readonly IModeRunner _modeRunner;
        private readonly IGitIntegrationService _gitIntegrationService;
        private readonly IConsoleService _consoleService;

        public AppStarter(IGitIntegrationService gitIntegrationService, IProjectInitializer projectInitializer, IModeRunner modeRunner, IConsoleService consoleService)
        {
            _projectInitializer = projectInitializer;
            _modeRunner = modeRunner;
            _gitIntegrationService = gitIntegrationService;
            _consoleService = consoleService;
        }

        public async Task RunAsync()
        {
            // Initialize project settings.
            var projectConfig = _projectInitializer.Initialize();

            if (projectConfig.IsGitRepository)
            {
                // If the project comes from a Git repository, use the Git integration service.
                _consoleService.WriteColored("Git repository mode detected.", ConsoleColor.Green);
                await _gitIntegrationService.MonitorAndTriggerAsync(projectConfig);
                _consoleService.WriteColored("Git monitoring and pull request creation activated.", ConsoleColor.Blue);
            }
            else
            {
                // Ask the user for the mode.
                var mode = _consoleService.Prompt("Choose mode: Auto (A) for automatic detection of changes, Manual (M) for manual test creation:", ConsoleColor.Cyan)
                          .ToLower();

                if (mode == "a" || mode == "auto")
                {
                    await _modeRunner.RunAutoModeAsync(projectConfig);
                }
                else if (mode == "m" || mode == "manual")
                {
                    await _modeRunner.RunManualModeAsync(projectConfig);
                }
                else
                {
                    _consoleService.WriteColored("Invalid mode selected. Exiting program.", ConsoleColor.Red);
                }
            }
        }
    }
}
