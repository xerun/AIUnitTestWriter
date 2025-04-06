using AIUnitTestWriter.CodeParsers;
using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.Services;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Moq;
using Microsoft.CodeAnalysis;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class CodeAnalyzerTests
    {
        private readonly CodeAnalyzer _codeAnalyzer;

        public CodeAnalyzerTests()
        {
            _codeAnalyzer = new CodeAnalyzer();
        }

        [Fact]
        public void GetMethodCode_ShouldReturnMethodCode_WhenMethodExists()
        {
            string sourceCode = @"
                public class SampleClass
                {
                    public void MyMethod()
                    {
                        Console.WriteLine(""Hello, World!"");
                    }
                }";

            string result = _codeAnalyzer.GetMethodCode(sourceCode, "MyMethod", ".cs");

            Assert.False(string.IsNullOrWhiteSpace(result));
            Assert.Contains("MyMethod", result);
            Assert.Contains("Console.WriteLine", result);
        }

        [Fact]
        public void GetMethodCode_ShouldReturnEmpty_WhenMethodDoesNotExist()
        {
            string sourceCode = @"
                public class SampleClass
                {
                    public void MyMethod()
                    {
                        Console.WriteLine(""Hello, World!"");
                    }
                }";

            string result = _codeAnalyzer.GetMethodCode(sourceCode, "NonExistentMethod", ".cs");

            Assert.True(string.IsNullOrEmpty(result));
        }

        [Fact]
        public void FilterTestMethods_ShouldReturnMatchingTestMethods()
        {
            string testCode = @"
                public class SampleTests
                {
                    [Fact]
                    public void MyTestMethod()
                    {
                        Assert.True(true);
                    }

                    [Fact]
                    public void MyTestMethod_Extra()
                    {
                        Assert.False(false);
                    }
                }";

            string result = _codeAnalyzer.FilterTestMethods(testCode, "MyTestMethod", ".cs");

            Assert.Contains("MyTestMethod", result);
            Assert.Contains("Assert.True", result);
            Assert.Contains("Assert.False", result);
        }

        [Fact]
        public void FilterTestMethods_ShouldReturnEmpty_WhenNoMatchingMethods()
        {
            string testCode = @"
                public class SampleTests
                {
                    [Fact]
                    public void AnotherTest()
                    {
                        Assert.True(true);
                    }
                }";

            string result = _codeAnalyzer.FilterTestMethods(testCode, "NonExistentMethod", ".cs");

            Assert.True(string.IsNullOrEmpty(result));
        }

        [Fact]
        public void GetPublicMethodNames_ShouldReturnListOfPublicMethods()
        {
            string sourceCode = @"
                public class SampleClass
                {
                    public void MethodOne() {}
                    private void MethodTwo() {}
                    protected void MethodThree() {}
                    public void MethodFour() {}
                }";

            List<string> result = _codeAnalyzer.GetPublicMethodNames(sourceCode, ".cs");

            Assert.Equal(2, result.Count);
            Assert.Contains("MethodOne", result);
            Assert.Contains("MethodFour", result);
            Assert.DoesNotContain("MethodTwo", result);
            Assert.DoesNotContain("MethodThree", result);
        }

        [Fact]
        public void GetPublicMethodNames_ShouldReturnEmptyList_WhenNoPublicMethods()
        {
            string sourceCode = @"
                public class SampleClass
                {
                    private void MethodOne() {}
                    protected void MethodTwo() {}
                    internal void MethodThree() {}
                }";

            List<string> result = _codeAnalyzer.GetPublicMethodNames(sourceCode, ".cs");

            Assert.Empty(result);
        }

        [Fact]
        public void GetChangedLines_ShouldReturnChangedLineNumbers()
        {
            string oldContent = "line1\nline2\nline3";
            string newContent = "line1\nline2 changed\nline3\nline4";

            var changedLines = _codeAnalyzer.GetChangedLines(oldContent, newContent);

            Assert.Equal(new List<int> { 2, 4 }, changedLines);
        }

        [Fact]
        public void GetChangedLines_ShouldReturnEmptyList_WhenNoChanges()
        {
            string content = "line1\nline2\nline3";

            var changedLines = _codeAnalyzer.GetChangedLines(content, content);

            Assert.Empty(changedLines);
        }

        [Fact]
        public void GetMethodsAroundLines_ShouldReturnMethods_FromMockedParser()
        {
            string sourceCode = "public class Test { void A() {} void B() {} }";
            var changedLines = new List<int> { 1 };
            var codeExtension = ".cs";

            var mockParser = new Mock<ICodeParser>();
            mockParser.Setup(p => p.ParseSourceCode(sourceCode))
                      .Returns(CSharpSyntaxTree.ParseText(sourceCode));

            var methodSyntax = SyntaxFactory.MethodDeclaration(
                                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                                    "A")
                                .WithBody(SyntaxFactory.Block());

            mockParser.Setup(p => p.GetMethods(It.IsAny<SyntaxTree>(), changedLines))
                      .Returns(new List<MethodDeclarationSyntax> { methodSyntax });

            var methods = _codeAnalyzer.GetMethodsAroundLines(sourceCode, changedLines, codeExtension);

            Assert.Contains("void A", methods[0]);
        }

        [Fact]
        public void GetMethodWithDependencies_ShouldReturnFullClass()
        {
            string sourceCode = @"
                using System;
                namespace TestNamespace {
                    public class Sample {
                        private int x;
                        public Sample() {}
                    }
                }";

            var methods = new List<string> {
                "public void TestMethod() { Console.WriteLine(\"Hello\"); }"
            };

            string result = _codeAnalyzer.GetMethodWithDependencies(sourceCode, methods, ".cs");

            Assert.Contains("using System;", result);
            Assert.Contains("public class Sample", result);
            Assert.Contains("private int x;", result);
            Assert.Contains("public Sample()", result);
            Assert.Contains("public void TestMethod()", result);
            Assert.Contains("// END CLASS", result);
        }

        [Fact]
        public void GetMethodWithDependencies_ShouldHandleMissingClassGracefully()
        {
            string sourceCode = @"
                using System;
                namespace TestNamespace {
                    public class Sample {
                        private int x;
                        public Sample() {}
                    }
                }";
            var methods = new List<string> { "public void Foo() {}" };

            string result = _codeAnalyzer.GetMethodWithDependencies(sourceCode, methods, ".cs");

            Assert.Contains("using System;", result);
            Assert.Contains("// BEGIN CLASS", result);
            Assert.Contains("public void Foo()", result);
            Assert.Contains("// END CLASS", result);
        }

        [Fact]
        public void GetMethodWithDependencies_ShouldIncludeConstructorAndFields()
        {
            string sourceCode = @"
                using System;
                public class MyService
                {
                    private int counter;
                    public MyService() { counter = 0; }
                }";
                    var methods = new List<string>
                {
                    "public void Run() { counter++; }"
                };

            var result = _codeAnalyzer.GetMethodWithDependencies(sourceCode, methods, ".cs");

            Assert.Contains("private int counter;", result);
            Assert.Contains("public MyService()", result);
            Assert.Contains("public void Run()", result);
        }
    }
}
