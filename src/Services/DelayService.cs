using AIUnitTestWriter.Interfaces;

namespace AIUnitTestWriter.Services
{
    public class DelayService : IDelayService
    {
        public async Task DelayAsync(int milliseconds, CancellationToken cancellationToken = default)
        {
            await Task.Delay(milliseconds, cancellationToken);
        }
    }
}
