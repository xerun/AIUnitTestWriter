using AIUnitTestWriter.Models;
using AIUnitTestWriter.SettingOptions;
using AIUnitTestWriter.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace AIUnitTestWriter.Services
{
    public class ProjectInitializerService : IProjectInitializer
    {        
        private readonly IConsoleService _consoleService;
        private readonly ProjectSettings _projectSettings;
        private readonly GitSettings _gitSettings;

        public ProjectInitializerService(IOptions<ProjectSettings> projectSettings, IOptions<GitSettings> gitSettings, IConsoleService consoleService)
        {
            _projectSettings = projectSettings?.Value ?? throw new ArgumentNullException(nameof(projectSettings));
            _gitSettings = gitSettings?.Value ?? throw new ArgumentNullException(nameof(gitSettings));
            _consoleService = consoleService ?? throw new ArgumentNullException(nameof(consoleService));
        }

        public ProjectConfigModel Initialize()
        {
            var config = new ProjectConfigModel();

            config.ProjectPath = _consoleService.Prompt(
               "Enter the full path to your .NET project or a Git repository URL:",
               ConsoleColor.Cyan);

            // Determine if input is a local path or Git URL.
            if (Directory.Exists(config.ProjectPath))
            {
                // Local project path.
                config.IsGitRepository = false;
            }
            else if (config.ProjectPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                     config.ProjectPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // It's a Git repository URL.
                config.IsGitRepository = true;
                config.GitRepositoryUrl = config.ProjectPath;
                config.ProjectPath = _gitSettings.LocalRepositoryPath;

                // If the directory does not exist, create it.
                if (!Directory.Exists(_gitSettings.LocalRepositoryPath))
                {
                    Directory.CreateDirectory(_gitSettings.LocalRepositoryPath);
                }
            }
            else
            {
                _consoleService.WriteColored("Invalid input. Please enter a valid local path or a Git repository URL.", ConsoleColor.Red);
                return Initialize(); // Re-prompt if input is invalid.
            }

            var srcFolderName = _projectSettings.SourceFolder ?? throw new ArgumentException(nameof(_projectSettings.SourceFolder));
            var testsFolderName = _projectSettings.TestsFolder ?? throw new ArgumentException(nameof(_projectSettings.TestsFolder));
            config.SrcFolder = Path.Combine(config.ProjectPath, srcFolderName);
            config.TestsFolder = Path.Combine(config.ProjectPath, testsFolderName);

            string sampleResponse = _consoleService.Prompt("Would you like to provide a sample unit test file for reference? (Y/N)", ConsoleColor.DarkYellow);
            int retryCount = 0;
            int maxRetries = 3; // Limit retries for testability
            if (sampleResponse.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                sampleResponse.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                string sampleFilePath = _consoleService.Prompt("Please enter the full path to the sample unit test file:", ConsoleColor.Yellow);
                while ((string.IsNullOrWhiteSpace(sampleFilePath) || !File.Exists(sampleFilePath)) && retryCount < maxRetries)
                {
                    _consoleService.WriteColored("Invalid file path. Please enter a valid sample unit test file path:", ConsoleColor.Red);
                    sampleFilePath = _consoleService.Prompt("Please enter the full path to the sample unit test file:", ConsoleColor.Yellow);
                    retryCount++;
                }
                
                if (retryCount == maxRetries && string.IsNullOrWhiteSpace(sampleFilePath))
                {
                    _consoleService.WriteColored("Maximum retry limit reached. Skipping sample file selection.", ConsoleColor.Red);
                    config.SampleUnitTestContent = string.Empty;
                }
                else
                {
                    config.SampleUnitTestContent = File.ReadAllText(sampleFilePath);
                }
            }
            else
            {
                config.SampleUnitTestContent = string.Empty;
            }

            return config;
        }
    }
}
