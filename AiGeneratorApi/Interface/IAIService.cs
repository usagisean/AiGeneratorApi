using AiGeneratorApi.Model;

namespace AiGeneratorApi.Interface
{
    public interface IAIService
    {
        // 统一接口：传入 Request，返回结构化的生成结果（标题/正文/关键词/摘要）
        Task<GenerateResult> GenerateContentAsync(GenerateRequest request);
        Task<List<string>> GetModelsAsync();
    }
}
