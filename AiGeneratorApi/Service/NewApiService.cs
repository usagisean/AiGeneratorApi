using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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

        // 保持之前的伪装，防止 Cloudflare 拦截
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    public async Task<string> GenerateContentAsync(GenerateRequest request)
    {
        // 1. 确定模型
        var model = !string.IsNullOrEmpty(request.ModelName) ? request.ModelName : _config.DefaultModelId;

        // 2. 【智能路由核心逻辑】
        // 只要配置了免费 Key，就先试免费的（省钱第一！）
        if (!string.IsNullOrEmpty(_config.FreeApiKey))
        {
            try
            {
                // 尝试用免费通道请求
                // 注意：如果模型在免费账号里没有（比如 gpt-4），这里会抛出异常，正好被下面的 catch 捕获
                return await ExecuteRequestAsync(request.Prompt, model, _config.FreeApiKey, "Free");
            }
            catch (Exception ex)
            {
                // 捕获所有失败（404模型不存在、401没权限、500服务器挂了...）
                // 只是在控制台记录一下，不抛出给用户
                Console.WriteLine($"[SmartRoute] 免费通道无法服务 '{model}': {ex.Message} -> 正在切换 VIP 通道兜底...");
            }
        }

        // 3. 既然免费通道搞不定（或者没配），那就用 VIP 通道（全能王）兜底
        // 如果这里也挂了，那就真挂了，直接把异常抛给 Controller
        return await ExecuteRequestAsync(request.Prompt, model, _config.VipApiKey, "VIP");
    }

    private async Task<string> ExecuteRequestAsync(string prompt, string model, string apiKey, string channel)
    {
        if(string.IsNullOrEmpty(apiKey)) throw new Exception($"{channel} Key 未配置");

        var url = $"{_config.BaseUrl.TrimEnd('/')}/v1/chat/completions";
        var requestBody = new
        {
            model = model,
            messages = new[] { new { role = "user", content = prompt } },
            temperature = 0.7
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(httpRequest);
        
        if (!response.IsSuccessStatusCode)
        {
            // 这里抛出的异常会被外面的 catch 捕获，从而触发降级逻辑
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

    // 获取模型列表逻辑保持不变，依然是“我全都要”
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