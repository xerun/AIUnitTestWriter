using AIUnitTestWriter.Interfaces;
using Microsoft.CodeAnalysis;

namespace AIUnitTestWriter.CodeParsers
{
    public class TypeScriptCodeParser : ICodeParser
    {
        public SyntaxTree ParseSourceCode(string sourceCode)
        {
            throw new NotImplementedException("TypeScript parsing is not implemented yet.");
        }

        public IEnumerable<SyntaxNode> GetMethods(SyntaxTree tree, List<int> changedLines)
        {
            throw new NotImplementedException("TypeScript parsing is not implemented yet.");
        }
    }
}
