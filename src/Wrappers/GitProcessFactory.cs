using AIUnitTestWriter.Services.Git;
using System.Diagnostics;

namespace AIUnitTestWriter.Wrappers
{
    public class GitProcessFactory : IGitProcessFactory
    {
        public IProcessWrapper StartProcess(ProcessStartInfo processStartInfo)
        {
            var process = new Process { StartInfo = processStartInfo };
            if (!process.Start())
            {
                throw new InvalidOperationException("Process did not start correctly.");
            }
            return new ProcessWrapper(process);
        }
    }
}
