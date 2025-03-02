namespace AIUnitTestWriter.SettingOptions
{
    public class AISettings
    {
        public string Provider { get; set; } = null!;
        public string ApiKey { get; set; } = null!;
        public string Endpoint { get; set; } = null!;
        public string Model { get; set; } = null!;
        public int MaxTokens { get; set; }
        public double Temperature { get; set; }
    }
}
