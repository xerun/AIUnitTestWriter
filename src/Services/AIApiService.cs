using AIUnitTestWriter.Enum;
using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.SettingOptions;
using AIUnitTestWriter.Wrappers;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AIUnitTestWriter.Services
{
    public class AIApiService : IAIApiService
    {
        private readonly IHttpClientWrapper _httpClient;
        private readonly IHttpRequestMessageFactory _requestFactory;
        private readonly IAzureOpenAIClient _openAIClient;
        private readonly AISettings _aiSettings;
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _model;
        private readonly int _maxTokens;
        private readonly float _temperature;
        private readonly AIProvider _provider;

        public AIApiService(IOptions<AISettings> aiSettings, IHttpClientWrapper httpClientWrapper, IHttpRequestMessageFactory requestFactory, IAzureOpenAIClient openAIClient)
        {
            _httpClient = httpClientWrapper ?? throw new ArgumentNullException(nameof(httpClientWrapper));
            _requestFactory = requestFactory ?? throw new ArgumentNullException(nameof(requestFactory));
            _aiSettings = aiSettings?.Value ?? throw new ArgumentNullException(nameof(aiSettings));
            _openAIClient = openAIClient ?? throw new ArgumentNullException(nameof(openAIClient));

            _apiKey = _aiSettings.ApiKey;
            _provider = _aiSettings.Provider;
            _endpoint = _aiSettings.Endpoint;
            _model = _aiSettings.Model;
            _maxTokens = _aiSettings.MaxTokens;
            _temperature = _aiSettings.Temperature;
        }

        /// <inheritdoc/>
        [ExcludeFromCodeCoverage(Justification = "All the related methods are already tested.")]
        public async Task<string> GenerateTestsAsync(string prompt, CancellationToken cancellationToken = default)
        {
            return _provider switch
            {
                AIProvider.Ollama => await GenerateTestsOllamaAsync(prompt, cancellationToken),
                AIProvider.AzureOpenAI => await GenerateTestsAzureOpenAIAsync(prompt, cancellationToken),
                _ => await GenerateTestsOpenAIAsync(prompt, cancellationToken),
            };
        }

        internal async Task<string> GenerateTestsOpenAIAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var requestBody = new
            {
                model = _model,
                prompt,
                stream = false,
                max_tokens = _maxTokens,
                temperature = _temperature
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            // Use API key for OpenAI.
            var request = _requestFactory.Create(HttpMethod.Post, _endpoint, httpContent, _apiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"OpenAI API request failed: {response.StatusCode}");
                return string.Empty;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);

            using (var doc = JsonDocument.Parse(jsonResponse))
            {
                var root = doc.RootElement;
                var choices = root.GetProperty("choices");
                if (choices.GetArrayLength() > 0)
                {
                    var generatedText = choices[0].GetProperty("text").GetString();
                    if (!string.IsNullOrWhiteSpace(generatedText))
                    {
                        return generatedText.Trim();
                    }
                }
            }
            return string.Empty;
        }

        internal async Task<string> GenerateTestsOllamaAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var requestBody = new
            {
                model = _model,
                prompt,
                stream = false,
                max_tokens = _maxTokens,
                temperature = _temperature
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var request = _requestFactory.Create(HttpMethod.Post, _endpoint, httpContent, _apiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ollama API request failed: {response.StatusCode}");
                return string.Empty;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var generatedText = string.Empty;

            using (var doc = JsonDocument.Parse(jsonResponse))
            {
                generatedText = doc.RootElement.GetProperty("response").GetString();
            }

            // Remove everything inside <think>...</think> tags if present for reasoning models.
            if (string.IsNullOrWhiteSpace(generatedText))
            {
                return string.Empty;
            }
            generatedText = Regex.Replace(generatedText, "<think>.*?</think>", string.Empty, RegexOptions.Singleline).Trim();

            // Extract code between triple backticks if available.
            var codeBlockMatch = Regex.Match(generatedText, @"```csharp\s*(.*?)```", RegexOptions.Singleline);
            if (codeBlockMatch.Success)
            {
                generatedText = codeBlockMatch.Groups[1].Value.Trim();
            }

            return generatedText;
        }

        internal async Task<string> GenerateTestsAzureOpenAIAsync(string prompt, CancellationToken cancellationToken = default)
        {
            var chatClient = _openAIClient.GetChatClient(_model);

            var options = new ChatCompletionOptions
            {
                Temperature = _temperature,
                MaxOutputTokenCount = _maxTokens
            };

            var generatedText = await chatClient.CompleteChatAsync(
                new List<ChatMessage>
                {
                    new SystemChatMessage("You are a helpful AI that generates unit tests."),
                    new UserChatMessage(prompt)
                }, options, cancellationToken);

            if (string.IsNullOrWhiteSpace(generatedText))
            {
                return string.Empty;
            }
            return generatedText.Trim();
        }
    }
}
