# AiGeneratorApi 模型使用说明

> 更新时间：2026-07-06  
> 测试方式：通过 VPS `POST /api/v1/article/generate`，`isHtml=false`，短文本提示词逐个调用。  
> 说明：耗时为一次短文本实测结果，只用于粗略判断稳定性和延迟；不同时间、负载和提示词长度会波动。

## 推荐优先级

| 优先级 | 模型 | 建议用途 | 备注 |
|---:|---|---|---|
| 1 | `z-ai/glm-5.2` | 客户端优先展示 | 默认推荐。中文写作、博客、结构化 HTML 输出表现均衡。 实测：4.0s |
| 2 | `qwen/qwen3.5-122b-a10b` | 客户端优先展示 | 中文能力强，实测响应较快，适合中文内容生成。 实测：1.51s |
| 3 | `deepseek-ai/deepseek-v4-flash` | 客户端优先展示 | 偏快速通用模型，适合批量短文、摘要、改写。 实测：7.54s |
| 4 | `minimaxai/minimax-m3` | 客户端优先展示 | 实测很快，适合快速生成、标题、短内容。 实测：0.51s |
| 5 | `moonshotai/kimi-k2.6` | 客户端优先展示 | 中文写作和长上下文友好，适合文章、资料整理。 实测：4.37s |
| 6 | `openai/gpt-oss-120b` | 客户端优先展示 | OpenAI OSS 大模型，通用能力强，实测较快。 实测：1.33s |
| 7 | `nvidia/nemotron-3-super-120b-a12b` | 客户端优先展示 | NVIDIA 中大型模型，速度和能力平衡。 实测：1.12s |

## 可展示模型（直连成功）

这些模型会保留在 `/api/v1/models?provider=newapi` 返回列表中，共 **27** 个。

| 模型 | 实测耗时 | 速度 | 建议用途/备注 |
|---|---:|---|---|
| `z-ai/glm-5.2` | 4.0s | 中 | 默认推荐。中文写作、博客、结构化 HTML 输出表现均衡。 |
| `deepseek-ai/deepseek-v4-flash` | 7.54s | 较慢 | 偏快速通用模型，适合批量短文、摘要、改写。 |
| `deepseek-ai/deepseek-v4-pro` | 14.83s | 较慢 | 质量优先，适合更复杂推理、长文和深度内容。 |
| `qwen/qwen3.5-122b-a10b` | 1.51s | 中 | 中文能力强，实测响应较快，适合中文内容生成。 |
| `qwen/qwen3.5-397b-a17b` | 31.94s | 慢 | 超大 Qwen，质量潜力高但实测较慢，适合少量高质量任务。 |
| `moonshotai/kimi-k2.6` | 4.37s | 中 | 中文写作和长上下文友好，适合文章、资料整理。 |
| `minimaxai/minimax-m3` | 0.51s | 快 | 实测很快，适合快速生成、标题、短内容。 |
| `nvidia/nemotron-3-ultra-550b-a55b` | 0.95s | 快 | NVIDIA 大模型，实测直连且快，适合推理/通用问答。 |
| `nvidia/nemotron-3-super-120b-a12b` | 1.12s | 快 | NVIDIA 中大型模型，速度和能力平衡。 |
| `openai/gpt-oss-120b` | 1.33s | 快 | OpenAI OSS 大模型，通用能力强，实测较快。 |
| `openai/gpt-oss-20b` | 0.8s | 快 | 轻量 OSS，速度快，适合低成本短任务。 |
| `google/diffusiongemma-26b-a4b-it` | 0.8s | 快 | Gemma 系模型，实测可走 chat；建议低优先级展示。 |
| `google/gemma-4-31b-it` | 0.52s | 快 | Gemma 指令模型，实测快，适合英文/通用短任务。 |
| `meta/llama-3.1-70b-instruct` | 0.86s | 快 | Llama 70B 指令模型，英文/通用任务友好。 |
| `minimaxai/minimax-m2.7` | 11.43s | 较慢 | MiniMax 旧/中型模型，直连成功但实测偏慢。 |
| `mistralai/mistral-large-3-675b-instruct-2512` | 0.5s | 快 | Mistral 大模型，实测很快，适合英文、技术、指令任务。 |
| `mistralai/mistral-medium-3.5-128b` | 0.53s | 快 | Mistral 中型模型，速度快，适合通用内容。 |
| `mistralai/mistral-nemotron` | 0.82s | 快 | Mistral + Nemotron 路线，适合推理/技术类。 |
| `mistralai/mistral-small-4-119b-2603` | 0.56s | 快 | Mistral Small，实测快，适合低延迟通用任务。 |
| `nvidia/llama-3.1-nemotron-nano-8b-v1` | 1.26s | 快 | NVIDIA Nano 小模型，速度快，适合短问答。 |
| `nvidia/llama-3.3-nemotron-super-49b-v1` | 23.13s | 慢 | NVIDIA Super 49B，直连成功但实测慢。 |
| `nvidia/llama-3.3-nemotron-super-49b-v1.5` | 8.69s | 较慢 | NVIDIA Super 49B v1.5，质量/速度折中。 |
| `nvidia/nemotron-3-nano-30b-a3b` | 1.08s | 快 | NVIDIA Nano 30B，速度快，适合通用任务。 |
| `nvidia/nvidia-nemotron-nano-9b-v2` | 4.38s | 中 | NVIDIA Nano 9B，速度中等，适合短任务。 |
| `qwen/qwen3-next-80b-a3b-instruct` | 0.63s | 快 | Qwen Next 指令模型，中文/通用且实测很快。 |
| `stepfun-ai/step-3.5-flash` | 1.14s | 快 | 阶跃 Flash，速度快，适合短内容和轻量任务。 |
| `stepfun-ai/step-3.7-flash` | 1.11s | 快 | 阶跃 Flash 新版本，速度快，适合短内容和轻量任务。 |

