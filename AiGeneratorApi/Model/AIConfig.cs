namespace AiGeneratorApi.Model
{
    public class AIConfig
    {
        public GeminiSettings Gemini { get; set; } = new();
        public NewApiSettings NewApi { get; set; } = new();
    }
}
