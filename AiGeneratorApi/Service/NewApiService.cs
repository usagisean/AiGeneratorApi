using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions; // 引入正则
using AiGeneratorApi.Interface;
using AiGeneratorApi.Model;
using Microsoft.Extensions.Options;

namespace AiGeneratorApi.Service;

public class NewApiService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly NewApiSettings _config;

    public NewApiService(IHttpClientFactory httpClientFactory, IOptions<AIConfig> config)
    {
        _httpClient = httpClientFactory.CreateClient();
        _config = config.Value.NewApi;

        // 保持之前的伪装
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

   public async Task<string> GenerateContentAsync(GenerateRequest request)
    {
        var model = !string.IsNullOrEmpty(request.ModelName) ? request.ModelName : _config.DefaultModelId;
        string rawContent = "";

        // =========================================================
        // 核心修改：在这里拼接提示词！
        // 如果是 HTML 模式，说明是要写文章，我们把用户输入的简单标题包装一下
        // =========================================================
        string finalPrompt = request.Prompt;
        
        if (request.IsHtml)
        {
            finalPrompt = BuildArticlePrompt(request.Prompt);
        }

        // 优先尝试免费通道
        if (!string.IsNullOrEmpty(_config.FreeApiKey))
        {
            try
            {
                // 注意：这里传入的是 finalPrompt (包装后的)
                rawContent = await ExecuteRequestAsync(finalPrompt, model, _config.FreeApiKey, "Free", request.IsHtml);
                return CleanAiResponse(rawContent, request.IsHtml);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmartRoute] 免费通道异常: {ex.Message} -> 切换 VIP");
            }
        }

        // VIP 通道兜底
        rawContent = await ExecuteRequestAsync(finalPrompt, model, _config.VipApiKey, "VIP", request.IsHtml);
        return CleanAiResponse(rawContent, request.IsHtml);
    }

    // 私有方法：构建文章提示词模版
    private string BuildArticlePrompt(string topic)
    {
        return $@"
你是一名资深的新闻时评人。请根据我提供的【热点话题】，写一篇结构完整的深度评论文章。

【热点话题】：
{topic}

【写作要求】：
1. 拟定标题：根据话题自拟一个吸引人的标题，必须用 <h1> 标签包裹。
2. 内容结构：
   - 开篇：简述该话题反映的社会现象或背景。
   - 分析：从社会、法律、人性等角度深度剖析（3-4段）。
   - 结尾：总结全文，升华主题。
3. 格式严格要求：
   - 只输出 HTML 代码片段。
   - 严禁 使用 Markdown 代码块标记（不要写 ```html）。
   - 正文段落必须用 <p> 标签包裹。
   - 重点金句可以用 <strong> 加粗。
";
    }

    private async Task<string> ExecuteRequestAsync(string prompt, string model, string apiKey, string channel, bool isHtml)
    {
        if(string.IsNullOrEmpty(apiKey)) throw new Exception($"{channel} Key 未配置");

        var url = $"{_config.BaseUrl.TrimEnd('/')}/v1/chat/completions";

        string systemInstruction = isHtml 
            ? "你是一个 HTML 生成器。只输出纯净的 HTML 代码，不要包含 Markdown 标记。" 
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

    private string CleanAiResponse(string content, bool isHtmlMode)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;

        // 去掉 Markdown
        content = Regex.Replace(content, @"```[a-zA-Z]*", "", RegexOptions.IgnoreCase);
        content = content.Replace("```", "").Trim();

        if (isHtmlMode)
        {
            // 兜底：如果没有标签，手动加
            bool hasHtmlTags = Regex.IsMatch(content, @"<[a-z][\s\S]*>", RegexOptions.IgnoreCase);
            if (!hasHtmlTags)
            {
                var processed = content.Replace("\n\n", "</p><p>").Replace("\n", "<br/>");
                content = $"<div class=\"ai-generated\"><p>{processed}</p></div>";
            }
            // 暴力去换行
            content = content.Replace("\r", "").Replace("\n", "");
        }
        return content;
    }

    // GetModelsAsync 保持你原有的逻辑不变
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
            Console.WriteLine($"[NewApi Warning] {channelName} 获取模型失败: {ex.Message}");
            return new List<string>();
        }
    }
}