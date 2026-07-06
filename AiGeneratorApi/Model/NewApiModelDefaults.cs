namespace AiGeneratorApi.Model;

/// <summary>
/// NewAPI / LiteLLM 后端的推荐默认模型集中定义。
/// appsettings.json 可以覆盖这些值；这里作为缺省兜底，避免配置缺失时回到旧模型。
/// </summary>
public static class NewApiModelDefaults
{
    public const string DefaultBaseUrl = "https://api.zxaihub.com";
    public const string DefaultModelId = "z-ai/glm-5.2";

    public static readonly string[] KnownModels =
    [
        "z-ai/glm-5.2",
        "deepseek-ai/deepseek-v4-flash",
        "deepseek-ai/deepseek-v4-pro",
        "qwen/qwen3.5-122b-a10b",
        "qwen/qwen3.5-397b-a17b",
        "moonshotai/kimi-k2.6",
        "minimaxai/minimax-m3",
        "nvidia/nemotron-3-ultra-550b-a55b",
        "nvidia/nemotron-3-super-120b-a12b",
        "openai/gpt-oss-120b",
        "openai/gpt-oss-20b"
    ];

    public static readonly string[] FallbackModels =
    [
        "z-ai/glm-5.2",
        "deepseek-ai/deepseek-v4-flash",
        "qwen/qwen3.5-122b-a10b",
        "moonshotai/kimi-k2.6",
        "openai/gpt-oss-120b"
    ];
}
