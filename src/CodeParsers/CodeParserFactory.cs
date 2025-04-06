using AIUnitTestWriter.Interfaces;

namespace AIUnitTestWriter.CodeParsers
{
    public class CodeParserFactory
    {
        public static ICodeParser GetParser(string fileExtension)
        {
            return fileExtension.ToLower() switch
            {
                ".cs" => new CSharpCodeParser(),
                ".java" => new JavaCodeParser(),
                ".py" => new PythonCodeParser(),
                ".ts" => new TypeScriptCodeParser(),
                ".js" => new JavaScriptCodeParser(),
                _ => throw new NotSupportedException($"Parser for files with '{fileExtension}' extension not supported.")
            };
        }
    }
}
