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
        // 1. 确定模型
        var model = !string.IsNullOrEmpty(request.ModelName) ? request.ModelName : _config.DefaultModelId;

        string rawContent = "";

        // 2. 【智能路由核心逻辑】
        // 优先尝试免费通道
        if (!string.IsNullOrEmpty(_config.FreeApiKey))
        {
            try
            {
                // 注意：这里多传了 request.IsHtml 参数
                rawContent = await ExecuteRequestAsync(request.Prompt, model, _config.FreeApiKey, "Free", request.IsHtml);
                // 如果成功拿到内容，直接去清洗并返回
                return CleanAiResponse(rawContent, request.IsHtml);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmartRoute] 免费通道无法服务 '{model}': {ex.Message} -> 正在切换 VIP 通道兜底...");
            }
        }

        // 3. VIP 通道兜底
        rawContent = await ExecuteRequestAsync(request.Prompt, model, _config.VipApiKey, "VIP", request.IsHtml);
        
        // 4. 清洗并返回
        return CleanAiResponse(rawContent, request.IsHtml);
    }

    // 修改：增加了 isHtml 参数
    private async Task<string> ExecuteRequestAsync(string prompt, string model, string apiKey, string channel, bool isHtml)
    {
        if(string.IsNullOrEmpty(apiKey)) throw new Exception($"{channel} Key 未配置");

        var url = $"{_config.BaseUrl.TrimEnd('/')}/v1/chat/completions";

        // --- 动态构建 System Prompt ---
        string systemInstruction;
        if (isHtml)
        {
            systemInstruction = @"你是一个专业的 HTML 代码生成器。
规则：
1. 只输出标准的 HTML 代码。
2. 严禁使用 Markdown 代码块（不要写 ```html）。
3. 严禁包含任何解释性文字。
4. 自动为文章分段添加 <p> 标签。";
        }
        else
        {
            systemInstruction = @"你是一个有用的 AI 助手。请直接回答用户的问题。";
        }

        // --- 构建 Messages 数组 ---
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

    /// <summary>
    /// 清洗 AI 返回的数据 (Markdown -> HTML)
    /// </summary>
    private string CleanAiResponse(string content, bool isHtmlMode)
    {
        if (string.IsNullOrWhiteSpace(content)) return string.Empty;

        // 1. 去掉 Markdown 代码块
        content = Regex.Replace(content, @"```[a-zA-Z]*", "", RegexOptions.IgnoreCase);
        content = content.Replace("```", "").Trim();

        // 2. HTML 模式下的兜底逻辑
        if (isHtmlMode)
        {
            // 如果内容里找不到 <p> 或 <div>，说明 AI 给了纯文本
            bool hasHtmlTags = Regex.IsMatch(content, @"<[a-z][\s\S]*>", RegexOptions.IgnoreCase);

            if (!hasHtmlTags)
            {
                // 把换行符变成 HTML 标签
                var processed = content.Replace("\n\n", "</p><p>")
                                       .Replace("\r\n\r\n", "</p><p>")
                                       .Replace("\n", "<br/>");
                content = $"<div class=\"ai-generated\"><p>{processed}</p></div>";
            }
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