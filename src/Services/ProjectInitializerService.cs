using AIUnitTestWriter.Models;
using AIUnitTestWriter.SettingOptions;
using Microsoft.Extensions.Options;
using AIUnitTestWriter.Interfaces;

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

            // Check if Git URL is provided in settings
            if (!string.IsNullOrWhiteSpace(_gitSettings.RemoteRepositoryUrl))
            {
                config.IsBackgroundService = true;
                SetupGitRepository(config, _gitSettings.RemoteRepositoryUrl);
            }
            else
            {
                SetupUserInput(config);
            }

            SetupFolders(config);
            LoadSampleUnitTest(config);

            return config;
        }

        // Setup Git repository from app settings
        private void SetupGitRepository(ProjectConfigModel config, string projectPath)
        {
            config.IsGitRepository = true;
            config.GitRepositoryUrl = projectPath;
            config.ProjectPath = _gitSettings.LocalRepositoryPath;

            if (!Directory.Exists(_gitSettings.LocalRepositoryPath))
            {
                Directory.CreateDirectory(_gitSettings.LocalRepositoryPath);
            }
        }

        // Setup project from user input
        private void SetupUserInput(ProjectConfigModel config)
        {
            config.ProjectPath = _consoleService.Prompt(
                "Enter the full path to your .NET project or a Git repository URL:",
                ConsoleColor.Cyan);

            if (Directory.Exists(config.ProjectPath))
            {
                config.IsGitRepository = false;
            }
            else if (config.ProjectPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                     config.ProjectPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                SetupGitRepository(config, config.ProjectPath);
            }
            else
            {
                _consoleService.WriteColored(
                    "Invalid input. Please enter a valid local path or a Git repository URL.",
                    ConsoleColor.Red);

                // Re-prompt if input is invalid
                SetupUserInput(config);
            }
        }

        // Setup source and test folders
        private void SetupFolders(ProjectConfigModel config)
        {
            var srcFolderName = _projectSettings.SourceFolder
                                ?? throw new ArgumentException(nameof(_projectSettings.SourceFolder));
            var testsFolderName = _projectSettings.TestsFolder
                                  ?? throw new ArgumentException(nameof(_projectSettings.TestsFolder));

            config.SrcFolder = Path.Combine(config.ProjectPath, srcFolderName);
            config.TestsFolder = Path.Combine(config.ProjectPath, testsFolderName);
        }

        // Load sample unit test (with retry)
        private void LoadSampleUnitTest(ProjectConfigModel config)
        {
            if (!string.IsNullOrWhiteSpace(_projectSettings.SampleUnitTest))
            {
                config.SampleUnitTestContent = File.ReadAllText(_projectSettings.SampleUnitTest);
                return;
            }
            else if (!config.IsBackgroundService)
            {
                string sampleResponse = _consoleService.Prompt(
                    "Would you like to provide a sample unit test file for reference? (Y/N)",
                    ConsoleColor.DarkYellow);

                if (!sampleResponse.Equals("y", StringComparison.OrdinalIgnoreCase) &&
                    !sampleResponse.Equals("yes", StringComparison.OrdinalIgnoreCase))
                {
                    config.SampleUnitTestContent = string.Empty;
                    return;
                }

                int retryCount = 0;
                int maxRetries = 3;

                while (retryCount < maxRetries)
                {
                    string sampleFilePath = _consoleService.Prompt(
                        "Please enter the full path to the sample unit test file:",
                        ConsoleColor.Yellow);

                    if (!string.IsNullOrWhiteSpace(sampleFilePath) && File.Exists(sampleFilePath))
                    {
                        config.SampleUnitTestContent = File.ReadAllText(sampleFilePath);
                        return;
                    }

                    _consoleService.WriteColored("Invalid file path. Please enter a valid sample unit test file path.", ConsoleColor.Red);
                    retryCount++;
                }

                _consoleService.WriteColored("Maximum retry limit reached. Skipping sample file selection.", ConsoleColor.Red);
                config.SampleUnitTestContent = string.Empty;
            }
        }
    }
}
