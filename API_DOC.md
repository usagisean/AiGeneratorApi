# AiGeneratorApi 接口文档

> **Base URL**：`http://你的VPS域名:6677`  
> **认证方式**：请求头 `x-api-key: 你的API密钥`

---

## 1. 生成文章

**`POST /api/v1/article/generate`**

### 请求参数（JSON Body）

| 字段 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `prompt` | string | ✅ | — | 文章主题 / 提示词 |
| `provider` | string | ❌ | `"newapi"` | AI 提供商：`google` / `newapi` |
| `style` | string | ❌ | `"news"` | 写作风格（见下表） |
| `modelName` | string | ❌ | 配置默认 | 指定模型名称 |
| `wordCount` | int | ❌ | `1500` | 目标字数，0 = 不限 |
| `language` | string | ❌ | `"zh"` | 语言：`zh` / `en` / `ja` |
| `isHtml` | bool | ❌ | `true` | true = 生成结构化文章，false = 普通聊天 |

### 可用写作风格 (`style`)

| 值 | 风格 | 说明 |
|----|------|------|
| `news` | 新闻时评 | 严肃客观，深度分析 |
| `blog` | 博客随笔 | 轻松个人化，有故事感 |
| `review` | 产品评测 | 对比专业，有数据支撑 |
| `tech` | 科技解读 | 前沿技术，通俗易懂 |
| `opinion` | 观点评论 | 犀利有态度，引发思考 |
| `tutorial` | 教程指南 | 步骤清晰，实操性强 |
| `story` | 故事叙事 | 情节驱动，引人入胜 |
| `lifestyle` | 生活方式 | 温馨实用，生活气息 |
| `finance` | 财经分析 | 数据驱动，理性分析 |
| `health` | 健康科普 | 科学权威，通俗易懂 |

### 成功响应

```json
{
  "success": true,
  "message": "生成成功",
  "timestamp": "2026-03-04T21:10:00+08:00",
  "data": {
    "provider": "newapi",
    "modelUsed": "gpt-4o",
    "style": "tech",
    "title": "AI革命：人工智能重塑教育未来",
    "content": "<h2>变革浪潮</h2><p>正文内容...</p>",
    "keywords": "人工智能,教育改革,AI教学,智能教育",
    "description": "本文深度分析人工智能对传统教育的冲击..."
  }
}
```

### 失败响应

```json
{
  "success": false,
  "message": "提示词 prompt 不能为空",
  "timestamp": "2026-03-04T21:10:00+08:00",
  "data": null
}
```

---

## 2. 获取模型列表

**`GET /api/v1/models?provider=newapi`**

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `provider` | string | `"newapi"` | `google` / `newapi` |

### 响应

```json
{
  "success": true,
  "message": "获取成功",
  "timestamp": "...",
  "data": {
    "provider": "newapi",
    "count": 5,
    "models": ["gpt-4o", "gpt-4o-mini", "..."]
  }
}
```

---

## 3. 获取写作风格列表

**`GET /api/v1/styles`**

无需参数，返回所有可用风格及其描述。

### 响应

```json
{
  "success": true,
  "message": "获取成功",
  "timestamp": "...",
  "data": {
    "count": 10,
    "styles": [
      {
        "value": "news",
        "role": "资深新闻时评人",
        "instruction": "以客观立场深度分析..."
      }
    ]
  }
}
```
