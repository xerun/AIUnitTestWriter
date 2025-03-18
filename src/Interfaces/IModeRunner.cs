namespace AIUnitTestWriter.Interfaces
{
    public interface IModeRunner
    {
        /// <summary>
        /// Run the auto mode.
        /// </summary>
        /// <returns></returns>
        Task RunAutoModeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Run the manual mode.
        /// </summary>
        /// <returns></returns>
        Task RunManualModeAsync(CancellationToken cancellationToken = default);
    }
}