## 已排除模型

以下模型在本轮测试中出现 fallback 或 timeout，默认不会再返回给客户端，共 **18** 个。

| 模型 | 测试结果 | 实测耗时 | 说明 |
|---|---|---:|---|
| `bytedance/seed-oss-36b-instruct` | timeout | 90.0s | 90 秒内无响应，排除。 |
| `gemma-pro` | fallback | 4.0s | 请求该模型后降级到 `z-ai/glm-5.2(fallback from gemma-pro)`，说明该模型名当前不可直接使用。 |
| `glm-4` | fallback | 4.34s | 请求该模型后降级到 `z-ai/glm-5.2(fallback from glm-4)`，说明该模型名当前不可直接使用。 |
| `glm-5` | fallback | 4.95s | 请求该模型后降级到 `z-ai/glm-5.2(fallback from glm-5)`，说明该模型名当前不可直接使用。 |
| `gpt-4o` | fallback | 0.79s | 请求该模型后降级到 `z-ai/glm-5.2(fallback from gpt-4o)`，说明该模型名当前不可直接使用。 |
| `gpt-4o-mini` | fallback | 2.33s | 请求该模型后降级到 `z-ai/glm-5.2(fallback from gpt-4o-mini)`，说明该模型名当前不可直接使用。 |
| `gpt-5` | fallback | 3.02s | 请求该模型后降级到 `z-ai/glm-5.2(fallback from gpt-5)`，说明该模型名当前不可直接使用。 |
| `gpt-oss` | fallback | 6.6s | 请求该模型后降级到 `z-ai/glm-5.2(fallback from gpt-oss)`，说明该模型名当前不可直接使用。 |
| `kimi` | fallback | 0.91s | 请求该模型后降级到 `z-ai/glm-5.2(fallback from kimi)`，说明该模型名当前不可直接使用。 |
| `meta/llama-3.3-70b-instruct` | timeout | 90.0s | 90 秒内无响应，排除。 |
| `microsoft/phi-4-mini-instruct` | timeout | 90.0s | 90 秒内无响应，排除。 |
| `minimax` | fallback | 8.76s | 请求该模型后降级到 `z-ai/glm-5.2(fallback from minimax)`，说明该模型名当前不可直接使用。 |
| `nemotron-ultra` | fallback | 7.45s | 请求该模型后降级到 `z-ai/glm-5.2(fallback from nemotron-ultra)`，说明该模型名当前不可直接使用。 |
| `o1` | fallback | 3.76s | 请求该模型后降级到 `z-ai/glm-5.2(fallback from o1)`，说明该模型名当前不可直接使用。 |
| `o1-mini` | fallback | 2.36s | 请求该模型后降级到 `z-ai/glm-5.2(fallback from o1-mini)`，说明该模型名当前不可直接使用。 |
| `phi-mini` | fallback | 4.02s | 请求该模型后降级到 `z-ai/glm-5.2(fallback from phi-mini)`，说明该模型名当前不可直接使用。 |
| `turbo-chat` | fallback | 1.51s | 请求该模型后降级到 `z-ai/glm-5.2(fallback from turbo-chat)`，说明该模型名当前不可直接使用。 |
| `vision-lite` | fallback | 4.93s | 请求该模型后降级到 `z-ai/glm-5.2(fallback from vision-lite)`，说明该模型名当前不可直接使用。 |

## 客户端展示建议

- 默认选中：`z-ai/glm-5.2`。
- 快速模式：优先 `minimaxai/minimax-m3`、`qwen/qwen3-next-80b-a3b-instruct`、`stepfun-ai/step-3.7-flash`。
- 中文内容：优先 `z-ai/glm-5.2`、`qwen/qwen3.5-122b-a10b`、`moonshotai/kimi-k2.6`。
- 高质量但可能慢：`deepseek-ai/deepseek-v4-pro`、`qwen/qwen3.5-397b-a17b`、`nvidia/llama-3.3-nemotron-super-49b-v1`。
- 不建议展示短别名：如 `gpt-4o`、`gpt-5`、`glm-5`、`kimi`，本轮均触发 fallback。请展示完整模型 ID。
