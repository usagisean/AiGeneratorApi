# NVIDIA NIM 模型选型说明

最后验证：2026-10-10。测试链路：NVIDIA NIM → LiteLLM → NewAPI → AiGeneratorApi。

## 当前可用模型

| 优先级 | 模型 | 用途 |
|---|---|---|
| 1 | `z-ai/glm-5.3-flash` | 默认模型；中文写作、HTML 结构化输出、速度和质量均衡 |
| 2 | `nvidia/nemotron-3.5-lightning-30b-a3b` | 快速 fallback，适合批量生成 |
| 3 | `nvidia/nemotron-3-super-120b-a12b` | 质量优先的通用文本生成 |
| 4 | `openai/gpt-oss-20b` | 轻量、稳定 fallback |
| 5 | `z-ai/glm-5.3` | 复杂推理、长文和代码任务 |
| 6 | `nvidia/nemotron-3-ultra-550b-a55b` | 高质量复杂任务 |
| 7 | `moonshotai/kimi-k3` | 长上下文、多模态和 agentic 任务 |

## 默认 fallback 顺序

1. `z-ai/glm-5.3-flash`
2. `nvidia/nemotron-3.5-lightning-30b-a3b`
3. `nvidia/nemotron-3-super-120b-a12b`
4. `openai/gpt-oss-20b`

`z-ai/glm-5.2` 及旧版 DeepSeek/Qwen/Kimi/GPT-OSS-120B 模型已从客户端列表移除。LiteLLM 暂时保留 `z-ai/glm-5.2` 兼容别名，并将其路由到 `z-ai/glm-5.3-flash`，用于平滑迁移旧客户端。
