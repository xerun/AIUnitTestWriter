namespace AIUnitTestWriter.Interfaces
{
    public interface IConsoleService
    {
        /// <summary>
        /// Writes the string message to the console using the specified color.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="color">The color to use for the message.</param>
        void WriteColored(string message, ConsoleColor color);

        /// <summary>
        /// Ask user to enter any prompt.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        string Prompt(string message, ConsoleColor color);

        /// <summary>
        /// Reads a line of characters from the console.
        /// </summary>
        /// <returns></returns>
        string ReadLine();
    }
}
