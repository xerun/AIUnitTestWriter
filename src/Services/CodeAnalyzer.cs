using AIUnitTestWriter.Services.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace AIUnitTestWriter.Services
{
    public class CodeAnalyzer : ICodeAnalyzer
    {
        /// <inheritdoc/>
        public string GetMethodCode(string sourceCode, string methodName)
        {
            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetCompilationUnitRoot();

            var methodNode = root.DescendantNodes()
                                 .OfType<MethodDeclarationSyntax>()
                                 .FirstOrDefault(m => m.Identifier.ValueText.Equals(methodName));
            if (methodNode != null)
            {
                // Optionally include the class declaration if needed.
                return methodNode.ToFullString();
            }
            return null;
        }

        /// <inheritdoc/>
        public string FilterTestMethods(string testCode, string methodName)
        {
            var tree = CSharpSyntaxTree.ParseText(testCode);
            var root = tree.GetCompilationUnitRoot();

            var testMethods = root.DescendantNodes()
                                  .OfType<MethodDeclarationSyntax>()
                                  .Where(m => m.Identifier.ValueText.StartsWith(methodName));

            var sb = new StringBuilder();
            foreach (var m in testMethods)
            {
                sb.AppendLine(m.ToFullString());
            }
            return sb.ToString();
        }

        /// <inheritdoc/>
        public List<string> GetPublicMethodNames(string sourceCode)
        {
            var methodNames = new List<string>();

            var tree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = tree.GetCompilationUnitRoot();

            // Look for class declarations in the file.
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classDecl in classDeclarations)
            {
                // Extract public methods only.
                var methods = classDecl.Members.OfType<MethodDeclarationSyntax>()
                    .Where(m => m.Modifiers.Any(SyntaxKind.PublicKeyword));

                foreach (var method in methods)
                {
                    methodNames.Add(method.Identifier.ValueText);
                }
            }
            return methodNames;
        }
    }
}
