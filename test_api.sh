#!/usr/bin/env bash
set -euo pipefail

# AiGeneratorApi 最小验证脚本
# 用法：
#   export BASE_URL=http://localhost:6677
#   export API_KEY=<MY_API_KEY>
#   ./test_api.sh

BASE_URL="${BASE_URL:-http://localhost:6677}"
API_KEY="${API_KEY:-${MY_API_KEY:-}}"
MODEL_NAME="${MODEL_NAME:-z-ai/glm-5.2}"

if [[ -z "${API_KEY}" ]]; then
  echo "ERROR: 请先设置 API_KEY 或 MY_API_KEY" >&2
  exit 1
fi

pretty_json() {
  python3 -m json.tool 2>/dev/null || cat
}

api_get() {
  local path="$1"
  curl -sS "${BASE_URL}${path}" \
    -H "x-api-key: ${API_KEY}"
}

api_post() {
  local path="$1"
  local payload="$2"
  curl -sS -X POST "${BASE_URL}${path}" \
    -H "Content-Type: application/json" \
    -H "x-api-key: ${API_KEY}" \
    -d "${payload}"
}

printf '==========================================\n'
printf '  AiGeneratorApi minimal smoke test\n'
printf '  BASE_URL=%s\n' "${BASE_URL}"
printf '  MODEL_NAME=%s\n' "${MODEL_NAME}"
printf '==========================================\n\n'

printf '1) GET /api/v1/styles\n'
api_get "/api/v1/styles" | pretty_json
printf '\n'

printf '2) GET /api/v1/models?provider=newapi\n'
MODELS_RESPONSE="$(api_get "/api/v1/models?provider=newapi")"
printf '%s' "${MODELS_RESPONSE}" | pretty_json
printf '\n'

if ! printf '%s' "${MODELS_RESPONSE}" | grep -q "${MODEL_NAME}"; then
  echo "ERROR: 模型列表中未找到 ${MODEL_NAME}" >&2
  exit 1
fi

printf '3) POST /api/v1/article/generate with modelName=%s\n' "${MODEL_NAME}"
GENERATE_PAYLOAD=$(cat <<JSON
{
  "prompt": "请用一句话确认当前模型可用，直接回答即可。",
  "provider": "newapi",
  "modelName": "${MODEL_NAME}",
  "isHtml": false,
  "language": "zh"
}
JSON
)
api_post "/api/v1/article/generate" "${GENERATE_PAYLOAD}" | pretty_json
printf '\n'

printf '4) POST /api/v1/article/generate empty prompt should fail\n'
api_post "/api/v1/article/generate" '{"prompt":"","provider":"newapi"}' | pretty_json
printf '\n'

printf '==========================================\n'
printf '  Smoke test finished\n'
printf '==========================================\n'
