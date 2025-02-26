namespace AIUnitTestWriter.Models
{
    public class TestGenerationResultModel
    {
        public string TempFilePath { get; set; } = null!;
        public string TestFilePath { get; set; } = null!;
        public string GeneratedTestCode { get; set; } = null!;
    }
}
