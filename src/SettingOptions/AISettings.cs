using AIUnitTestWriter.Enum;

namespace AIUnitTestWriter.SettingOptions
{
    public class AISettings
    {
        public string Prompt { get; set; } = null!;
        public AIProvider Provider { get; set; }
        public string ApiKey { get; set; } = null!;
        public string Endpoint { get; set; } = null!;
        public string Model { get; set; } = null!;
        public int MaxTokens { get; set; }
        public double Temperature { get; set; }
        public bool PreviewResult { get; set; }
    }
}
