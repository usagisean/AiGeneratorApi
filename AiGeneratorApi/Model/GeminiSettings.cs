namespace AiGeneratorApi.Model
{
    public class GeminiSettings
    {
        public bool Enabled { get; set; } = false;
        public string ProjectId { get; set; } = string.Empty;
        public string Location { get; set; } = "us-central1";
        public string DefaultModelId { get; set; } = "gemini-2.5-flash"; // 注意这里改名叫 DefaultModelId 比较贴切
        public string KeyFilePath { get; set; } = string.Empty;
        public string ProxyUrl { get; set; } = string.Empty;
    }
}
