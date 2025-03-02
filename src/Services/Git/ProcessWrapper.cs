using AIUnitTestWriter.Services.Interfaces;
using System.Diagnostics;

namespace AIUnitTestWriter.Services.Git
{
    public class ProcessWrapper : IProcessWrapper
    {
        private readonly Process _process;

        public ProcessWrapper(Process process)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
        }

        public StreamReader StandardOutput
        {
            get
            {
                if (_process.HasExited)
                    throw new InvalidOperationException("Cannot access StandardOutput after process has exited.");
                return _process.StandardOutput;
            }
        }
        public StreamReader StandardError => _process.StandardError;
        public int ExitCode => _process.ExitCode;

        public void WaitForExit() => _process.WaitForExit();

        public void Dispose() => _process.Dispose();
    }
}
