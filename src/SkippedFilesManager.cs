namespace AIUnitTestWriter
{
    public static class SkippedFilesManager
    {
        public static readonly HashSet<string> SkippedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Program.cs",
            "Startup.cs",
            "AssemblyInfo.cs",
            "GlobalUsings.cs",
            "Usings.cs"
        };

        public static bool ShouldSkip(string filePath)
        {
            return SkippedFiles.Contains(Path.GetFileName(filePath));
        }
    }
}
