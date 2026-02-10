using Google.Api;

namespace AiGeneratorApi.Model
{
    public class AIConfig
    {
        public GeminiSettings Gemini { get; set; }
        public NewApiSettings NewApi { get; set; }
    }
}
