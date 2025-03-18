namespace AIUnitTestWriter.Interfaces
{
    public interface IAIApiService
    {
        /// <summary>
        /// Generates tests based on the given prompt.
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> GenerateTestsAsync(string prompt, CancellationToken cancellationToken = default);
    }
}
