namespace AiGeneratorApi.Model;

public class GenerateRequest
{
    public string Prompt { get; set; } = string.Empty;

    // 前端可以传 "google", "openai", "baidu"
    public string Provider { get; set; } = "google";

    // 前端可以指定具体模型，例如 "gemini-1.5-pro-001"。如果不传，后端用默认的。
    public string? ModelName { get; set; }
}