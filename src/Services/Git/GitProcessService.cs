using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.Wrappers;
using System.Diagnostics;

namespace AIUnitTestWriter.Services.Git
{
    public class GitProcessService : IGitProcessService
    {
        private readonly IGitProcessFactory _processFactory;

        public GitProcessService(IGitProcessFactory processFactory)
        {
            _processFactory = processFactory ?? throw new ArgumentNullException(nameof(processFactory));
        }

        public string? RunCommand(string command, string workingDirectory)
        {
            var processInfo = new ProcessStartInfo("git", command)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = _processFactory.StartProcess(processInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Process failed to start.");
                }

                var output = process?.StandardOutput.ReadToEnd();
                var error = process?.StandardError.ReadToEnd();
                process?.WaitForExit();

                if (process?.ExitCode != 0)
                {
                    throw new Exception($"Git Error: {error}");
                }
                return output;
            }
        }
    }
}
