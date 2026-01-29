using AiGeneratorApi.Model;

namespace AiGeneratorApi.Interface
{
    public interface IAIService
    {
        // 统一接口：传入 Request，返回生成的文本
        Task<string> GenerateContentAsync(GenerateRequest request);
    }
}
