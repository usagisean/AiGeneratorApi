# AiGeneratorApi 接口文档

> **Base URL**：`http://你的VPS域名:6677`  
> **认证方式**：请求头 `x-api-key: 你的API密钥`

## 客户端推荐流程

1. 调用 `GET /api/v1/models?provider=newapi` 获取模型列表。
2. 客户端展示模型并让用户选择。
3. 调用 `POST /api/v1/article/generate`，把选择结果放到 `modelName`。
4. 服务端使用 NewAPI / LiteLLM / NVIDIA NIM 渠道执行该模型。

保持兼容：接口路径、请求字段、响应包装结构不变。

---

## 1. 生成文章

**`POST /api/v1/article/generate`**

### 请求参数（JSON Body）

| 字段 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `prompt` | string | ✅ | — | 文章主题 / 提示词 |
| `provider` | string | ❌ | `"newapi"` | AI 提供商。当前默认启用 `newapi`；`google` 保留但默认禁用 |
| `style` | string | ❌ | `"news"` | 写作风格（见下表） |
| `modelName` | string | ❌ | `z-ai/glm-5.2` | 指定模型名称。通常来自 `/api/v1/models` 返回列表 |
| `wordCount` | int | ❌ | `1500` | 目标字数，0 = 不限 |
| `language` | string | ❌ | `"zh"` | 语言：`zh` / `en` / `ja` |
| `isHtml` | bool | ❌ | `true` | true = 生成结构化文章；false = 普通文本 |

### modelName 说明

- 不传 `modelName`：使用 `AIConfig__NewApi__DefaultModelId`，默认 `z-ai/glm-5.2`。
- 传 `modelName`：服务端优先按客户端指定模型调用 NewAPI。
- 如果指定模型调用失败，服务端仍保留原有 fallback 行为，响应中的 `data.modelUsed` 会显示实际使用模型。

### 当前 NewAPI 模型列表

默认会实时请求 NewAPI `/v1/models` 拉取全量模型，并和本服务内置重点模型合并去重。下面是内置重点模型，实际返回数量会随 NewAPI 后台配置变化：

```text
z-ai/glm-5.2
deepseek-ai/deepseek-v4-flash
deepseek-ai/deepseek-v4-pro
qwen/qwen3.5-122b-a10b
qwen/qwen3.5-397b-a17b
moonshotai/kimi-k2.6
minimaxai/minimax-m3
nvidia/nemotron-3-ultra-550b-a55b
nvidia/nemotron-3-super-120b-a12b
openai/gpt-oss-120b
openai/gpt-oss-20b
```

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

### 请求示例

```json
{
  "prompt": "人工智能对教育行业的影响",
  "provider": "newapi",
  "modelName": "z-ai/glm-5.2",
  "style": "tech",
  "wordCount": 800,
  "language": "zh",
  "isHtml": true
}
```

### 成功响应

```json
{
  "success": true,
  "message": "生成成功",
  "timestamp": "2026-03-04T21:10:00+08:00",
  "data": {
    "provider": "newapi",
    "modelUsed": "z-ai/glm-5.2",
    "style": "tech",
    "title": "AI革命：人工智能重塑教育未来",
    "content": "<div>正文内容...</div>",
    "keywords": "人工智能,教育改革,AI教学,智能教育",
    "description": "一段自然摘要...",
    "comments": ["评论1", "评论2", "评论3", "评论4"]
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
| `provider` | string | `"newapi"` | 当前默认使用 `newapi` |

### 响应

```json
{
  "success": true,
  "message": "获取成功",
  "timestamp": "...",
  "data": {
    "provider": "newapi",
    "count": 42,
    "models": [
      "z-ai/glm-5.2",
      "deepseek-ai/deepseek-v4-flash",
      "deepseek-ai/deepseek-v4-pro",
      "..."
    ]
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
