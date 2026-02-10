using Microsoft.AspNetCore.Mvc;
using AiGeneratorApi.Interface;
using AiGeneratorApi.Model;

namespace AiGeneratorApi.Controllers;

[ApiController]
[Route("[controller]")]
public class GeneratorController : ControllerBase
{
    // 这里的 _serviceProvider 就是用来“查找”服务的工具
    private readonly IServiceProvider _serviceProvider;

    // 构造函数：注入 IServiceProvider
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
        // 1. 【核心逻辑】根据 provider 字符串（"google" 或 "newapi"）获取对应的服务
        // GetKeyedService 是 .NET 8 新增的 API，专门配合 AddKeyedScoped 使用
        var aiService = _serviceProvider.GetKeyedService<IAIService>(provider);

        // 2. 如果找不到（比如用户传了 provider=baidu，但我们没注册），报错
        if (aiService == null)
        {
            return BadRequest($"不支持的提供商: {provider}。请使用 'google' 或 'newapi'。");
        }

        // 3. 调用服务
        try
        {
            var result = await aiService.GenerateContentAsync(request);
            
            return Ok(new
            {
                provider = provider,
                modelUsed = request.ModelName ?? "Default",
                content = result
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"生成失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 获取模型列表接口
    /// 调用方式：GET /Generator/models?provider=newapi
    /// </summary>
    [HttpGet("models")]
    public async Task<IActionResult> GetModels([FromQuery] string provider = "google")
    {
        // 1. 同样的逻辑，根据 key 获取服务
        var aiService = _serviceProvider.GetKeyedService<IAIService>(provider);

        if (aiService == null)
        {
            return BadRequest($"不支持的提供商: {provider}");
        }

        // 2. 调用 GetModelsAsync
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