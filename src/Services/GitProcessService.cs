using AIUnitTestWriter.Services.Interfaces;
using System.Diagnostics;

namespace AIUnitTestWriter.Services
{
    public class GitProcessService : IGitProcessService
    {
        public string RunCommand(string command, string workingDirectory)
        {
            var processInfo = new ProcessStartInfo("git", command)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            return !string.IsNullOrWhiteSpace(error) ? error : output;
        }
    }
}
