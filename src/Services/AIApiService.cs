using AIUnitTestWriter.SettingOptions;
using AIUnitTestWriter.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AIUnitTestWriter.Services
{
    public class AIApiService : IAIApiService
    {
        private readonly HttpClient _httpClient;
        private readonly AISettings _aiSettings;
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _model;
        private readonly int _maxTokens;
        private readonly double _temperature;
        private readonly string _provider;

        public AIApiService(IOptions<AISettings> aiSettings, IHttpClientFactory httpClientFactory)
        {
            if (httpClientFactory == null) throw new ArgumentNullException(nameof(httpClientFactory));
            _httpClient = httpClientFactory.CreateClient();
            _aiSettings = aiSettings.Value;

            _apiKey = _aiSettings.ApiKey ?? throw new ArgumentNullException(nameof(_aiSettings.ApiKey));
            _provider = _aiSettings.Provider ?? throw new ArgumentNullException(nameof(_aiSettings.Provider));
            _endpoint = _aiSettings.Endpoint ?? throw new ArgumentNullException(nameof(_aiSettings.Endpoint));
            _model = _aiSettings.Model ?? throw new ArgumentNullException(nameof(_aiSettings.Model));
            _maxTokens = _aiSettings.MaxTokens;
            _temperature = _aiSettings.Temperature;
        }

        public async Task<string> GenerateTestsAsync(string prompt)
        {
            if (_provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                return await GenerateTestsOpenAI(prompt);
            }
            else if (_provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
            {
                return await GenerateTestsOllama(prompt);
            }
            else
            {
                // Default to OpenAI if provider is not recognized.
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
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _httpClient.PostAsync(_endpoint, httpContent);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"OpenAI API request failed: {response.StatusCode}");
                return string.Empty;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();

            // Parse the JSON response assuming the API returns a choices array.
            var doc = JsonDocument.Parse(jsonResponse);            
            var root = doc.RootElement;
            var choices = root.GetProperty("choices");
            if (choices.GetArrayLength() > 0)
            {
                var generatedText = choices[0].GetProperty("text").GetString();
                return generatedText.Trim();
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

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            // Assume local Ollama does not require an API key.
            var response = await _httpClient.PostAsync(_endpoint, httpContent);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Ollama API request failed: {response.StatusCode}");
                return string.Empty;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var generatedText = string.Empty;

            var doc = JsonDocument.Parse(jsonResponse);            
            generatedText = doc.RootElement.GetProperty("response").GetString();            

            // Remove everything inside <think>...</think> tags if present for reasoning models..
            if (!string.IsNullOrWhiteSpace(generatedText))
            {
                generatedText = Regex.Replace(generatedText, "<think>.*?</think>", string.Empty, RegexOptions.Singleline).Trim();
            }

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
