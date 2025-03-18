namespace AIUnitTestWriter.Wrappers
{
    public interface IHttpClientWrapper
    {
        /// <summary>
        /// Send a HTTP request message
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
    }
}
