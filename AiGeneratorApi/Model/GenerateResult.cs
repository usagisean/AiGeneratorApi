namespace AiGeneratorApi.Model;

/// <summary>
/// AI 生成结果的结构化响应模型
/// 包含 SEO 优化所需的标题、正文、关键词和摘要
/// </summary>
public class GenerateResult
{
    /// <summary>
    /// SEO 优化的文章标题（不含 HTML 标签）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 文章 HTML 正文
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// SEO 关键词，逗号分隔
    /// </summary>
    public string Keywords { get; set; } = string.Empty;

    /// <summary>
    /// 文章摘要 / meta description，约 150 字以内
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 实际使用的模型名称（当指定模型不可用时降级为 fallback 模型，此字段非 null 表示发生了降级）
    /// </summary>
    public string? ActualModel { get; set; }

    /// <summary>
    /// 基于文章内容生成的 4 条有趣用户评论
    /// </summary>
    public List<string> Comments { get; set; } = new();
}
