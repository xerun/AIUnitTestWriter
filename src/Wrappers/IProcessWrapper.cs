namespace AIUnitTestWriter.Wrappers
{
    public interface IProcessWrapper : IDisposable
    {
        StreamReader StandardOutput { get; }
        StreamReader StandardError { get; }
        int ExitCode { get; }
        void WaitForExit();
    }
}
