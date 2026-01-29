namespace AiGeneratorApi.Model
{
    public class GeminiSettings
    {
        public string ProjectId { get; set; }
        public string Location { get; set; }
        public string DefaultModelId { get; set; } // 注意这里改名叫 DefaultModelId 比较贴切
        public string KeyFilePath { get; set; }
        public string ProxyUrl { get; set; }
    }
}
