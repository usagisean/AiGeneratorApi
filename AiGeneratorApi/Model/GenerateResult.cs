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
}
