using AIUnitTestWriter.Models;
using AIUnitTestWriter.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AIUnitTestWriter.Services
{
    public class ProjectInitializerService : IProjectInitializer
    {
        private readonly IConfiguration _configuration;
        private readonly IConsoleService _consoleService;

        public ProjectInitializerService(IConfiguration configuration, IConsoleService consoleService)
        {
            _configuration = configuration;
            _consoleService = consoleService;
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

                // Ask for a local path where the repository should be cloned.
                config.ProjectPath = _consoleService.Prompt(
                    "Enter the local path where the repository should be cloned:",
                    ConsoleColor.Cyan);

                // If the directory does not exist, create it.
                if (!Directory.Exists(config.ProjectPath))
                {
                    Directory.CreateDirectory(config.ProjectPath);
                }
            }
            else
            {
                _consoleService.WriteColored("Invalid input. Please enter a valid local path or a Git repository URL.", ConsoleColor.Red);
                return Initialize(); // Re-prompt if input is invalid.
            }

            var srcFolderName = _configuration["Project:SourceFolder"] ?? "src";
            var testsFolderName = _configuration["Project:TestsFolder"] ?? "tests";
            config.SrcFolder = Path.Combine(config.ProjectPath, srcFolderName);
            config.TestsFolder = Path.Combine(config.ProjectPath, testsFolderName);

            string sampleResponse = _consoleService.Prompt("Would you like to provide a sample unit test file for reference? (Y/N)", ConsoleColor.DarkYellow);
            if (sampleResponse.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                sampleResponse.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                string sampleFilePath = _consoleService.Prompt("Please enter the full path to the sample unit test file:", ConsoleColor.Yellow);
                while (string.IsNullOrWhiteSpace(sampleFilePath) || !File.Exists(sampleFilePath))
                {
                    _consoleService.WriteColored("Invalid file path. Please enter a valid sample unit test file path:", ConsoleColor.Red);
                    sampleFilePath = _consoleService.Prompt("Please enter the full path to the sample unit test file:", ConsoleColor.Yellow);
                }
                config.SampleUnitTestContent = File.ReadAllText(sampleFilePath);
            }
            else
            {
                config.SampleUnitTestContent = string.Empty;
            }

            return config;
        }
    }
}
