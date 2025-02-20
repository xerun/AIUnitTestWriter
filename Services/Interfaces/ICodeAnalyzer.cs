namespace AIUnitTestWriter.Services.Interfaces
{
    public interface ICodeAnalyzer
    {
        /// <summary>
        /// Returns the code of the method with the given name.
        /// </summary>
        string GetMethodCode(string sourceCode, string methodName);

        /// <summary>
        /// Filters and returns test methods that start with the specified method name.
        /// For example, if the changed method is "Calculate", it returns tests like "Calculate_When_Return".
        /// </summary>
        string FilterTestMethods(string testCode, string methodName);

        /// <summary>
        /// Returns a list of public method names found in the source code.
        /// </summary>
        List<string> GetPublicMethodNames(string sourceCode);
    }
}
