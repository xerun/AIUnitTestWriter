using AIUnitTestWriter.Services;

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
            // Arrange
            string sourceCode = @"
            public class SampleClass
            {
                public void MyMethod()
                {
                    Console.WriteLine(""Hello, World!"");
                }
            }";

            // Act
            string result = _codeAnalyzer.GetMethodCode(sourceCode, "MyMethod");

            // Assert
            Assert.NotNull(result);
            Assert.Contains("MyMethod", result);
            Assert.Contains("Console.WriteLine", result);
        }

        [Fact]
        public void GetMethodCode_ShouldReturnNull_WhenMethodDoesNotExist()
        {
            // Arrange
            string sourceCode = @"
            public class SampleClass
            {
                public void MyMethod()
                {
                    Console.WriteLine(""Hello, World!"");
                }
            }";

            // Act
            string result = _codeAnalyzer.GetMethodCode(sourceCode, "NonExistentMethod");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FilterTestMethods_ShouldReturnMatchingTestMethods()
        {
            // Arrange
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

            // Act
            string result = _codeAnalyzer.FilterTestMethods(testCode, "MyTestMethod");

            // Assert
            Assert.Contains("MyTestMethod", result);
            Assert.Contains("Assert.True(true);", result);
            Assert.Contains("Assert.False(false);", result);
        }

        [Fact]
        public void FilterTestMethods_ShouldReturnEmpty_WhenNoMatchingMethods()
        {
            // Arrange
            string testCode = @"
            public class SampleTests
            {
                [Fact]
                public void AnotherTest()
                {
                    Assert.True(true);
                }
            }";

            // Act
            string result = _codeAnalyzer.FilterTestMethods(testCode, "NonExistentMethod");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetPublicMethodNames_ShouldReturnListOfPublicMethods()
        {
            // Arrange
            string sourceCode = @"
            public class SampleClass
            {
                public void MethodOne() {}
                private void MethodTwo() {}
                protected void MethodThree() {}
                public void MethodFour() {}
            }";

            // Act
            List<string> result = _codeAnalyzer.GetPublicMethodNames(sourceCode);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains("MethodOne", result);
            Assert.Contains("MethodFour", result);
            Assert.DoesNotContain("MethodTwo", result);
            Assert.DoesNotContain("MethodThree", result);
        }

        [Fact]
        public void GetPublicMethodNames_ShouldReturnEmptyList_WhenNoPublicMethods()
        {
            // Arrange
            string sourceCode = @"
            public class SampleClass
            {
                private void MethodOne() {}
                protected void MethodTwo() {}
                internal void MethodThree() {}
            }";

            // Act
            List<string> result = _codeAnalyzer.GetPublicMethodNames(sourceCode);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
