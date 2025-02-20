using AIUnitTestWriter.Models;

namespace AIUnitTestWriter.Services.Interfaces
{
    public interface IModeRunner
    {
        Task RunAutoModeAsync(ProjectConfigModel config);
        Task RunManualModeAsync(ProjectConfigModel config);
    }
}
