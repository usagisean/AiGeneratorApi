using AiGeneratorApi.Interface;
using AiGeneratorApi.Model;

namespace AiGeneratorApi.Service
{
    public class OpenAIService : IAIService
    {

        public async Task<string> GenerateContentAsync(GenerateRequest request)
        {
            return $"来自 OpenAI 的回复: {request.Prompt}";
        }
    }
}
