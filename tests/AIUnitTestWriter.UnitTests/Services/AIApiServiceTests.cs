using AIUnitTestWriter.Enum;
using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.Services;
using AIUnitTestWriter.SettingOptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OpenAI.Chat;
using System.Net;
using System.Text;
using System.Text.Json;

namespace AIUnitTestWriter.UnitTests.Services
{
    public class AIApiServiceTests
    {
        private readonly CancellationToken _cancellationToken = CancellationToken.None;
        private readonly Mock<IHttpClientWrapper> _httpClientMock;
        private readonly Mock<IHttpRequestMessageFactory> _requestFactoryMock;
        private readonly Mock<IAzureOpenAIClient> _openAIClientMock;
        private readonly Mock<IChatClient> _chatClientMock;
        private readonly IOptions<AISettings> _aiSettings;
        private readonly Mock<ILogger<AIApiService>> _loggerMock;
        private readonly AIApiService _service;

        public AIApiServiceTests()
        {
            _aiSettings = Options.Create(new AISettings
            {
                Endpoint = "https://api.openai.com/v1/chat/completions",
                ApiKey = "test-api-key",
                Model = "gpt-4",
                MaxTokens = 500,
                Temperature = 0.2f,
                Provider = AIProvider.OpenAI
            });

            _httpClientMock = new Mock<IHttpClientWrapper>();
            _requestFactoryMock = new Mock<IHttpRequestMessageFactory>();
            _openAIClientMock = new Mock<IAzureOpenAIClient>();
            _chatClientMock = new Mock<IChatClient>();
            _loggerMock = new Mock<ILogger<AIApiService>>();
            _openAIClientMock
                .Setup(x => x.GetChatClient(_aiSettings.Value.Model))
                .Returns(_chatClientMock.Object);

            _service = new AIApiService(_aiSettings, _httpClientMock.Object, _requestFactoryMock.Object, _openAIClientMock.Object, _loggerMock.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldThrowException_WhenNull()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Assert.Throws<ArgumentNullException>(() => new AIApiService(null, _httpClientMock.Object, _requestFactoryMock.Object, _openAIClientMock.Object, _loggerMock.Object));
            Assert.Throws<ArgumentNullException>(() => new AIApiService(_aiSettings, null, _requestFactoryMock.Object, _openAIClientMock.Object, _loggerMock.Object));
            Assert.Throws<ArgumentNullException>(() => new AIApiService(_aiSettings, _httpClientMock.Object, null, _openAIClientMock.Object, _loggerMock.Object));
            Assert.Throws<ArgumentNullException>(() => new AIApiService(_aiSettings, _httpClientMock.Object, _requestFactoryMock.Object, null, _loggerMock.Object));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }
        #endregion

        #region GenerateTestsOpenAIAsync Tests

        [Fact]
        public async Task GenerateTestsOpenAIAsync_WhenSuccessfulResponse_ReturnsGeneratedText()
        {
            // Arrange
            var expectedText = "Generated Test";
            var jsonResponse = JsonSerializer.Serialize(new
            {
                choices = new[]
                {
                new { text = expectedText }
            }
            });

            var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            var mockRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");

            _requestFactoryMock
                .Setup(f => f.Create(HttpMethod.Post, It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<string>()))
                .Returns(mockRequest);

            _httpClientMock
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), _cancellationToken))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GenerateTestsOpenAIAsync("Write a unit test", _cancellationToken);

