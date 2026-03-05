using Google.Cloud.AIPlatform.V1; 
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Grpc.Core;
using Google.Apis.Auth.OAuth2;
using Grpc.Auth;
using Microsoft.Extensions.Options;
using AiGeneratorApi.Interface;
using AiGeneratorApi.Model;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AiGeneratorApi.Service;

public class GeminiService : IAIService
{
    private readonly GeminiSettings _config;
    private readonly IHttpClientFactory _httpClientFactory;

    // NOTE: 按优先级排列的 Gemini 可靠 fallback 模型，客户端传入的模型失败时依次尝试
    private static readonly string[] FALLBACK_MODELS = 
    { 
        "gemini-2.0-flash-001",
        "gemini-1.5-flash"
    };

    public GeminiService(IHttpClientFactory httpClientFactory, IOptions<AIConfig> config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config.Value.Gemini;
    }

    public async Task<GenerateResult> GenerateContentAsync(GenerateRequest request)
    {
        var requestedModel = !string.IsNullOrEmpty(request.ModelName) ? request.ModelName 
                           : !string.IsNullOrEmpty(_config.DefaultModelId) ? _config.DefaultModelId 
                           : "gemini-2.0-flash-001";

        // 根据模式构建不同的提示词
        string finalPrompt = request.IsHtml ? BuildArticlePrompt(request) : request.Prompt;

        // 先尝试客户端指定的模型
        try
        {
            var result = await ExecuteGeminiRequestAsync(finalPrompt, requestedModel, request.IsHtml);
            result.ActualModel = requestedModel;
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GeminiService] 模型 [{requestedModel}] 失败: {ex.Message} -> 进入 Fallback 策略");
        }

        // NOTE: Fallback 策略：依次尝试可靠模型列表
        foreach (var fallbackModel in FALLBACK_MODELS)
        {
            // 避免重复尝试已失败的同名模型
            if (fallbackModel.Equals(requestedModel, StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                Console.WriteLine($"[Fallback] Gemini 尝试模型: {fallbackModel}");
                var result = await ExecuteGeminiRequestAsync(finalPrompt, fallbackModel, request.IsHtml);
                result.ActualModel = $"{fallbackModel}(fallback from {requestedModel})";
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Fallback] Gemini/{fallbackModel} 失败: {ex.Message}");
            }
        }

