using AIUnitTestWriter.Services;
using AIUnitTestWriter.SettingOptions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class AIApiServiceTests
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly HttpClient _httpClient;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly IOptions<AISettings> _mockAISettings;

        public AIApiServiceTests()
        {
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(_httpClient);

            _mockAISettings = Options.Create(new AISettings
            {
                ApiKey = "test-key",
                Provider = "OpenAI",
                Endpoint = "https://api.openai.com/v1/completions",
                Model = "gpt-4",
                MaxTokens = 100,
                Temperature = 0.7
            });
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenHttpClientFactoryIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new AIApiService(_mockAISettings, null));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenAISettingsAreInvalid()
        {
            var invalidSettings = Options.Create(new AISettings { ApiKey = null });

            Assert.Throws<ArgumentNullException>(() => new AIApiService(invalidSettings, _mockHttpClientFactory.Object));
        }

        [Fact]
        public async Task GenerateTestsAsync_ShouldCallGenerateTestsOpenAI_WhenProviderIsOpenAI()
        {
            // Arrange
            var aiService = new AIApiService(_mockAISettings, _mockHttpClientFactory.Object);

            var mockResponse = new
            {
                choices = new[]
                {
                new { text = "Generated OpenAI test" }
            }
            };

            var jsonResponse = JsonSerializer.Serialize(mockResponse);
            SetupHttpResponse(jsonResponse, HttpStatusCode.OK);

            // Act
            var result = await aiService.GenerateTestsAsync("Test prompt");

            // Assert
            Assert.Equal("Generated OpenAI test", result);
        }

        [Fact]
        public async Task GenerateTestsAsync_ShouldCallGenerateTestsOllama_WhenProviderIsOllama()
        {
            // Arrange
            var ollamaSettings = Options.Create(new AISettings
            {
                ApiKey = "test-key",
                Provider = "Ollama",
                Endpoint = "http://localhost:11434/api/generate",
                Model = "ollama-model",
                MaxTokens = 100,
                Temperature = 0.7
            });

            var aiService = new AIApiService(ollamaSettings, _mockHttpClientFactory.Object);

            var mockResponse = new { response = "Generated Ollama test" };
            var jsonResponse = JsonSerializer.Serialize(mockResponse);
            SetupHttpResponse(jsonResponse, HttpStatusCode.OK);

            // Act
            var result = await aiService.GenerateTestsAsync("Test prompt");

            // Assert
            Assert.Equal("Generated Ollama test", result);
        }

        [Fact]
        public async Task GenerateTestsAsync_ShouldReturnEmptyString_WhenHttpResponseIsNotSuccessful()
        {
            // Arrange
            var aiService = new AIApiService(_mockAISettings, _mockHttpClientFactory.Object);

            SetupHttpResponse("{}", HttpStatusCode.BadRequest);

            // Act
            var result = await aiService.GenerateTestsAsync("Test prompt");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GenerateTestsOllama_ShouldRemoveThinkTags_AndExtractCSharpCode()
        {
            // Arrange
            var ollamaSettings = Options.Create(new AISettings
            {
                ApiKey = "test-key",
                Provider = "Ollama",
                Endpoint = "http://localhost:11434/api/generate",
                Model = "ollama-model",
                MaxTokens = 100,
                Temperature = 0.7
            });

            var aiService = new AIApiService(ollamaSettings, _mockHttpClientFactory.Object);

            var mockResponse = new
            {
                response = "<think>Some thoughts...</think> ```csharp\npublic class Test { }\n```"
            };

            var jsonResponse = JsonSerializer.Serialize(mockResponse);
            SetupHttpResponse(jsonResponse, HttpStatusCode.OK);

            // Act
            var result = await aiService.GenerateTestsAsync("Test prompt");

            // Assert
            Assert.Equal("public class Test { }", result);
        }

        [Fact]
        public async Task GenerateTestsOllama_ShouldReturnFullResponse_IfNoCodeBlockExists()
        {
            // Arrange
            var ollamaSettings = Options.Create(new AISettings
            {
                ApiKey = "test-key",
                Provider = "Ollama",
                Endpoint = "http://localhost:11434/api/generate",
                Model = "ollama-model",
                MaxTokens = 100,
                Temperature = 0.7
            });

            var aiService = new AIApiService(ollamaSettings, _mockHttpClientFactory.Object);

            var mockResponse = new { response = "Regular text response" };
            var jsonResponse = JsonSerializer.Serialize(mockResponse);
            SetupHttpResponse(jsonResponse, HttpStatusCode.OK);

            // Act
            var result = await aiService.GenerateTestsAsync("Test prompt");

            // Assert
            Assert.Equal("Regular text response", result);
        }

        private void SetupHttpResponse(string responseBody, HttpStatusCode statusCode)
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                });
        }
    }
}
