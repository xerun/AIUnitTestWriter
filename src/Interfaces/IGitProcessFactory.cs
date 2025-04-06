using System.Diagnostics;

namespace AIUnitTestWriter.Interfaces
{
    public interface IGitProcessFactory
    {
        IProcessWrapper StartProcess(ProcessStartInfo processStartInfo);
    }
}
