namespace AIUnitTestWriter.Interfaces
{
    public interface IHttpRequestMessageFactory
    {
        /// <summary>
        /// Create a new HttpRequestMessage
        /// </summary>
        /// <param name="method"></param>
        /// <param name="requestUri"></param>
        /// <param name="content"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        HttpRequestMessage Create(HttpMethod method, string requestUri, HttpContent content, string apiKey);
    }
}
