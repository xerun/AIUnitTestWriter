using AIUnitTestWriter.Services.Interfaces;
using System.Diagnostics;

namespace AIUnitTestWriter.Services.Git
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
