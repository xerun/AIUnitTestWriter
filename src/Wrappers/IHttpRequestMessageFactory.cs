namespace AIUnitTestWriter.Wrappers
{
    public interface IHttpRequestMessageFactory
    {
        HttpRequestMessage Create(HttpMethod method, string requestUri, HttpContent content, string apiKey);
    }
}
