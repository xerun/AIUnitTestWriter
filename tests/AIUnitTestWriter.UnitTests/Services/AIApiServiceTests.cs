using AIUnitTestWriter.Services;
using AIUnitTestWriter.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class AIApiServiceTests
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly IConfiguration _configuration;
        private IAIApiService _aiApiService;
        private readonly HttpClient _httpClient;

        public AIApiServiceTests()
        {
            // Mock Configuration Settings
            var configData = new Dictionary<string, string>
        {
            { "AI:ApiKey", "test-api-key" },
            { "AI:Provider", "OpenAI" },
            { "AI:Endpoint", "https://fake-api.com" },
            { "AI:Model", "gpt-4" },
            { "AI:MaxTokens", "1000" },
            { "AI:Temperature", "0.7" }
        };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Mock HttpMessageHandler
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            // Create HttpClient with mocked handler
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

            // Inject HttpClient into AIApiService
            _aiApiService = new AIApiService(_configuration, _httpClient);
        }

        [Fact]
        public async Task GenerateTestsAsync_OpenAI_ReturnsGeneratedCode()
        {
            // Arrange
            var expectedResponse = @"{
            ""choices"": [{ ""text"": ""public class MyTest { }"" }]
        }";

            MockHttpResponse(HttpStatusCode.OK, expectedResponse);

            // Act
            var result = await _aiApiService.GenerateTestsAsync("Generate test code");

            // Assert
            Assert.Equal("public class MyTest { }", result);
        }

        [Fact]
        public async Task GenerateTestsAsync_Ollama_ReturnsGeneratedCode()
        {
            // Arrange
            SetProvider("Ollama");
            var expectedResponse = @"{ ""response"": ""```public class MyTest { }```"" }";

            MockHttpResponse(HttpStatusCode.OK, expectedResponse);

            // Act
            var result = await _aiApiService.GenerateTestsAsync("Generate test code");

            // Assert
            Assert.Equal("public class MyTest { }", result);
        }

        [Fact]
        public async Task GenerateTestsAsync_WhenApiFails_ReturnsEmptyString()
        {
            // Arrange
            MockHttpResponse(HttpStatusCode.BadRequest, "{}");

            // Act
            var result = await _aiApiService.GenerateTestsAsync("Generate test code");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GenerateTestsAsync_OpenAI_HandlesMalformedJsonGracefully()
        {
            // Arrange
            var invalidJson = "{ \"choices\": [{ \"text\": ";

            MockHttpResponse(HttpStatusCode.OK, invalidJson);

            // Act & Assert
            await Assert.ThrowsAsync<JsonException>(() => _aiApiService.GenerateTestsAsync("Generate test code"));
        }

        [Fact]
        public async Task GenerateTestsAsync_Ollama_HandlesThinkTagsAndCodeExtraction()
        {
            // Arrange
            SetProvider("Ollama");
            var responseWithThinkTags = @"{
            ""response"": ""<think>Ignore this</think> ```public class MyTest { }```""
        }";

            MockHttpResponse(HttpStatusCode.OK, responseWithThinkTags);

            // Act
            var result = await _aiApiService.GenerateTestsAsync("Generate test code");

            // Assert
            Assert.Equal("public class MyTest { }", result);
        }

        [Fact]
        public void Constructor_ThrowsException_WhenConfigMissing()
        {
            // Arrange
            var configData = new Dictionary<string, string>
        {
            { "AI:Provider", "OpenAI" } // Missing required keys
        };

            var faultyConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() => new AIApiService(faultyConfig, _httpClient));
            Assert.Contains("not configured", exception.Message);
        }

        /// <summary>
        /// Helper method to mock an HTTP response
        /// </summary>
        private void MockHttpResponse(HttpStatusCode statusCode, string responseContent)
        {
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                });
        }

        /// <summary>
        /// Helper method to update AI provider in configuration
        /// </summary>
        private void SetProvider(string provider)
        {
            var newConfigData = new Dictionary<string, string>
        {
            { "AI:ApiKey", "test-api-key" },
            { "AI:Provider", provider },
            { "AI:Endpoint", "https://fake-api.com" },
            { "AI:Model", "gpt-4" },
            { "AI:MaxTokens", "1000" },
            { "AI:Temperature", "0.7" }
        };

            var updatedConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(newConfigData)
                .Build();

            // Reinitialize the service with updated config
            _aiApiService = new AIApiService(updatedConfig, _httpClient);
        }
    }
}
