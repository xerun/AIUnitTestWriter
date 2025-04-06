using AIUnitTestWriter.Interfaces;
using OpenAI.Chat;

namespace AIUnitTestWriter.Wrappers
{
    public class ChatClientWrapper : IChatClient
    {
        private readonly ChatClient _chatClient;

        public ChatClientWrapper(ChatClient chatClient)
        {
            _chatClient = chatClient;
        }

        /// <inheritdoc/>
        public async Task<string> CompleteChatAsync(
            IEnumerable<ChatMessage> messages,
            ChatCompletionOptions options,
            CancellationToken cancellationToken = default)
        {
            var result = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            return result.Value.Content[0].Text;
        }
    }
}
