using AIUnitTestWriter.Interfaces;
using Microsoft.CodeAnalysis;

namespace AIUnitTestWriter.CodeParsers
{
    public class PythonCodeParser : ICodeParser
    {
        public SyntaxTree ParseSourceCode(string sourceCode)
        {
            throw new NotImplementedException("Python parsing is not implemented yet.");
        }

        public IEnumerable<SyntaxNode> GetMethods(SyntaxTree tree, List<int> changedLines)
        {
            throw new NotImplementedException("Python parsing is not implemented yet.");
        }
    }
}
