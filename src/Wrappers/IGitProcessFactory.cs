using System.Diagnostics;

namespace AIUnitTestWriter.Wrappers
{
    public interface IGitProcessFactory
    {
        IProcessWrapper StartProcess(ProcessStartInfo processStartInfo);
    }
}
