namespace AIUnitTestWriter.Interfaces
{
    public interface ISkippedFilesManager
    {
        /// <summary>
        /// Checks if the file should be skipped.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        bool ShouldSkip(string filePath);
    }
}
