using Google.Cloud.AIPlatform.V1; 
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Grpc.Core;
using Google.Apis.Auth.OAuth2;
using Grpc.Auth;
using Microsoft.Extensions.Options;
using AiGeneratorApi.Interface;
using AiGeneratorApi.Model;
using System.Text.RegularExpressions;

namespace AiGeneratorApi.Service;

public class GeminiService : IAIService
{
    private readonly GeminiSettings _config;
    private readonly IHttpClientFactory _httpClientFactory;

    public GeminiService(IHttpClientFactory httpClientFactory, IOptions<AIConfig> config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config.Value.Gemini;
    }

    public async Task<string> GenerateContentAsync(GenerateRequest request)
    {
        var targetModelId = !string.IsNullOrEmpty(request.ModelName) ? request.ModelName : _config.DefaultModelId;
        var channel = CreateGrpcChannel();
        var client = new PredictionServiceClientBuilder { CallInvoker = channel.CreateCallInvoker() }.Build();
        var modelResourceName = $"projects/{_config.ProjectId}/locations/{_config.Location}/publishers/google/models/{targetModelId}";

        // =========================================================
        // 核心修改：在这里拼接提示词！
        // =========================================================
        string finalPrompt = request.Prompt;
        if (request.IsHtml)
        {
             finalPrompt = $@"
你是一名资深的新闻时评人。请根据我提供的【热点话题】，写一篇结构完整的深度评论文章。

【热点话题】：
{request.Prompt}

【写作要求】：
1. 拟定标题：根据话题自拟一个吸引人的标题，必须用 <h1> 标签包裹。
2. 内容结构：
   - 开篇：简述该话题反映的社会现象。
   - 分析：深度剖析（3-4段）。
   - 结尾：总结全文。
3. 格式严格要求：
   - 只输出 HTML 代码片段。
   - 严禁 使用 Markdown 代码块标记。
   - 正文段落必须用 <p> 标签包裹。
";
        }

        string systemText = request.IsHtml 
            ? "你是一个 HTML 生成器。只输出 HTML 代码，不要 Markdown。" 
            : "你是一个 AI 助手。";

        var req = new GenerateContentRequest
        {
            Model = modelResourceName,
            SystemInstruction = new Content { Parts = { new Part { Text = systemText } } },
            Contents = { new Content { Role = "user", Parts = { new Part { Text = finalPrompt } } } },
            GenerationConfig = new GenerationConfig { Temperature = 0.7f, MaxOutputTokens = 8000 }
        };

        try
        {
            var resp = await client.GenerateContentAsync(req);
            string fullText = "";
            if (resp.Candidates != null && resp.Candidates.Count > 0)
            {
                foreach (var part in resp.Candidates[0].Content.Parts) fullText += part.Text;
            }
            return CleanAiResponse(fullText, request.IsHtml);
        }
        catch (RpcException e)
        {
            throw new Exception($"Google API Error: {e.Status.Detail}");
        }
    }

    public Task<List<string>> GetModelsAsync()
    {
         // 简单直接的列表
        return Task.FromResult(new List<string> { "gemini-2.0-flash-exp", "gemini-1.5-pro", "gemini-1.5-flash" });
    }

    private GrpcChannel CreateGrpcChannel()
    {
        var httpClient = _httpClientFactory.CreateClient("GeminiClient");
        if (!File.Exists(_config.KeyFilePath)) throw new FileNotFoundException($"密钥文件未找到: {_config.KeyFilePath}");
        var credential = GoogleCredential.FromFile(_config.KeyFilePath).CreateScoped(PredictionServiceClient.DefaultScopes);
        return GrpcChannel.ForAddress($"https://{_config.Location}-aiplatform.googleapis.com", new GrpcChannelOptions
        {
            HttpClient = httpClient,
            Credentials = ChannelCredentials.Create(ChannelCredentials.SecureSsl, credential.ToCallCredentials())
        });
    }

    private string CleanAiResponse(string content, bool isHtmlMode)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;
        content = Regex.Replace(content, @"```[a-zA-Z]*", "", RegexOptions.IgnoreCase);
        content = content.Replace("```", "").Trim();

        if (isHtmlMode)
        {
            bool hasHtmlTags = Regex.IsMatch(content, @"<[a-z][\s\S]*>", RegexOptions.IgnoreCase);
            if (!hasHtmlTags)
            {
                var processed = content.Replace("\n\n", "</p><p>").Replace("\n", "<br/>");
                content = $"<div class=\"gemini-generated\"><p>{processed}</p></div>";
            }
            content = content.Replace("\r", "").Replace("\n", "");
        }
        return content;
    }
}