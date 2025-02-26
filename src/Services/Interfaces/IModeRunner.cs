using AIUnitTestWriter.Models;

namespace AIUnitTestWriter.Services.Interfaces
{
    public interface IModeRunner
    {
        /// <summary>
        /// Run the auto mode.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        Task RunAutoModeAsync(ProjectConfigModel config);

        /// <summary>
        /// Run the manual mode.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        Task RunManualModeAsync(ProjectConfigModel config);
    }
}
