using AIUnitTestWriter.DTOs;
using AIUnitTestWriter.Models;

namespace AIUnitTestWriter.Interfaces
{
    public interface ITestUpdaterService
    {
        /// <summary>
        /// Processes the changed file. If the file is very long, it prompts for the changed method,
        /// then only that method (and its dependencies, if desired) is fed to the AI.
        /// In manual mode, returns a TestGenerationResultModel for approval.
        /// In auto mode, finalizes immediately and returns null.
        /// </summary>
        Task<TestGenerationResultModel?> ProcessFileChangeAsync(FileChangeProcessingDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Finalizes the test update by writing the generated code to the test file.
        /// </summary>
        void FinalizeTestUpdate(TestGenerationResultModel result);
    }
}
