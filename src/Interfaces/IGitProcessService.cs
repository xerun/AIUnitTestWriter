namespace AIUnitTestWriter.Interfaces
{
    public interface IGitProcessService
    {
        /// <summary>
        /// Run a command in the specified working directory.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="workingDirectory"></param>
        /// <returns></returns>
        string? RunCommand(string command, string workingDirectory);
    }
}
