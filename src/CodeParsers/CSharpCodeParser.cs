using AIUnitTestWriter.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AIUnitTestWriter.CodeParsers
{
    public class CSharpCodeParser : ICodeParser
    {
        public SyntaxTree ParseSourceCode(string sourceCode)
        {
            return CSharpSyntaxTree.ParseText(sourceCode);
        }

        public IEnumerable<SyntaxNode> GetMethods(SyntaxTree tree, List<int> changedLines)
        {
            var root = tree.GetRoot();

            return root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m =>
                {
                    var span = m.GetLocation().GetLineSpan();
                    var start = span.StartLinePosition.Line + 1;
                    var end = span.EndLinePosition.Line + 1;

                    return changedLines.Any(line => line >= start && line <= end);
                });
        }
    }
}
