namespace AIUnitTestWriter.Services.Interfaces
{
    public interface IAIApiService
    {
        Task<string> GenerateTestsAsync(string prompt);
    }
}
