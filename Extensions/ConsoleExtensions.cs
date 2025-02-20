namespace AIUnitTestWriter.Extensions
{
    public static class ConsoleExtensions
    {
        /// <summary>
        /// Writes the string message to the console using the specified color.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="color">The color to use for the message.</param>
        public static void WriteColored(this string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Ask user to enter any prompt.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static string Prompt(string message, ConsoleColor color)
        {
            WriteColored(message, color);
            return Console.ReadLine()?.Trim();
        }
    }
}