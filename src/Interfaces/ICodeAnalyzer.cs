namespace AIUnitTestWriter.Interfaces
{
    public interface ICodeAnalyzer
    {
        /// <summary>
        /// Returns the code of the method with the given name.
        /// </summary>
        string GetMethodCode(string sourceCode, string methodName, string codeExtension);

        /// <summary>
        /// Filters and returns test methods that start with the specified method name.
        /// For example, if the changed method is "Calculate", it returns tests like "Calculate_When_Return".
        /// </summary>
        string FilterTestMethods(string testCode, string methodName, string codeExtension);

        /// <summary>
        /// Returns a list of public method names found in the source code.
        /// </summary>
        List<string> GetPublicMethodNames(string sourceCode, string codeExtension);

        /// <summary>
        /// Returns a list of line numbers that have changed between the old and new content.
        /// </summary>
        /// <param name="oldContent"></param>
        /// <param name="newContent"></param>
        /// <returns></returns>
        List<int> GetChangedLines(string oldContent, string newContent);

        /// <summary>
        /// Returns the code of the methods around the changed lines.
        /// </summary>
        /// <param name="sourceCode"></param>
        /// <param name="changedLines"></param>
        /// <returns></returns>
        List<string> GetMethodsAroundLines(string sourceCode, List<int> changedLines, string codeExtension);

        /// <summary>
        /// Returns the code of the method with the given name and its dependencies.
        /// </summary>
        /// <param name="sourceCode"></param>
        /// <param name="methods"></param>
        /// <returns></returns>
        string GetMethodWithDependencies(string sourceCode, List<string> methods, string codeExtension);
    }
}
