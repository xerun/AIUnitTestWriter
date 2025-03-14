using AIUnitTestWriter.Interfaces;

namespace AIUnitTestWriter.Services
{
    public class DelayService : IDelayService
    {
        public async Task DelayAsync(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }
    }
}
