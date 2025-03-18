using OpenAI.Chat;

namespace AIUnitTestWriter.Wrappers
{
    public interface IChatClient
    {
        /// <summary>
        /// Completes a chat prompt with the given messages.
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<string> CompleteChatAsync(
            IEnumerable<ChatMessage> messages,
            ChatCompletionOptions options,
            CancellationToken cancellationToken = default);
    }
}
