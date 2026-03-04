using AiGeneratorApi.Interface;
using AiGeneratorApi.Model;

namespace AiGeneratorApi.Service
{
    public class OpenAIService : IAIService
    {

        public async Task<GenerateResult> GenerateContentAsync(GenerateRequest request)
        {
            // 占位实现，返回结构化结果
            return new GenerateResult
            {
                Title = "OpenAI 测试标题",
                Content = $"<p>来自 OpenAI 的回复: {request.Prompt}</p>",
                Keywords = "OpenAI,测试",
                Description = "这是一个 OpenAI 占位实现的测试响应"
            };
        }
        public Task<List<string>> GetModelsAsync()
        {
            return Task.FromResult(new List<string> 
            { 
                "gpt-4o"
            });
        }
    }
}
