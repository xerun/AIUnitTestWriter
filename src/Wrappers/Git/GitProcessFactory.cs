using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.Services.Git;
using System.Diagnostics;

namespace AIUnitTestWriter.Wrappers.Git
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
