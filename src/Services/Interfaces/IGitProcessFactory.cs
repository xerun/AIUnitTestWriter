using System.Diagnostics;

namespace AIUnitTestWriter.Services.Interfaces
{
    public interface IGitProcessFactory
    {
        IProcessWrapper StartProcess(ProcessStartInfo processStartInfo);
    }
}
