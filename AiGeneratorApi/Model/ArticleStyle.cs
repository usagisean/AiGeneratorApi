namespace AiGeneratorApi.Model;

/// <summary>
/// 文章写作风格枚举
/// 不同风格会影响提示词的构建方式，使生成内容更贴合目标场景
/// </summary>
public enum ArticleStyle
{
    /// <summary>新闻时评：严肃、客观、有深度分析</summary>
    News,

    /// <summary>博客随笔：轻松、个人化、有故事感</summary>
    Blog,

    /// <summary>产品评测：对比、专业、有数据支撑</summary>
    Review,

    /// <summary>科技解读：前沿、通俗易懂、有技术含量</summary>
    Tech,

    /// <summary>观点评论：犀利、有态度、引发思考</summary>
    Opinion,

    /// <summary>教程指南：步骤清晰、实用、有操作性</summary>
    Tutorial,

    /// <summary>故事叙事：情节驱动、有画面感、引人入胜</summary>
    Story,

    /// <summary>生活方式：温馨、实用、有生活气息</summary>
    Lifestyle,

    /// <summary>财经分析：数据驱动、理性、有市场洞察</summary>
    Finance,

    /// <summary>健康科普：科学、权威、通俗易懂</summary>
    Health
}

/// <summary>
/// 风格描述映射工具类
/// 将枚举转换为提示词中使用的角色和写作指令
/// </summary>
public static class ArticleStyleExtensions
{
    /// <summary>
    /// 获取风格对应的 AI 角色描述（用于提示词中的角色定义）
    /// </summary>
    public static string GetRoleDescription(this ArticleStyle style) => style switch
    {
        ArticleStyle.News      => "资深新闻时评人",
        ArticleStyle.Blog      => "知名博主和自媒体创作者",
        ArticleStyle.Review    => "专业产品评测师",
        ArticleStyle.Tech      => "资深科技记者和技术分析师",
        ArticleStyle.Opinion   => "犀利的社会评论家",
        ArticleStyle.Tutorial  => "经验丰富的技术教程作者",
        ArticleStyle.Story     => "擅长叙事的非虚构作家",
        ArticleStyle.Lifestyle => "生活方式领域的知名博主",
        ArticleStyle.Finance   => "资深财经分析师",
        ArticleStyle.Health    => "权威健康科普作者",
        _ => "资深新闻时评人"
    };

    /// <summary>
    /// 获取风格对应的写作指令（用于提示词中的具体要求）
    /// </summary>
    public static string GetWritingInstruction(this ArticleStyle style) => style switch
    {
        ArticleStyle.News      => "以客观立场深度分析事件背景、社会影响和未来走向，语言严谨有力。",
        ArticleStyle.Blog      => "以第一人称、轻松口吻分享观点和经历，拉近与读者的距离，适当使用比喻和故事。",
        ArticleStyle.Review    => "从多个维度进行对比分析，列举优缺点，给出专业推荐建议，语言客观有说服力。",
        ArticleStyle.Tech      => "将复杂技术概念用通俗语言解释，分析技术趋势和应用前景，保持专业但不晦涩。",
        ArticleStyle.Opinion   => "观点鲜明、论证有力，善用反问和对比，引导读者思考，但避免偏激。",
        ArticleStyle.Tutorial  => "步骤清晰、图文并茂，每一步都有具体操作说明，适合零基础读者跟着做。",
        ArticleStyle.Story     => "以故事化手法展开叙述，有人物、有场景、有情节转折，让读者身临其境。",
        ArticleStyle.Lifestyle => "温馨实用，分享生活技巧和美好体验，传递积极向上的生活态度。",
        ArticleStyle.Finance   => "数据说话、逻辑严密，分析市场趋势和投资机会，给出理性建议。",
        ArticleStyle.Health    => "引用权威研究，用通俗语言解释医学概念，给出科学实用的健康建议。",
        _ => "以客观立场深度分析事件背景、社会影响和未来走向。"
    };
}
