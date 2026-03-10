using Microsoft.AspNetCore.Mvc;
using AiGeneratorApi.Interface;
using AiGeneratorApi.Model;

namespace AiGeneratorApi.Controllers;

[ApiController]
[Route("api/v1")]
public class GeneratorController : ControllerBase
{
    private readonly IServiceProvider _serviceProvider;

    public GeneratorController(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 生成文章接口
    /// POST /api/v1/article/generate
    /// </summary>
    [HttpPost("article/generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateRequest request)
    {
        // 1. 参数校验
        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return BadRequest(ApiResponse<object>.Fail("提示词 prompt 不能为空"));
        }

        // 2. 获取对应的 AI 服务
        var aiService = _serviceProvider.GetKeyedService<IAIService>(request.Provider);
        if (aiService == null)
        {
            var supported = new[] { "google", "newapi" };
            return BadRequest(ApiResponse<object>.Fail(
                $"不支持的提供商: {request.Provider}。可选值: {string.Join(", ", supported)}"
            ));
        }

        try
        {
            // 3. 调用服务生成内容（服务层内部会自动 fallback）
            var result = await aiService.GenerateContentAsync(request);

            return Ok(ApiResponse<object>.Ok(new
            {
                provider = request.Provider,
                // NOTE: 优先展示 result.ActualModel（服务层 fallback 后会设置此值），兜底用请求参数
                modelUsed = result.ActualModel ?? request.ModelName ?? "default",
                style = request.Style.ToString().ToLower(),
                title = result.Title,
                content = result.Content,
                keywords = result.Keywords,
                description = result.Description,
                comments = result.Comments
            }));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail($"生成失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 获取可用模型列表
    /// GET /api/v1/models?provider=newapi
    /// </summary>
    [HttpGet("models")]
    public async Task<IActionResult> GetModels([FromQuery] string provider = "newapi")
    {
        var aiService = _serviceProvider.GetKeyedService<IAIService>(provider);
        if (aiService == null)
        {
            return BadRequest(ApiResponse<object>.Fail($"不支持的提供商: {provider}"));
        }

        try
        {
            var models = await aiService.GetModelsAsync();
            return Ok(ApiResponse<object>.Ok(new
            {
                provider = provider,
                count = models.Count,
                models = models
            }, "获取成功"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<object>.Fail($"获取模型列表失败: {ex.Message}"));
        }
    }

    /// <summary>
    /// 获取支持的写作风格列表
    /// GET /api/v1/styles
    /// </summary>
    [HttpGet("styles")]
    public IActionResult GetStyles()
    {
        var styles = Enum.GetValues<ArticleStyle>().Select(s => new
        {
            value = s.ToString().ToLower(),
            role = s.GetRoleDescription(),
            instruction = s.GetWritingInstruction()
        });

        return Ok(ApiResponse<object>.Ok(new
        {
            count = styles.Count(),
            styles = styles
        }, "获取成功"));
    }
}