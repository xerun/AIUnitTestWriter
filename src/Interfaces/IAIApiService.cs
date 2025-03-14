namespace AIUnitTestWriter.Interfaces
{
    public interface IAIApiService
    {
        Task<string> GenerateTestsAsync(string prompt);
    }
}
