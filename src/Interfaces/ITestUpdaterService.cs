using AIUnitTestWriter.DTOs;
using AIUnitTestWriter.Models;

namespace AIUnitTestWriter.Interfaces
{
    public interface ITestUpdaterService
    {
        /// <summary>
        /// Processes the changed file and returns a TestGenerationResultModel in manual mode.
        /// In auto mode, finalizes the update immediately and returns null.
        /// </summary>
        Task<TestGenerationResultModel?> ProcessFileChangeAsync(FileChangeProcessingDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finalizes the test update by writing the generated code to the test file.
        /// </summary>
        void FinalizeTestUpdate(TestGenerationResultModel result);
    }
}
