using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.SettingOptions;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;

namespace AIUnitTestWriter.Wrappers
{
    public class AzureOpenAIClientWrapper : IAzureOpenAIClient
    {
        private readonly AISettings _aiSettings;
        private readonly string _endpoint;
        private readonly AzureKeyCredential _apiKey;
        private readonly AzureOpenAIClient _openAIClient;

        public AzureOpenAIClientWrapper(IOptions<AISettings> aiSettings)
        {
            _aiSettings = aiSettings?.Value ?? throw new ArgumentNullException(nameof(aiSettings));
            _endpoint = _aiSettings.Endpoint;
            _apiKey = new AzureKeyCredential(_aiSettings.ApiKey);
            _openAIClient = new AzureOpenAIClient(new Uri(_endpoint), _apiKey);
        }

        /// <inheritdoc/>
        public IChatClient GetChatClient(string model)
        {
            var chatClient = _openAIClient.GetChatClient(model);
            return new ChatClientWrapper(chatClient);
        }
    }
}
