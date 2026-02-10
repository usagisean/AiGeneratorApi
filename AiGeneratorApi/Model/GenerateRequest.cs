using System.ComponentModel.DataAnnotations;

namespace AiGeneratorApi.Model;

public class GenerateRequest
{
    /// <summary>
    /// 用户输入的提示词
    /// </summary>
    public string Prompt { get; set; }

    /// <summary>
    /// 模型名称，默认 gpt-4o
    /// </summary>
    public string ModelName { get; set; } = "gpt-5-chat-latest";

    /// <summary>
    /// 是否生成 HTML 格式。
    /// true = 生成文章/网页，自动清洗 Markdown，强制 HTML 结构。
    /// false = 普通聊天，保留换行符。
    /// </summary>
    public bool IsHtml { get; set; } = true;
}