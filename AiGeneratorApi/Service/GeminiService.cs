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
    /// 构建文章生成提示词
    /// NOTE: 提示词针对蜘蛛池场景设计，要求 AI 以真人视角写作，完全避免机器写作痕迹
    /// </summary>
    private string BuildArticlePrompt(GenerateRequest request)
    {
        var role = request.Style.GetRoleDescription();
        var instruction = request.Style.GetWritingInstruction();

        var wordCountHint = request.WordCount > 0
            ? $"字数目标约 {request.WordCount} 字，可适当浮动。"
            : "字数根据话题深度自然把握，不做硬性限制。";

        var languageHint = request.Language switch
        {
            "en" => "Write in English.",
            "ja" => "日本語で書いてください。",
            _ => "使用简体中文写作。"
        };

        return $@"
你是一位{role}，正在为自己的个人博客撰写一篇文章。

话题：{request.Prompt}

写作要求：
{instruction}
{wordCountHint}
{languageHint}

正文 HTML 格式规范（严格遵守）：
- 正文由多个独立区块组成，每个区块用 <div> 包裹，不同区块可以有不同的背景色或边框风格
- 每个 <div> 区块内部，灵活使用 <h2>、<h3>、<h4>、<p>、<ul>、<li>、<blockquote>、<strong>、<em> 等标签组织内容
- 不同标签之间自然穿插，富有层次感，绝不只使用单一标签堆砌
- 每个 HTML 标签都必须附带随机的 style 属性，风格各异，例如调整颜色、字号、边距、行高、圆角、背景色等
- 禁止使用 <h1> 标签

内容红线（违反则重写）：
- 文章是真人写的博客，绝对不能出现任何暗示机器生成或营销目的的词汇，包括但不限于：SEO、关键词、搜索引擎、元描述、锚文本、外链、收录、蜘蛛、爬虫、流量、变现、营销
- 不要在文章任何位置列出关键词清单
- 不要用「本文」「本篇」「笔者」等刻板开头，直接讲内容
- 摘要只填充一段自然的引言，读起来像文章开篇第一段
- 必须基于生成的文章内容，补充 4 条逼真、有趣、符合读者视角的评论

严格按照以下 JSON 格式输出（禁止添加任何 Markdown 标记或额外说明）：
{{
  ""title"": ""吸引人的文章标题（纯文本）"",
  ""content"": ""由多个 <div> 区块组成的完整 HTML 正文（每个标签都有随机 style）"",
  ""keywords"": ""5到8个自然词汇（逗号分隔，仅供内部使用，不出现在文章中）"",
  ""description"": ""100到150字的引言式摘要，读起来像文章第一段"",
  ""comments"": [""结合文章内容的有趣用户评论1"", ""评论2"", ""评论3"", ""评论4""]
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
                    Description = jsonNode["description"]?.ToString() ?? "",
                    Comments = jsonNode["comments"]?.AsArray().Select(n => n?.ToString() ?? "").ToList() ?? new List<string>()
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