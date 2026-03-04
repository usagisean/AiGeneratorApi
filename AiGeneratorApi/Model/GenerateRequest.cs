using System.Text.Json.Serialization;

namespace AiGeneratorApi.Model;

/// <summary>
/// 文章生成请求模型
/// </summary>
public class GenerateRequest
{
    /// <summary>
    /// 用户输入的提示词 / 文章主题
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// AI 提供商：google / newapi，默认 newapi
    /// </summary>
    public string Provider { get; set; } = "newapi";

    /// <summary>
    /// 模型名称，默认使用提供商配置的默认模型
    /// </summary>
    public string ModelName { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// 写作风格，默认新闻时评
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ArticleStyle Style { get; set; } = ArticleStyle.News;

    /// <summary>
    /// 目标字数，0 表示不限制，默认 1500
    /// </summary>
    public int WordCount { get; set; } = 1500;

    /// <summary>
    /// 输出语言，默认中文
    /// </summary>
    public string Language { get; set; } = "zh";

    /// <summary>
    /// 是否生成 HTML 格式
    /// true = 生成结构化文章（标题/正文/关键词/摘要），自动清洗为 HTML
    /// false = 普通聊天，直接返回原文
    /// </summary>
    public bool IsHtml { get; set; } = true;
}