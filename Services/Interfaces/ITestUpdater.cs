using AIUnitTestWriter.Models;

namespace AIUnitTestWriter.Services.Interfaces
{
    public interface ITestUpdater
    {
        /// <summary>
        /// Processes the changed file and returns a TestGenerationResultModel in manual mode.
        /// In auto mode, finalizes the update immediately and returns null.
        /// </summary>
        Task<TestGenerationResultModel?> ProcessFileChange(string srcFolder, string testsFolder, string filePath, string sampleUnitTest = "", bool promptUser = true);

        /// <summary>
        /// Finalizes the test update by writing the generated code to the test file.
        /// </summary>
        void FinalizeTestUpdate(TestGenerationResultModel result);
    }
}
