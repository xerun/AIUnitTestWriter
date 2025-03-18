namespace AIUnitTestWriter.SettingOptions
{
    public class GitSettings
    {
        public string LocalRepositoryPath { get; set; } = null!;
        public string GitMainBranch { get; set; } = null!;
        public string GitHubToken { get; set; } = null!;
        public string BranchPrefix { get; set; } = null!;
        public int PollInterval { get; set; } = 0;
    }
}