        throw new Exception($"所有可用 Gemini 模型均已尝试失败，请求的模型为: {requestedModel}");
    }

    public Task<List<string>> GetModelsAsync()
    {
        return Task.FromResult(new List<string> { "gemini-2.0-flash-exp", "gemini-1.5-pro", "gemini-1.5-flash" });
    }

    /// <summary>
    /// 封装单次 Gemini API 调用
    /// </summary>
    private async Task<GenerateResult> ExecuteGeminiRequestAsync(string prompt, string modelId, bool isHtml)
    {
        var channel = CreateGrpcChannel();
        var client = new PredictionServiceClientBuilder { CallInvoker = channel.CreateCallInvoker() }.Build();
        var modelResourceName = $"projects/{_config.ProjectId}/locations/{_config.Location}/publishers/google/models/{modelId}";

        string systemText = isHtml 
            ? "你是一个 SEO 专家和内容生成器。请严格按照用户要求的 JSON 格式输出，不要包含任何 Markdown 标记。" 
            : "你是一个 AI 助手。";

        var req = new GenerateContentRequest
        {
            Model = modelResourceName,
            SystemInstruction = new Content { Parts = { new Part { Text = systemText } } },
            Contents = { new Content { Role = "user", Parts = { new Part { Text = prompt } } } },
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
            return ParseAiResponse(fullText, isHtml);
        }
        catch (RpcException e)
        {
            throw new Exception($"Google API Error: {e.Status.Detail}");
        }
    }

    /// <summary>
    /// 构建带有风格和字数要求的结构化 JSON 文章提示词
    /// </summary>
    private string BuildArticlePrompt(GenerateRequest request)
    {
        var role = request.Style.GetRoleDescription();
        var instruction = request.Style.GetWritingInstruction();

        var wordCountHint = request.WordCount > 0
            ? $"文章正文目标字数约 {request.WordCount} 字。"
            : "文章长度不限，根据话题深度自行把握。";

        var languageHint = request.Language switch
        {
            "en" => "请用英文写作。",
            "ja" => "请用日文写作。",
            _ => "请用简体中文写作。"
        };

        return $@"
你是一名{role}兼 SEO 专家。请根据我提供的【话题】，写一篇结构完整的文章。

【话题】：
{request.Prompt}

【写作风格要求】：
{instruction}

【基本要求】：
1. 标题：拟一个吸引人的、适合 SEO 的文章标题（纯文本，不带 HTML 标签）。
2. 正文格式：为了确保排版丰富美观，请随机搭配使用丰富的 HTML 标签（如 <h2>, <h3>, <h4>, <p>, <ul>, <li>, <blockquote>, <strong> 等），切忌只用单一标签排版。绝对不要包含 <h1> 标签。
3. 样式要求：务必在生成的所有 HTML 标签中加入随机、多样且美观的内联 CSS 样式 `style='...'`。可以随机调整文字颜色、背景色、边距（margin/padding）、边框（border）、行高（line-height）、圆角（border-radius）等现代设计常用属性。
4. 关键词：提取 5-8 个适合 SEO 的关键词，用英文逗号分隔。
5. 摘要：写一段 120-150 字的文章摘要，适合用作 meta description。
6. {wordCountHint}
7. {languageHint}

【输出格式】：严格按照以下 JSON 格式输出，不要添加任何 Markdown 代码块标记：
{{
  ""title"": ""文章标题"",
  ""content"": ""包含丰富内联样式的 HTML 正文内容"",
  ""keywords"": ""关键词1,关键词2,关键词3"",
  ""description"": ""文章摘要""
}}";
    }

    /// <summary>
    /// 解析 AI 返回内容为结构化结果
    /// </summary>
    private GenerateResult ParseAiResponse(string rawContent, bool isHtmlMode)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
            return new GenerateResult();

        if (!isHtmlMode)
        {
            return new GenerateResult { Content = rawContent };
        }

        try
        {
            var cleaned = Regex.Replace(rawContent, @"```[a-zA-Z]*", "", RegexOptions.IgnoreCase)
                               .Replace("```", "").Trim();

            var jsonStart = cleaned.IndexOf('{');
            var jsonEnd = cleaned.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                cleaned = cleaned.Substring(jsonStart, jsonEnd - jsonStart + 1);
            }

            var jsonNode = JsonNode.Parse(cleaned);
            if (jsonNode != null)
            {
                var result = new GenerateResult
                {
                    Title = jsonNode["title"]?.ToString() ?? "",
                    Content = jsonNode["content"]?.ToString() ?? "",
                    Keywords = jsonNode["keywords"]?.ToString() ?? "",
                    Description = jsonNode["description"]?.ToString() ?? ""
                };

                result.Content = CleanHtmlContent(result.Content);
                return result;
            }
        }
        catch (JsonException)
        {
            Console.WriteLine("[Gemini Warning] AI 返回的不是有效 JSON，回退到纯文本模式");
        }

        return new GenerateResult
        {
            Content = CleanHtmlContent(rawContent)
        };
    }

    /// <summary>
    /// 清洗 HTML 内容
    /// </summary>
    private string CleanHtmlContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;

        content = Regex.Replace(content, @"```[a-zA-Z]*", "", RegexOptions.IgnoreCase);
        content = content.Replace("```", "").Trim();

        bool hasHtmlTags = Regex.IsMatch(content, @"<[a-z][\s\S]*>", RegexOptions.IgnoreCase);
        if (!hasHtmlTags)
        {
            var processed = content.Replace("\n\n", "</p><p>").Replace("\n", "<br/>");
            content = $"<div class=\"gemini-generated\"><p>{processed}</p></div>";
        }

        content = content.Replace("\r", "").Replace("\n", "");
        return content;
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
}