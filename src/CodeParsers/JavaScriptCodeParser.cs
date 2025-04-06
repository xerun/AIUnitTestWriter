using AIUnitTestWriter.Interfaces;
using Microsoft.CodeAnalysis;

namespace AIUnitTestWriter.CodeParsers
{
    public class JavaScriptCodeParser : ICodeParser
    {
        public SyntaxTree ParseSourceCode(string sourceCode)
        {
            throw new NotImplementedException("JavaScript parsing is not implemented yet.");
        }

        public IEnumerable<SyntaxNode> GetMethods(SyntaxTree tree, List<int> changedLines)
        {
            throw new NotImplementedException("JavaScript parsing is not implemented yet.");
        }
    }
}
