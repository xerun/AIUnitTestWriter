using AIUnitTestWriter.Services;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class ConsoleServiceTests
    {
        private readonly ConsoleService _consoleService;

        public ConsoleServiceTests()
        {
            _consoleService = new ConsoleService();
        }

        [Fact]
        public void WriteColored_ShouldWriteMessageWithColor()
        {
            // Arrange
            string message = "Test message";
            ConsoleColor color = ConsoleColor.Green;

            using (StringWriter sw = new StringWriter())
            {
                Console.SetOut(sw); // Redirect Console output

                // Act
                _consoleService.WriteColored(message, color);

                // Assert
                string consoleOutput = sw.ToString().Trim();
                Assert.Equal(message, consoleOutput);
            }
        }

        [Fact]
        public void Prompt_ShouldWriteMessageAndReturnUserInput()
        {
            // Arrange
            string expectedMessage = "Enter name:";
            string expectedInput = "JohnDoe";
            ConsoleColor color = ConsoleColor.Blue;

            using (StringWriter sw = new StringWriter())
            using (StringReader sr = new StringReader(expectedInput))
            {
                Console.SetOut(sw);  // Redirect Console output
                Console.SetIn(sr);   // Simulate Console input

                // Act
                string result = _consoleService.Prompt(expectedMessage, color);

                // Assert
                Assert.Equal(expectedInput, result);

                string consoleOutput = sw.ToString().Trim();
                Assert.Equal(expectedMessage, consoleOutput);
            }
        }
    }
}
