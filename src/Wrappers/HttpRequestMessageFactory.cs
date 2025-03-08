using System.Net.Http.Headers;

namespace AIUnitTestWriter.Wrappers
{
    public class HttpRequestMessageFactory : IHttpRequestMessageFactory
    {
        public HttpRequestMessage Create(HttpMethod method, string requestUri, HttpContent content, string apiKey)
        {
            var request = new HttpRequestMessage(method, requestUri)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            return request;
        }
    }
}
