using AIUnitTestWriter.Extensions;

namespace AIUnitTestWriter.Services
{
    public static class UserPrompter
    {
        public static string Prompt(string message, ConsoleColor color)
        {
            return ConsoleExtensions.Prompt(message, color);
        }
    }
}
