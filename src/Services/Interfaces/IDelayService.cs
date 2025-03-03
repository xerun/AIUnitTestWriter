namespace AIUnitTestWriter.Services.Interfaces
{
    public interface IDelayService
    {
        Task DelayAsync(int milliseconds);
    }
}
