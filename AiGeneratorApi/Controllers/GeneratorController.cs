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

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateRequest request)
    {
        if (string.IsNullOrEmpty(request.Prompt))
            return BadRequest("提示词不能为空");

        // 默认为 google
        var providerKey = string.IsNullOrEmpty(request.Provider) ? "google" : request.Provider.ToLower();

        try
        {
            // 【核心】使用 Keyed Services 动态获取服务
            var aiService = _serviceProvider.GetKeyedService<IAIService>(providerKey);

            if (aiService == null)
            {
                return BadRequest($"不支持的 Provider: {providerKey}。请尝试: google, openai");
            }

            // 调用统一接口
            var result = await aiService.GenerateContentAsync(request);

            return Ok(new
            {
                Provider = providerKey,
                ModelUsed = request.ModelName ?? "Default",
                Content = result
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"生成失败: {ex.Message}");
        }
    }
}