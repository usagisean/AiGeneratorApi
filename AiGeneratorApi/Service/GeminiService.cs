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

namespace AiGeneratorApi.Service;

public class GeminiService : IAIService
{
    private readonly GeminiSettings _config;
    // 1. 新增：这是为了获取我们在 Program.cs 里配好的 HTTP 客户端
    private readonly IHttpClientFactory _httpClientFactory;

    // 2. 修改构造函数：注入 IHttpClientFactory
    public GeminiService(IHttpClientFactory httpClientFactory, IOptions<AIConfig> config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config.Value.Gemini;
    }

    public async Task<string> GenerateContentAsync(GenerateRequest request)
    {
        // 1. 确定要使用的模型 ID
        var targetModelId = !string.IsNullOrEmpty(request.ModelName)
            ? request.ModelName
            : _config.DefaultModelId;

        // 2. 创建客户端
        var client = CreateClient();

        // 3. 拼接全路径
        var modelResourceName = $"projects/{_config.ProjectId}/locations/{_config.Location}/publishers/google/models/{targetModelId}";

        // 4. 构建请求
        var req = new GenerateContentRequest
        {
            Model = modelResourceName,
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
            return fullText;
        }
        catch (RpcException e)
        {
            throw new Exception($"Google API Error: {e.Status.Detail} (Code: {e.StatusCode})");
        }
    }

    private PredictionServiceClient CreateClient()
    {
        // =================================================================
        // 核心修改：移除这里所有的代理判断逻辑！
        // 直接向工厂要一个 "GeminiClient"，它已经根据 Program.cs 的逻辑
        // 自动配置好了（本地走代理，VPS直连）。
        // =================================================================
        var httpClient = _httpClientFactory.CreateClient("GeminiClient");

        if (!File.Exists(_config.KeyFilePath))
            throw new FileNotFoundException($"密钥文件未找到: {_config.KeyFilePath}");

        // 加载凭证
        var credential = GoogleCredential.FromFile(_config.KeyFilePath)
            .CreateScoped(PredictionServiceClient.DefaultScopes);

        // 创建 gRPC 通道
        // 注意：这里把 HttpClient 传给 GrpcChannel，它就会使用我们在 Program.cs 里配好的代理规则
        var channel = GrpcChannel.ForAddress($"https://{_config.Location}-aiplatform.googleapis.com", new GrpcChannelOptions
        {
            HttpClient = httpClient, // <--- 关键点：使用工厂创建的 Client
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
        // 暂时返回硬编码列表，或者以后去读 Google SDK
        return Task.FromResult(new List<string> 
        { 
            "gemini-2.0-flash-exp", 
            "gemini-1.5-pro", 
            "gemini-1.5-flash" 
        });
    }
}