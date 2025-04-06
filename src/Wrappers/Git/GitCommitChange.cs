namespace AIUnitTestWriter.Wrappers.Git
{
    public class GitCommitChange
    {
        public string FilePath { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int Additions { get; set; }
        public int Deletions { get; set; }
    }
}
