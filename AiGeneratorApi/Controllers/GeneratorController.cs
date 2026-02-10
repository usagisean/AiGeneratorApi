using Microsoft.AspNetCore.Mvc;
using AiGeneratorApi.Interface;
using AiGeneratorApi.Model;

namespace AiGeneratorApi.Controllers;

[ApiController]
[Route("[controller]")]
public class GeneratorController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;

    public GeneratorController(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 生成文章接口
    /// 调用方式：POST /Generator/generate?provider=newapi
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateRequest request, [FromQuery] string provider = "google")
    {
        // 1. 获取对应的服务 (google 或 newapi)
        // 注意：如果不传 provider，默认用 google。如果你主要用 newapi，这里 URL 参数记得带上
        var aiService = _serviceProvider.GetKeyedService<IAIService>(provider);

        if (aiService == null)
        {
            return BadRequest($"不支持的提供商: {provider}。请使用 'google' 或 'newapi'。");
        }

        // 2. 校验参数
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest("提示词 Prompt 不能为空");
        }

        try
        {
            // 3. 调用服务
            // ⚠️ 关键点：NewApiService.cs 里已经处理了 BuildArticlePrompt 和 CleanAiResponse
            // 所以这里直接透传 request 即可，Controller 不需要再做任何逻辑处理！
            var result = await aiService.GenerateContentAsync(request);
            
            return Ok(new
            {
                provider = provider,
                modelUsed = request.ModelName ?? "Default",
                isHtml = request.IsHtml,
                content = result // Service 返回的已经是处理好、无换行的 HTML 了
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"生成失败: {ex.Message}" });
        }
    }

    /// <summary>
    /// 获取模型列表接口
    /// </summary>
    [HttpGet("models")]
    public async Task<IActionResult> GetModels([FromQuery] string provider = "google")
    {
        var aiService = _serviceProvider.GetKeyedService<IAIService>(provider);

        if (aiService == null)
        {
            return BadRequest($"不支持的提供商: {provider}");
        }

        try
        {
            var models = await aiService.GetModelsAsync();
            return Ok(new
            {
                provider = provider,
                count = models.Count,
                models = models
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"获取模型列表失败: {ex.Message}");
        }
    }
}