namespace AIUnitTestWriter.Models
{
    public record GitFileChange(
        string FilePath,
        string OldContent,
        string NewContent
    );
}
