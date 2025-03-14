using AIUnitTestWriter.Interfaces;

namespace AIUnitTestWriter.Services
{
    public class ConsoleService : IConsoleService
    {
        /// <inheritdoc/>
        public void WriteColored(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <inheritdoc/>
        public string Prompt(string message, ConsoleColor color)
        {
            WriteColored(message, color);
            return Console.ReadLine()?.Trim();
        }

        /// <inheritdoc/>
        public string ReadLine() => Console.ReadLine();
    }
}
