namespace AIUnitTestWriter.Models
{
    public class ProjectConfigModel
    {
        public string ProjectPath { get; set; } = null!;
        public string FilePath { get; set; } = string.Empty;
        public string SrcFolder { get; set; } = null!;
        public string TestsFolder { get; set; } = null!;
        public string SampleUnitTestContent { get; set; } = null!;
        public bool IsGitRepository { get; set; }
        public string GitRepositoryUrl { get; set; } = null!;
        public bool IsBackgroundService { get; set; }
    }
}
