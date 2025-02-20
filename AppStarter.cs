using AIUnitTestWriter.Extensions;
using AIUnitTestWriter.Services.Interfaces;

namespace AIUnitTestWriter
{
    public class AppStarter
    {
        private readonly IProjectInitializer _projectInitializer;
        private readonly IModeRunner _modeRunner;

        public AppStarter(IProjectInitializer projectInitializer, IModeRunner modeRunner)
        {
            _projectInitializer = projectInitializer;
            _modeRunner = modeRunner;
        }

        public async Task RunAsync()
        {
            // Initialize project settings.
            var projectConfig = _projectInitializer.Initialize();

            // Ask the user for the mode.
            string mode = ConsoleExtensions.Prompt("Choose mode: Auto (A) for automatic detection of changes, Manual (M) for manual test creation:", ConsoleColor.Cyan)
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
                "Invalid mode selected. Exiting program.".WriteColored(ConsoleColor.Red);
            }
        }
    }
}
