using Google.Cloud.AIPlatform.V1; // 如果你要用 gemini-3-preview，建议改为 using Google.Cloud.AIPlatform.V1Beta1;
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

    public GeminiService(IOptions<AIConfig> config)
    {
        _config = config.Value.Gemini;
    }

    public async Task<string> GenerateContentAsync(GenerateRequest request)
    {
        // 1. 确定要使用的模型 ID
        // 如果前端传了 ModelName，就用前端的；否则用配置文件里的默认值
        var targetModelId = !string.IsNullOrEmpty(request.ModelName)
            ? request.ModelName
            : _config.DefaultModelId;

        // 2. 创建客户端 (使用 Builder 模式修复报错)
        var client = CreateClient();

        // 3. 拼接全路径
        // 格式: projects/{project}/locations/{location}/publishers/google/models/{model}
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
            // 可选：设置一些默认参数
            GenerationConfig = new GenerationConfig { Temperature = 0.7f, MaxOutputTokens = 8000 }
        };

        try
        {
            var resp = await client.GenerateContentAsync(req);

            // 拼接返回结果
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
        // A. 配置代理
        var handler = new HttpClientHandler();
        // 【关键逻辑】只有当配置文件里配了 ProxyUrl 时，才启用代理
        // 在美国 VPS 上，ProxyUrl 留空，就不会走下面这段，直接直连 Google
        if (!string.IsNullOrEmpty(_config.ProxyUrl))
        {
            handler.Proxy = new System.Net.WebProxy(_config.ProxyUrl);
            handler.UseProxy = true;
            // 只有走代理时才需要忽略证书错误
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        if (!File.Exists(_config.KeyFilePath))
            throw new FileNotFoundException($"密钥文件未找到: {_config.KeyFilePath}");

        // B. 加载凭证
        var credential = GoogleCredential.FromFile(_config.KeyFilePath)
            .CreateScoped(PredictionServiceClient.DefaultScopes);

        // C. 创建 gRPC 通道
        var channel = GrpcChannel.ForAddress($"https://{_config.Location}-aiplatform.googleapis.com", new GrpcChannelOptions
        {
            HttpHandler = handler,
            Credentials = ChannelCredentials.Create(ChannelCredentials.SecureSsl, credential.ToCallCredentials())
        });

        // D. 【关键修复】使用 Builder 模式创建客户端
        // 这比直接调用 .Create() 更兼容，绝对不会报 CS1501 错误
        var builder = new PredictionServiceClientBuilder
        {
            CallInvoker = channel.CreateCallInvoker()
        };

        return builder.Build();
    }
}