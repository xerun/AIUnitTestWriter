using AIUnitTestWriter.Interfaces;
using Microsoft.CodeAnalysis;

namespace AIUnitTestWriter.CodeParsers
{
    public class JavaCodeParser : ICodeParser
    {
        public SyntaxTree ParseSourceCode(string sourceCode)
        {
            throw new NotImplementedException("Java parsing is not implemented yet.");
        }

        public IEnumerable<SyntaxNode> GetMethods(SyntaxTree tree, List<int> changedLines)
        {
            throw new NotImplementedException("Java parsing is not implemented yet.");
        }
    }
}
