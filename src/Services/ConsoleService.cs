using AIUnitTestWriter.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIUnitTestWriter.Services
{
    public class ConsoleService : IConsoleService
    {
        private readonly ILogger<ConsoleService> _logger;

        public ConsoleService(ILogger<ConsoleService> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public void WriteColored(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            //_logger.LogInformation(message);
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
