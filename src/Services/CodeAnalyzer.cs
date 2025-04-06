using AIUnitTestWriter.CodeParsers;
using AIUnitTestWriter.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace AIUnitTestWriter.Services
{
    public class CodeAnalyzer : ICodeAnalyzer
    {
        /// <inheritdoc/>
        public string GetMethodCode(string sourceCode, string methodName, string codeExtension)
        {
            var parser = CodeParserFactory.GetParser(codeExtension);
            var tree = parser.ParseSourceCode(sourceCode);
            var root = tree.GetCompilationUnitRoot();

            var methodNode = root.DescendantNodes()
                                 .OfType<MethodDeclarationSyntax>()
                                 .FirstOrDefault(m => m.Identifier.ValueText.Equals(methodName));
            if (methodNode != null)
            {
                // Optionally include the class declaration if needed.
                return methodNode.ToFullString();
            }
            return string.Empty;
        }

        /// <inheritdoc/>
        public string FilterTestMethods(string testCode, string methodName, string codeExtension)
        {
            var parser = CodeParserFactory.GetParser(codeExtension);
            var tree = parser.ParseSourceCode(testCode);
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
        public List<string> GetPublicMethodNames(string sourceCode, string codeExtension)
        {
            var methodNames = new List<string>();

            var parser = CodeParserFactory.GetParser(codeExtension);
            var tree = parser.ParseSourceCode(sourceCode);
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

        /// <inheritdoc/>
        public List<int> GetChangedLines(string oldContent, string newContent)
        {
            var oldLines = oldContent.Split('\n');
            var newLines = newContent.Split('\n');

            var changedLines = new List<int>();

            for (int i = 0; i < newLines.Length; i++)
            {
                if (i >= oldLines.Length || oldLines[i] != newLines[i])
                {
                    changedLines.Add(i + 1); // 1-based line numbers
                }
            }

            return changedLines;
        }

        /// <inheritdoc/>
        public List<string> GetMethodsAroundLines(string sourceCode, List<int> changedLines, string codeExtension)
        {
            var parser = CodeParserFactory.GetParser(codeExtension);
            var tree = parser.ParseSourceCode(sourceCode);

            var methods = parser.GetMethods(tree, changedLines);

            return methods.Select(m => m.ToFullString()).ToList();
        }

        /// <inheritdoc/>
        public string GetMethodWithDependencies(string sourceCode, List<string> methods, string codeExtension)
        {
            var parser = CodeParserFactory.GetParser(codeExtension);
            var tree = parser.ParseSourceCode(sourceCode);
            var root = tree.GetRoot();

            var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>();
            var classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            var fields = classNode?.DescendantNodes().OfType<FieldDeclarationSyntax>() ?? Enumerable.Empty<FieldDeclarationSyntax>();
            var constructors = classNode?.DescendantNodes().OfType<ConstructorDeclarationSyntax>() ?? Enumerable.Empty<ConstructorDeclarationSyntax>();

            var result = new StringBuilder();
            foreach (var u in usings) result.AppendLine(u.ToFullString());

            result.AppendLine();
            result.AppendLine($"// BEGIN CLASS {classNode?.Identifier.Text}");

            if (classNode != null)
            {
                result.AppendLine($"public class {classNode.Identifier.Text} {{");

                foreach (var f in fields) result.AppendLine(f.ToFullString());
                foreach (var ctor in constructors) result.AppendLine(ctor.ToFullString());

                foreach (var method in methods) result.AppendLine(method);

                result.AppendLine("// END CLASS");
                result.AppendLine("}");
            }

            return result.ToString();
        }
    }
}
