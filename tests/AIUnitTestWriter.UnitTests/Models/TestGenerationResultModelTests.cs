using AIUnitTestWriter.Models;

namespace AIUnitTestWriter.UnitTests.Models
{
    public class TestGenerationResultModelTests
    {
        [Fact]
        public void TestGenerationResultModel_ShouldSetAndGetPropertiesCorrectly()
        {
            // Arrange
            var result = new TestGenerationResultModel
            {
                TempFilePath = "/temp/test.cs",
                TestFilePath = "/tests/test.cs",
                GeneratedTestCode = "public class SampleTest { /* test code */ }"
            };

            // Act & Assert
            Assert.Equal("/temp/test.cs", result.TempFilePath);
            Assert.Equal("/tests/test.cs", result.TestFilePath);
            Assert.Equal("public class SampleTest { /* test code */ }", result.GeneratedTestCode);
        }

    }
}
