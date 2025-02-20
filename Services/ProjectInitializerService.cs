using AIUnitTestWriter.Extensions;
using AIUnitTestWriter.Models;
using AIUnitTestWriter.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AIUnitTestWriter.Services
{
    public class ProjectInitializerService : IProjectInitializer
    {
        private readonly IConfiguration _configuration;

        public ProjectInitializerService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ProjectConfigModel Initialize()
        {
            var config = new ProjectConfigModel();

            config.ProjectPath = ConsoleExtensions.Prompt("Enter the full path to your .NET project:", ConsoleColor.Cyan);
            while (string.IsNullOrWhiteSpace(config.ProjectPath) || !Directory.Exists(config.ProjectPath))
            {
                ConsoleExtensions.WriteColored("Invalid path. Please enter a valid project path:", ConsoleColor.Red);
                config.ProjectPath = ConsoleExtensions.Prompt("Enter the full path to your .NET project:", ConsoleColor.Cyan);
            }

            var srcFolderName = _configuration["Project:SourceFolder"] ?? "src";
            var testsFolderName = _configuration["Project:TestsFolder"] ?? "tests";
            config.SrcFolder = Path.Combine(config.ProjectPath, srcFolderName);
            config.TestsFolder = Path.Combine(config.ProjectPath, testsFolderName);

            string sampleResponse = ConsoleExtensions.Prompt("Would you like to provide a sample unit test file for reference? (Y/N)", ConsoleColor.DarkYellow);
            if (sampleResponse.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                sampleResponse.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                string sampleFilePath = ConsoleExtensions.Prompt("Please enter the full path to the sample unit test file:", ConsoleColor.Yellow);
                while (string.IsNullOrWhiteSpace(sampleFilePath) || !File.Exists(sampleFilePath))
                {
                    ConsoleExtensions.WriteColored("Invalid file path. Please enter a valid sample unit test file path:", ConsoleColor.Red);
                    sampleFilePath = ConsoleExtensions.Prompt("Please enter the full path to the sample unit test file:", ConsoleColor.Yellow);
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
