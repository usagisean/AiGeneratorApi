using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AiGeneratorApi.Interface;
using AiGeneratorApi.Model;
using Microsoft.Extensions.Options;

namespace AiGeneratorApi.Service;

public class NewApiService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly NewApiSettings _config;

    // NOTE: 已知不支持文字生成的模型黑名单（图片、音频、推理等特殊模型）
    private static readonly HashSet<string> NON_TEXT_MODELS = new(StringComparer.OrdinalIgnoreCase)
    {
        "dall-e-3", "dall-e-2",
        "sora",
        "tts-1", "tts-1-hd",
        "whisper-1",
    };

    // NOTE: 按优先级排列的可靠 fallback 模型，依次尝试直到成功
    private static readonly string[] FALLBACK_MODELS = { "gpt-4o-mini", "gpt-4o", "deepseek-chat" };

    public NewApiService(IHttpClientFactory httpClientFactory, IOptions<AIConfig> config)
    {
        _httpClient = httpClientFactory.CreateClient();
        _config = config.Value.NewApi;

        // 保持之前的伪装
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    public async Task<GenerateResult> GenerateContentAsync(GenerateRequest request)
    {
        // 三层兜底：请求指定 → 配置默认 → 硬编码默认（NewApi 用 gpt-4o-mini）
        var requestedModel = !string.IsNullOrEmpty(request.ModelName) ? request.ModelName
                           : !string.IsNullOrEmpty(_config.DefaultModelId) ? _config.DefaultModelId
                           : "gpt-4o-mini";

        // 根据模式构建不同的提示词
        string finalPrompt = request.IsHtml ? BuildArticlePrompt(request) : request.Prompt;

        // NOTE: 如果请求的是非文字模型，直接跳过，使用 fallback 策略，避免无谓的 API 调用
        bool skipRequested = NON_TEXT_MODELS.Contains(requestedModel);
        if (!skipRequested)
        {
            // 优先尝试免费通道 + 指定模型
            if (!string.IsNullOrEmpty(_config.FreeApiKey))
            {
                try
                {
                    var rawContent = await ExecuteRequestAsync(finalPrompt, requestedModel, _config.FreeApiKey, "Free", request.IsHtml);
                    var result = ParseAiResponse(rawContent, request.IsHtml);
                    result.ActualModel = requestedModel;
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SmartRoute] 免费通道/{requestedModel} 异常: {ex.Message} -> 切换 VIP");
                }
            }

            // VIP 通道 + 指定模型
            try
            {
                var rawContent = await ExecuteRequestAsync(finalPrompt, requestedModel, _config.VipApiKey, "VIP", request.IsHtml);
                var result = ParseAiResponse(rawContent, request.IsHtml);
                result.ActualModel = requestedModel;
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmartRoute] VIP/{requestedModel} 异常: {ex.Message} -> 进入 Fallback 策略");
            }
        }
        else
        {
            Console.WriteLine($"[SmartRoute] 模型 [{requestedModel}] 为非文字模型，跳过，直接进入 Fallback 策略");
        }

        // NOTE: Fallback 策略：依次尝试可靠模型列表，直到有一个成功
        foreach (var fallbackModel in FALLBACK_MODELS)
        {
            // 免费通道 + fallback 模型
            if (!string.IsNullOrEmpty(_config.FreeApiKey))
            {
                try
                {
                    Console.WriteLine($"[Fallback] 尝试免费通道/{fallbackModel}");
                    var rawContent = await ExecuteRequestAsync(finalPrompt, fallbackModel, _config.FreeApiKey, "Free-Fallback", request.IsHtml);
                    var result = ParseAiResponse(rawContent, request.IsHtml);
                    // 标记实际使用的 fallback 模型，便于调用方日志追踪
                    result.ActualModel = $"{fallbackModel}(fallback from {requestedModel})";
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Fallback] 免费通道/{fallbackModel} 失败: {ex.Message}");
                }
            }

            // VIP 通道 + fallback 模型
            try
            {
                Console.WriteLine($"[Fallback] 尝试 VIP 通道/{fallbackModel}");
                var rawContent = await ExecuteRequestAsync(finalPrompt, fallbackModel, _config.VipApiKey, "VIP-Fallback", request.IsHtml);
                var result = ParseAiResponse(rawContent, request.IsHtml);
                result.ActualModel = $"{fallbackModel}(fallback from {requestedModel})";
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Fallback] VIP/{fallbackModel} 失败: {ex.Message}");
            }
        }

        // 所有 fallback 都耗尽，抛出异常让 Controller 返回 500
        throw new Exception($"所有可用模型均已尝试失败，请求的模型为: {requestedModel}");
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
    /// HTML 模式：解析 JSON 结构；普通模式：仅返回 content
    /// </summary>
    private GenerateResult ParseAiResponse(string rawContent, bool isHtmlMode)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
            return new GenerateResult();

        // 非 HTML 模式（普通聊天），直接返回原文
        if (!isHtmlMode)
        {
            return new GenerateResult { Content = rawContent };
        }

        // HTML 模式：尝试从 AI 响应中提取 JSON
        try
        {
            // 清理可能的 Markdown 代码块标记
            var cleaned = Regex.Replace(rawContent, @"```[a-zA-Z]*", "", RegexOptions.IgnoreCase)
                               .Replace("```", "").Trim();

            // 尝试定位 JSON 对象的边界（AI 可能在 JSON 前后加了多余文字）
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

                // 对 content 执行 HTML 清洗
                result.Content = CleanHtmlContent(result.Content);
                return result;
            }
        }
        catch (JsonException)
        {
            // JSON 解析失败，回退到旧逻辑：整段当作 content
            Console.WriteLine("[NewApi Warning] AI 返回的不是有效 JSON，回退到纯文本模式");
        }

        // 回退：解析失败时把整个响应当作 content
        return new GenerateResult
        {
            Content = CleanHtmlContent(rawContent)
        };
    }

    /// <summary>
    /// 清洗 HTML 内容：去除 Markdown 残留、确保有 HTML 标签、去除换行
    /// </summary>
    private string CleanHtmlContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;

        // 去掉 Markdown 代码块标记
        content = Regex.Replace(content, @"```[a-zA-Z]*", "", RegexOptions.IgnoreCase);
        content = content.Replace("```", "").Trim();

        // 兜底：如果没有 HTML 标签，手动包裹
        bool hasHtmlTags = Regex.IsMatch(content, @"<[a-z][\s\S]*>", RegexOptions.IgnoreCase);
        if (!hasHtmlTags)
        {
            var processed = content.Replace("\n\n", "</p><p>").Replace("\n", "<br/>");
            content = $"<div class=\"ai-generated\"><p>{processed}</p></div>";
        }

        // 去除换行符，保持单行 HTML
        content = content.Replace("\r", "").Replace("\n", "");
        return content;
    }

    private async Task<string> ExecuteRequestAsync(string prompt, string model, string apiKey, string channel, bool isHtml)
    {
        if(string.IsNullOrEmpty(apiKey)) throw new Exception($"{channel} Key 未配置");

        var url = $"{_config.BaseUrl.TrimEnd('/')}/v1/chat/completions";

        // HTML 模式要求返回 JSON，所以 system 指令也相应调整
        string systemInstruction = isHtml 
            ? "你是一个 SEO 专家和内容生成器。请严格按照用户要求的 JSON 格式输出，不要包含任何 Markdown 标记。" 
            : "你是一个 AI 助手。";

        var requestBody = new
        {
            model = model,
            messages = new[] 
            { 
                new { role = "system", content = systemInstruction },
                new { role = "user", content = prompt } 
            },
            temperature = 0.7
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(httpRequest);
        
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            throw new Exception($"HTTP {response.StatusCode} - {err}");
        }

        var jsonString = await response.Content.ReadAsStringAsync();
        try 
        {
            var jsonNode = JsonNode.Parse(jsonString);
            return jsonNode?["choices"]?[0]?["message"]?["content"]?.ToString() ?? "";
        }
        catch 
        { 
            return jsonString; 
        }
    }

    // GetModelsAsync 保持原有逻辑不变
    public async Task<List<string>> GetModelsAsync()
    {
        var tasks = new List<Task<List<string>>>();

        if (!string.IsNullOrEmpty(_config.FreeApiKey)) 
            tasks.Add(FetchModelsByKeyAsync(_config.FreeApiKey, "Free"));
        
        if (!string.IsNullOrEmpty(_config.VipApiKey)) 
            tasks.Add(FetchModelsByKeyAsync(_config.VipApiKey, "VIP"));

        await Task.WhenAll(tasks);

        var allModels = new HashSet<string>();
        foreach (var task in tasks)
        {
            foreach (var model in task.Result) allModels.Add(model);
        }

        if (allModels.Count == 0) return new List<string> { _config.DefaultModelId };
        
        return allModels.OrderBy(x => x).ToList();
    }

    private async Task<List<string>> FetchModelsByKeyAsync(string apiKey, string channelName)
    {
        var url = $"{_config.BaseUrl.TrimEnd('/')}/v1/models";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.SendAsync(request, cts.Token);
            response.EnsureSuccessStatusCode();
            
            var jsonString = await response.Content.ReadAsStringAsync();
            var jsonNode = JsonNode.Parse(jsonString);
            var list = new List<string>();

            if (jsonNode?["data"] is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    var id = item?["id"]?.ToString();
                    if (!string.IsNullOrEmpty(id)) list.Add(id);
                }
            }
            return list;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NewApi Warning] {channelName} 获取模型列表失败: {ex.Message}");
            return new List<string>();
        }
    }
}