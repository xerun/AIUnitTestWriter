using AIUnitTestWriter.Enum;
using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.SettingOptions;
using AIUnitTestWriter.Wrappers;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AIUnitTestWriter.Services
{
    public class AIApiService : IAIApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpRequestMessageFactory _requestFactory;
        private readonly AISettings _aiSettings;
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _model;
        private readonly int _maxTokens;
        private readonly double _temperature;
        private readonly AIProvider _provider;

        public AIApiService(IOptions<AISettings> aiSettings, IHttpClientFactory httpClientFactory, IHttpRequestMessageFactory requestFactory)
        {
            if (httpClientFactory == null) throw new ArgumentNullException(nameof(httpClientFactory));
            _httpClient = httpClientFactory.CreateClient();
            _requestFactory = requestFactory ?? throw new ArgumentNullException(nameof(requestFactory));
            _aiSettings = aiSettings?.Value ?? throw new ArgumentNullException(nameof(aiSettings));

            _apiKey = _aiSettings.ApiKey;
            _provider = _aiSettings.Provider;
            _endpoint = _aiSettings.Endpoint;
            _model = _aiSettings.Model;
            _maxTokens = _aiSettings.MaxTokens;
            _temperature = _aiSettings.Temperature;
        }

        public async Task<string> GenerateTestsAsync(string prompt)
        {
            if (_provider == AIProvider.Ollama)
            {
                return await GenerateTestsOllama(prompt);
            }
            else
            {
                return await GenerateTestsOpenAI(prompt);
            }
        }

        private async Task<string> GenerateTestsOpenAI(string prompt)
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

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"OpenAI API request failed: {response.StatusCode}");
                return string.Empty;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();

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

        private async Task<string> GenerateTestsOllama(string prompt)
        {
            // For Ollama, the payload may be similar but could be adjusted as needed.
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

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ollama API request failed: {response.StatusCode}");
                return string.Empty;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
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
    }
}
