namespace AIUnitTestWriter.Services.Interfaces
{
    public interface IModeRunner
    {
        /// <summary>
        /// Run the auto mode.
        /// </summary>
        /// <returns></returns>
        Task RunAutoModeAsync();

        /// <summary>
        /// Run the manual mode.
        /// </summary>
        /// <returns></returns>
        Task RunManualModeAsync();
    }
}
