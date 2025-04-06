using Microsoft.CodeAnalysis;

namespace AIUnitTestWriter.Interfaces
{
    public interface ICodeParser
    {
        SyntaxTree ParseSourceCode(string sourceCode);
        IEnumerable<SyntaxNode> GetMethods(SyntaxTree tree, List<int> changedLines);
    }
}
