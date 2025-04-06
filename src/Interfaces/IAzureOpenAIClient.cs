namespace AIUnitTestWriter.Interfaces
{
    public interface IAzureOpenAIClient
    {
        /// <summary>
        /// Get a chat client for the specified model.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        IChatClient GetChatClient(string model);
    }
}