            // Assert
            Assert.Equal(expectedText, result);
        }

        [Fact]
        public async Task GenerateTestsOpenAIAsync_WhenApiRequestFails_ReturnsEmptyString()
        {
            // Arrange
            var mockResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);

            var mockRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");

            _requestFactoryMock
                .Setup(f => f.Create(HttpMethod.Post, It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<string>()))
                .Returns(mockRequest);

            _httpClientMock
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), _cancellationToken))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GenerateTestsOpenAIAsync("Write a unit test", _cancellationToken);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GenerateTestsOpenAIAsync_WhenResponseDoesNotContainChoices_ReturnsEmptyString()
        {
            // Arrange
            var jsonResponse = JsonSerializer.Serialize(new
            {
                choices = new object[] { }
            });

            var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            var mockRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");

            _requestFactoryMock
                .Setup(f => f.Create(HttpMethod.Post, It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<string>()))
                .Returns(mockRequest);

            _httpClientMock
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), _cancellationToken))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GenerateTestsOpenAIAsync("Write a unit test", _cancellationToken);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GenerateTestsOpenAIAsync_WhenGeneratedTextIsNullOrWhitespace_ReturnsEmptyString()
        {
            // Arrange
            var jsonResponse = JsonSerializer.Serialize(new
            {
                choices = new[]
                {
                new { text = (string?)null }
            }
            });

            var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            var mockRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");

            _requestFactoryMock
                .Setup(f => f.Create(HttpMethod.Post, It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<string>()))
                .Returns(mockRequest);

            _httpClientMock
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), _cancellationToken))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GenerateTestsOpenAIAsync("Write a unit test", _cancellationToken);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        #endregion GenerateTestsOpenAIAsync Tests

        #region GenerateTestsOllamaAsync Tests

        [Fact]
        public async Task GenerateTestsOllamaAsync_WhenSuccessfulResponse_ReturnsGeneratedText()
        {
            // Arrange
            var expectedText = "Generated Test Code";
            var jsonResponse = JsonSerializer.Serialize(new
            {
                response = expectedText
            });

            var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            var mockRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.ollama.com/v1/generate");

            _requestFactoryMock
                .Setup(f => f.Create(HttpMethod.Post, It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<string>()))
                .Returns(mockRequest);

            _httpClientMock
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), _cancellationToken))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GenerateTestsOllamaAsync("Write a unit test", _cancellationToken);

            // Assert
            Assert.Equal(expectedText, result);
        }

        [Fact]
        public async Task GenerateTestsOllamaAsync_WhenApiRequestFails_ReturnsEmptyString()
        {
            // Arrange
            var mockResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);

            var mockRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.ollama.com/v1/generate");

            _requestFactoryMock
                .Setup(f => f.Create(HttpMethod.Post, It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<string>()))
                .Returns(mockRequest);

            _httpClientMock
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), _cancellationToken))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GenerateTestsOllamaAsync("Write a unit test", _cancellationToken);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GenerateTestsOllamaAsync_WhenResponseIsEmpty_ReturnsEmptyString()
        {
            // Arrange
            var jsonResponse = JsonSerializer.Serialize(new { response = "" });

            var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            var mockRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.ollama.com/v1/generate");

            _requestFactoryMock
                .Setup(f => f.Create(HttpMethod.Post, It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<string>()))
                .Returns(mockRequest);

            _httpClientMock
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), _cancellationToken))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GenerateTestsOllamaAsync("Write a unit test", _cancellationToken);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GenerateTestsOllamaAsync_RemovesThinkTags()
        {
            // Arrange
            var responseText = "<think>Thinking...</think>Generated Test Code";
            var jsonResponse = JsonSerializer.Serialize(new { response = responseText });

            var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            var mockRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.ollama.com/v1/generate");

            _requestFactoryMock
                .Setup(f => f.Create(HttpMethod.Post, It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<string>()))
                .Returns(mockRequest);

            _httpClientMock
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), _cancellationToken))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GenerateTestsOllamaAsync("Write a unit test", _cancellationToken);

            // Assert
            Assert.Equal("Generated Test Code", result);
        }

        [Fact]
        public async Task GenerateTestsOllamaAsync_WhenAvailable_ExtractsCodeBlock()
        {
            // Arrange
            var responseText = "Here is the code: ```csharp\npublic class Test {}\n```";
            var jsonResponse = JsonSerializer.Serialize(new { response = responseText });

            var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };

            var mockRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.ollama.com/v1/generate");

            _requestFactoryMock
                .Setup(f => f.Create(HttpMethod.Post, It.IsAny<string>(), It.IsAny<HttpContent>(), It.IsAny<string>()))
                .Returns(mockRequest);

            _httpClientMock
                .Setup(c => c.SendAsync(It.IsAny<HttpRequestMessage>(), _cancellationToken))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _service.GenerateTestsOllamaAsync("Write a unit test", _cancellationToken);

            // Assert
            Assert.Equal("public class Test {}", result);
        }

        #endregion GenerateTestsOllamaAsync Tests

        #region GenerateTestsAzureOpenAIAsync Tests

        [Fact]
        public async Task GenerateTestsAzureOpenAIAsync_WhenSuccessful_ReturnsGeneratedText()
        {
            // Arrange
            var expectedText = "Generated unit test";
            var completionOptions = new ChatCompletionOptions
            {
                Temperature = 0.2f,
                MaxOutputTokenCount = 500
            };

            _chatClientMock
                .Setup(x => x.CompleteChatAsync(
                    It.IsAny<List<ChatMessage>>(),
                    It.IsAny<ChatCompletionOptions>(),
                    _cancellationToken))
                .ReturnsAsync(expectedText);

            // Act
            var result = await _service.GenerateTestsAzureOpenAIAsync("Write a unit test", _cancellationToken);

            // Assert
            Assert.Equal(expectedText, result);
        }

        [Fact]
        public async Task GenerateTestsAzureOpenAIAsync_WhenGeneratedTextIsNull_ReturnsEmptyString()
        {
            // Arrange
            _chatClientMock
                .Setup(x => x.CompleteChatAsync(
                    It.IsAny<List<ChatMessage>>(),
                    It.IsAny<ChatCompletionOptions>(),
                    _cancellationToken))
                .ReturnsAsync((string?)null);

            // Act
            var result = await _service.GenerateTestsAzureOpenAIAsync("Write a unit test", _cancellationToken);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GenerateTestsAzureOpenAIAsync_WhenGeneratedTextIsWhitespace_ReturnsEmptyString()
        {
            // Arrange
            _chatClientMock
                .Setup(x => x.CompleteChatAsync(
                    It.IsAny<List<ChatMessage>>(),
                    It.IsAny<ChatCompletionOptions>(),
                    _cancellationToken))
                .ReturnsAsync("   ");

            // Act
            var result = await _service.GenerateTestsAzureOpenAIAsync("Write a unit test", _cancellationToken);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GenerateTestsAzureOpenAIAsync_HandlesException_AndReturnsEmptyString()
        {
            // Arrange
            _chatClientMock
                .Setup(x => x.CompleteChatAsync(
                    It.IsAny<List<ChatMessage>>(),
                    It.IsAny<ChatCompletionOptions>(),
                    _cancellationToken))
                .ThrowsAsync(new Exception("API Error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(async () => await _service.GenerateTestsAzureOpenAIAsync("Write a unit test", _cancellationToken));
            Assert.Equal("API Error", exception.Message);
        }

        #endregion GenerateTestsAzureOpenAIAsync Tests
    }
}