using AIUnitTestWriter.Enum;
using AIUnitTestWriter.Services;
using AIUnitTestWriter.SettingOptions;
using AIUnitTestWriter.Wrappers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AIUnitTestWriter.UnitTests.Services
{

    #region Fake Implementations

    // Fake HttpMessageHandler for testing.
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
        {
            _handlerFunc = handlerFunc;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handlerFunc(request, cancellationToken);
        }
    }

    // Fake HttpClientFactory for testing.
    public class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;
        public FakeHttpClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name = "")
        {
            return _client;
        }
    }

    // Fake IHttpRequestMessageFactory for testing.
    public class FakeHttpRequestMessageFactory : IHttpRequestMessageFactory
    {
        public HttpRequestMessage LastRequest { get; private set; }

        public HttpRequestMessage Create(HttpMethod method, string requestUri, HttpContent content, string apiKey)
        {
            var request = new HttpRequestMessage(method, requestUri)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            LastRequest = request;
            return request;
        }
    }

    #endregion

    #region Test Builder

    /// <summary>
    /// Builder that encapsulates the construction of an AIApiService instance
    /// with customizable AISettings, HTTP response behavior, and a request factory.
    /// </summary>
    public class AIApiServiceTestBuilder
    {
        private AISettings _settings = new AISettings
        {
            ApiKey = "key",
            Provider = AIProvider.OpenAI,
            Endpoint = "http://test",
            Model = "model",
            MaxTokens = 100,
            Temperature = 0.5
        };

        private Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler =
            (req, ct) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

        private IHttpRequestMessageFactory _requestFactory = new FakeHttpRequestMessageFactory();

        public AIApiServiceTestBuilder WithSettings(AISettings settings)
        {
            _settings = settings;
            return this;
        }

        public AIApiServiceTestBuilder WithHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
            return this;
        }

        public AIApiServiceTestBuilder WithRequestFactory(IHttpRequestMessageFactory factory)
        {
            _requestFactory = factory;
            return this;
        }

        public AIApiService Build()
        {
            var fakeHandler = new FakeHttpMessageHandler(_handler);
            var httpClient = new HttpClient(fakeHandler);
            var fakeFactory = new FakeHttpClientFactory(httpClient);
            return new AIApiService(Options.Create(_settings), fakeFactory, _requestFactory);
        }
    }

    #endregion

    public class AIApiServiceTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_NullHttpClientFactory_ThrowsArgumentNullException()
        {
            var options = Options.Create(new AISettings
            {
                ApiKey = "key",
                Provider = AIProvider.OpenAI,
                Endpoint = "http://test",
                Model = "model",
                MaxTokens = 100,
                Temperature = 0.5
            });
            var requestFactory = new FakeHttpRequestMessageFactory();
            Assert.Throws<ArgumentNullException>(() => new AIApiService(options, null, requestFactory));
        }

        [Fact]
        public void Constructor_NullAISettings_ThrowsArgumentNullException()
        {
            var fakeFactory = new FakeHttpClientFactory(new HttpClient());
            var requestFactory = new FakeHttpRequestMessageFactory();
            Assert.Throws<ArgumentNullException>(() => new AIApiService(null, fakeFactory, requestFactory));
        }

        [Fact]
        public void Constructor_NullRequestFactory_ThrowsArgumentNullException()
        {
            var options = Options.Create(new AISettings
            {
                ApiKey = "key",
                Provider = AIProvider.OpenAI,
                Endpoint = "http://test",
                Model = "model",
                MaxTokens = 100,
                Temperature = 0.5
            });
            var fakeFactory = new FakeHttpClientFactory(new HttpClient());
            var logger = NullLogger<AIApiService>.Instance;
            Assert.Throws<ArgumentNullException>(() => new AIApiService(options, fakeFactory, null));
        }

        #endregion

        #region GenerateTestsAsync Tests

        [Fact]
        public async Task GenerateTestsAsync_OpenAI_Success_ReturnsTrimmedText()
        {
            // Arrange: Fake OpenAI response with choices array.
            var responseContent = JsonSerializer.Serialize(new
            {
                choices = new[] { new { text = " Generated text " } }
            });

            var service = new AIApiServiceTestBuilder()
                .WithSettings(new AISettings
                {
                    ApiKey = "key",
                    Provider = AIProvider.OpenAI,
                    Endpoint = "http://test",
                    Model = "model",
                    MaxTokens = 100,
                    Temperature = 0.5
                })
                .WithHandler((request, ct) =>
                    Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                    }))
                .Build();

            // Act
            var result = await service.GenerateTestsAsync("prompt");

            // Assert
            Assert.Equal("Generated text", result);
        }

        [Fact]
        public async Task GenerateTestsAsync_OpenAI_Failure_ReturnsEmpty()
        {
            // Arrange: Simulate failure response from OpenAI.
            var service = new AIApiServiceTestBuilder()
                .WithSettings(new AISettings
                {
                    ApiKey = "key",
                    Provider = AIProvider.OpenAI,
                    Endpoint = "http://test",
                    Model = "model",
                    MaxTokens = 100,
                    Temperature = 0.5
                })
                .WithHandler((request, ct) =>
                    Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)))
                .Build();

            // Act
            var result = await service.GenerateTestsAsync("prompt");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task GenerateTestsAsync_Ollama_Success_WithCodeBlock_ReturnsExtractedCode()
        {
            // Arrange: Fake Ollama response with a code block.
            var responseText = "Some prefix <think>irrelevant</think> ```csharp\n//Extracted code\n``` Some suffix";
            var responseContent = JsonSerializer.Serialize(new { response = responseText });
            var fakeRequestFactory = new FakeHttpRequestMessageFactory();

            var service = new AIApiServiceTestBuilder()
                .WithSettings(new AISettings
                {
                    ApiKey = "key",
                    Provider = AIProvider.Ollama,
                    Endpoint = "http://test",
                    Model = "model",
                    MaxTokens = 100,
                    Temperature = 0.5
                })
                .WithHandler((request, ct) =>
                    Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                    }))
                .WithRequestFactory(fakeRequestFactory)
                .Build();

            // Act
            var result = await service.GenerateTestsAsync("prompt");

            // Assert
            Assert.Equal("//Extracted code", result);

            // Verify that the HttpRequestMessage was created with the proper Authorization header.
            Assert.NotNull(fakeRequestFactory.LastRequest);
            Assert.Equal("Bearer", fakeRequestFactory.LastRequest.Headers.Authorization.Scheme);
            Assert.Equal("key", fakeRequestFactory.LastRequest.Headers.Authorization.Parameter);
        }

        [Fact]
        public async Task GenerateTestsAsync_Ollama_Success_WithoutCodeBlock_ReturnsCleanedResponse()
        {
            // Arrange: Fake Ollama response without a code block, but with <think> tags.
            var responseText = "Response with <think>to remove</think> extra text.";
            var responseContent = JsonSerializer.Serialize(new { response = responseText });

            var service = new AIApiServiceTestBuilder()
                .WithSettings(new AISettings
                {
                    ApiKey = "key",
                    Provider = AIProvider.Ollama,
                    Endpoint = "http://test",
                    Model = "model",
                    MaxTokens = 100,
                    Temperature = 0.5
                })
                .WithHandler((request, ct) =>
                    Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                    }))
                .Build();

            // Act
            var result = await service.GenerateTestsAsync("prompt");

            // Assert: <think> content should be removed.
            Assert.Equal("Response with  extra text.", result);
        }

        [Fact]
        public async Task GenerateTestsAsync_Ollama_Failure_ReturnsEmpty()
        {
            // Arrange: Simulate a failure response from Ollama.
            var service = new AIApiServiceTestBuilder()
                .WithSettings(new AISettings
                {
                    ApiKey = "key",
                    Provider = AIProvider.Ollama,
                    Endpoint = "http://test",
                    Model = "model",
                    MaxTokens = 100,
                    Temperature = 0.5
                })
                .WithHandler((request, ct) =>
                    Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)))
                .Build();

            // Act
            var result = await service.GenerateTestsAsync("prompt");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        #endregion
    }
}