# AiGeneratorApi

.NET 8 ASP.NET Core Web API，用于通过 AI provider 生成文章/HTML 文本。

当前默认接入方向：

1. 客户端调用 `GET /api/v1/models?provider=newapi` 获取可选模型列表。
2. 客户端自行选择模型。
3. 客户端调用 `POST /api/v1/article/generate`，把选择的 `modelName` 原样传回服务端。
4. 服务端通过 NewAPI / LiteLLM / NVIDIA NIM 渠道执行底层模型调用。

默认 provider 是 `newapi`，默认模型是 `z-ai/glm-5.2`。Google provider 保留代码兼容，但默认禁用。
模型列表默认会实时请求 NewAPI `/v1/models`，并和本服务内置的重点模型合并去重后返回给客户端。

## 主要接口

- `GET /api/v1/models?provider=newapi`：获取模型列表
- `POST /api/v1/article/generate`：生成文章或普通文本
- `GET /api/v1/styles`：获取文章风格列表

认证方式：所有接口需要请求头：

```http
x-api-key: <MY_API_KEY>
```

## 默认 NewAPI 模型

配置集中在 `AIConfig:NewApi`，默认模型列表包括：

- `z-ai/glm-5.2`
- `deepseek-ai/deepseek-v4-flash`
- `deepseek-ai/deepseek-v4-pro`
- `qwen/qwen3.5-122b-a10b`
- `qwen/qwen3.5-397b-a17b`
- `moonshotai/kimi-k2.6`
- `minimaxai/minimax-m3`
- `nvidia/nemotron-3-ultra-550b-a55b`
- `nvidia/nemotron-3-super-120b-a12b`
- `openai/gpt-oss-120b`
- `openai/gpt-oss-20b`

不传 `modelName` 时使用 `AIConfig__NewApi__DefaultModelId`，当前默认是 `z-ai/glm-5.2`。

## 本地运行

```bash
cd /Users/zhaozhixiang/usagi/AiGeneratorApi
cp .env.example AiGeneratorApi/.env
# 编辑 AiGeneratorApi/.env，填入 MY_API_KEY 和 NewAPI Key

dotnet restore
dotnet run --project AiGeneratorApi/AiGeneratorApi.csproj
```

默认 launch profile 端口通常是 `http://localhost:5082`。如果要固定端口：

```bash
ASPNETCORE_URLS=http://localhost:6677 dotnet run --project AiGeneratorApi/AiGeneratorApi.csproj
```

## 最小验证

```bash
export BASE_URL=http://localhost:6677
export API_KEY=<你的 MY_API_KEY>
./test_api.sh
```

手动验证 `z-ai/glm-5.2`：

```bash
curl -s "${BASE_URL}/api/v1/models?provider=newapi" \
  -H "x-api-key: ${API_KEY}" | python3 -m json.tool

curl -s -X POST "${BASE_URL}/api/v1/article/generate" \
  -H "Content-Type: application/json" \
  -H "x-api-key: ${API_KEY}" \
  -d '{
    "prompt": "请用一句话确认当前模型可用。",
    "provider": "newapi",
    "modelName": "z-ai/glm-5.2",
    "isHtml": false
  }' | python3 -m json.tool
```

响应里的 `data.modelUsed` 应为 `z-ai/glm-5.2`，除非该模型调用失败后触发 fallback。

## Docker 构建与运行

构建镜像：

```bash
docker build -t aigeneratorapi:local .
```

本地运行：

```bash
docker run --rm -p 6677:8080 \
  -e IP_WHITELIST="*" \
  -e MY_API_KEY="<你的访问 API Key>" \
  -e AIConfig__NewApi__BaseUrl="https://api.zxaihub.com" \
  -e AIConfig__NewApi__DefaultModelId="z-ai/glm-5.2" \
  -e AIConfig__NewApi__FetchRemoteModels="true" \
  -e AIConfig__NewApi__FreeApiKey="<可选>" \
  -e AIConfig__NewApi__VipApiKey="<可选>" \
  -e AIConfig__Gemini__Enabled="false" \
  aigeneratorapi:local
```

## 生产环境变量

必填：

- `MY_API_KEY`：业务访问密钥，对应请求头 `x-api-key`
- `IP_WHITELIST`：允许访问的 IP 列表，多个用逗号分隔；可信测试环境可用 `*`
- `AIConfig__NewApi__BaseUrl`：默认 `https://api.zxaihub.com`
- `AIConfig__NewApi__DefaultModelId`：默认 `z-ai/glm-5.2`
- `AIConfig__NewApi__FreeApiKey` 或 `AIConfig__NewApi__VipApiKey`：至少配置一个真实 NewAPI Key

建议：

- `AIConfig__NewApi__FetchRemoteModels=true`：默认实时拉取 NewAPI 全量模型，并合并本服务内置重点模型；如需只返回固定列表可改为 `false`
- `AIConfig__Gemini__Enabled=false`：生产默认不依赖 Google
- `AIConfig__NewApi__RequestTimeoutSeconds=300`
- `AIConfig__NewApi__ModelListTimeoutSeconds=10`

如需覆盖模型列表，可使用数组环境变量格式：

```bash
AIConfig__NewApi__Models__0=z-ai/glm-5.2
AIConfig__NewApi__Models__1=deepseek-ai/deepseek-v4-flash
AIConfig__NewApi__FallbackModels__0=z-ai/glm-5.2
```
