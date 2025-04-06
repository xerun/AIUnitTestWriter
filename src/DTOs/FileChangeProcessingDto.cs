namespace AIUnitTestWriter.DTOs
{
    public class FileChangeProcessingDto
    {
        public string FilePath { get; set; } = null!;
        public string OldContent { get; set; } = string.Empty;
        public string NewContent { get; set; } = string.Empty;
        public string CodeExtension { get; set; } = null!;
        public string SampleUnitTest { get; set; } = string.Empty;
        public string ExistingUnitTest { get; set; } = string.Empty;
        public bool PromptUser { get; set; }
        public string ProjectFolder { get; set; } = null!;
        public string SrcFolder { get; set; } = null!;
        public string TestsFolder { get; set; } = null!;

        public FileChangeProcessingDto(string filePath, string oldContent, string newContent, string codeExtension,
        string sampleUnitTest = "", string existingUnitTest = "", bool promptUser = true, string projectFolder = "", string srcFolder = "", string testsFolder = "")
        {
            FilePath = filePath;
            OldContent = oldContent;
            NewContent = newContent;
            CodeExtension = codeExtension;
            SampleUnitTest = sampleUnitTest;
            ExistingUnitTest = existingUnitTest;
            PromptUser = promptUser;
            ProjectFolder = projectFolder;
            SrcFolder = srcFolder;
            TestsFolder = testsFolder;
        }
    }
}
