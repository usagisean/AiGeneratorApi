using Google.Cloud.AIPlatform.V1; 
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Grpc.Core;
using Google.Apis.Auth.OAuth2;
using Grpc.Auth;
using Microsoft.Extensions.Options;
using AiGeneratorApi.Interface;
using AiGeneratorApi.Model;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions; // 引入正则

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
        // 1. 确定模型
        var targetModelId = !string.IsNullOrEmpty(request.ModelName)
            ? request.ModelName
            : _config.DefaultModelId;

        // 2. 创建客户端 (复用你的代理逻辑)
        var client = CreateClient();

        var modelResourceName = $"projects/{_config.ProjectId}/locations/{_config.Location}/publishers/google/models/{targetModelId}";

        // 3. 构建 System Instruction (系统人设)
        string systemText;
        if (request.IsHtml)
        {
            systemText = @"你是一个 HTML 生成器。只输出 HTML 代码，不包含 Markdown 标记，不包含解释性文字。";
        }
        else
        {
            systemText = "你是一个 AI 助手。";
        }

        // 4. 构建请求
        var req = new GenerateContentRequest
        {
            Model = modelResourceName,
            // Gemini 支持直接设置 SystemInstruction
            SystemInstruction = new Content
            {
                Parts = { new Part { Text = systemText } }
            },
            Contents =
            {
                new Content
                {
                    Role = "user",
                    Parts = { new Part { Text = request.Prompt } }
                }
            },
            GenerationConfig = new GenerationConfig { Temperature = 0.7f, MaxOutputTokens = 8000 }
        };

        try
        {
            var resp = await client.GenerateContentAsync(req);

            string fullText = "";
            if (resp.Candidates != null && resp.Candidates.Count > 0)
            {
                foreach (var part in resp.Candidates[0].Content.Parts)
                {
                    fullText += part.Text;
                }
            }

            // 5. 返回前进行清洗
            return CleanAiResponse(fullText, request.IsHtml);
        }
        catch (RpcException e)
        {
            throw new Exception($"Google API Error: {e.Status.Detail} (Code: {e.StatusCode})");
        }
    }

    /// <summary>
    /// 清洗 AI 返回的数据
    /// </summary>
    private string CleanAiResponse(string content, bool isHtmlMode)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;

        // 1. 去掉 Markdown
        content = Regex.Replace(content, @"```[a-zA-Z]*", "", RegexOptions.IgnoreCase);
        content = content.Replace("```", "").Trim();

        // 2. HTML 兜底转换
        if (isHtmlMode)
        {
            bool hasHtmlTags = Regex.IsMatch(content, @"<[a-z][\s\S]*>", RegexOptions.IgnoreCase);
            if (!hasHtmlTags)
            {
                var processed = content.Replace("\n\n", "</p><p>")
                                       .Replace("\r\n\r\n", "</p><p>")
                                       .Replace("\n", "<br/>");
                content = $"<div class=\"gemini-generated\"><p>{processed}</p></div>";
            }
        }
        return content;
    }

    private PredictionServiceClient CreateClient()
    {
        var httpClient = _httpClientFactory.CreateClient("GeminiClient");

        if (!File.Exists(_config.KeyFilePath))
            throw new FileNotFoundException($"密钥文件未找到: {_config.KeyFilePath}");

        var credential = GoogleCredential.FromFile(_config.KeyFilePath)
            .CreateScoped(PredictionServiceClient.DefaultScopes);

        var channel = GrpcChannel.ForAddress($"https://{_config.Location}-aiplatform.googleapis.com", new GrpcChannelOptions
        {
            HttpClient = httpClient, 
            Credentials = ChannelCredentials.Create(ChannelCredentials.SecureSsl, credential.ToCallCredentials())
        });

        var builder = new PredictionServiceClientBuilder
        {
            CallInvoker = channel.CreateCallInvoker()
        };

        return builder.Build();
    }

    public Task<List<string>> GetModelsAsync()
    {
        return Task.FromResult(new List<string> 
        { 
            "gemini-2.0-flash-exp", 
            "gemini-1.5-pro", 
            "gemini-1.5-flash" 
        });
    }
}