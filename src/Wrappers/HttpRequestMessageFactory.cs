using System.Net.Http.Headers;

namespace AIUnitTestWriter.Wrappers
{
    public class HttpRequestMessageFactory : IHttpRequestMessageFactory
    {
        /// <inheritdoc/>
        public HttpRequestMessage Create(HttpMethod method, string url, HttpContent content, string apiKey)
        {
            var request = new HttpRequestMessage(method, url)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            return request;
        }
    }
}
