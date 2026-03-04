#!/bin/bash
# =============================================
# AiGeneratorApi 测试脚本
# 用法: chmod +x test_api.sh && ./test_api.sh
# =============================================

# ⚠️ 请修改为你的实际配置
BASE_URL="http://localhost:6677"
API_KEY=" apiley"

# 通用请求头
HEADERS="-H \"Content-Type: application/json\" -H \"x-api-key: ${API_KEY}\""

echo "=========================================="
echo "  AiGeneratorApi 接口测试"
echo "=========================================="

# ------------------------------------------
# 测试 1: 获取写作风格列表
# ------------------------------------------
echo ""
echo "📋 [测试 1] 获取写作风格列表"
echo "GET /api/v1/styles"
echo "------------------------------------------"
curl -s -X GET "${BASE_URL}/api/v1/styles" \
  -H "x-api-key: ${API_KEY}" | python3 -m json.tool
echo ""

# ------------------------------------------
# 测试 2: 获取模型列表 (NewApi)
# ------------------------------------------
echo ""
echo "📋 [测试 2] 获取模型列表 (NewApi)"
echo "GET /api/v1/models?provider=newapi"
echo "------------------------------------------"
curl -s -X GET "${BASE_URL}/api/v1/models?provider=newapi" \
  -H "x-api-key: ${API_KEY}" | python3 -m json.tool
echo ""

# ------------------------------------------
# 测试 3: 生成文章 (新闻时评风格)
# ------------------------------------------
echo ""
echo "📝 [测试 3] 生成文章 - news 风格"
echo "POST /api/v1/article/generate"
echo "------------------------------------------"
curl -s -X POST "${BASE_URL}/api/v1/article/generate" \
  -H "Content-Type: application/json" \
  -H "x-api-key: ${API_KEY}" \
  -d '{
    "prompt": "人工智能对教育行业的影响",
    "provider": "newapi",
    "style": "news",
    "wordCount": 800,
    "language": "zh"
  }' | python3 -m json.tool
echo ""

# ------------------------------------------
# 测试 4: 生成文章 (科技解读风格)
# ------------------------------------------
echo ""
echo "📝 [测试 4] 生成文章 - tech 风格"
echo "POST /api/v1/article/generate"
echo "------------------------------------------"
curl -s -X POST "${BASE_URL}/api/v1/article/generate" \
  -H "Content-Type: application/json" \
  -H "x-api-key: ${API_KEY}" \
  -d '{
    "prompt": "2026年大模型技术的最新突破",
    "provider": "newapi",
    "style": "tech",
    "wordCount": 1000,
    "language": "zh"
  }' | python3 -m json.tool
echo ""

# ------------------------------------------
# 测试 5: 生成文章 (博客随笔风格)
# ------------------------------------------
echo ""
echo "📝 [测试 5] 生成文章 - blog 风格"
echo "POST /api/v1/article/generate"
echo "------------------------------------------"
curl -s -X POST "${BASE_URL}/api/v1/article/generate" \
  -H "Content-Type: application/json" \
  -H "x-api-key: ${API_KEY}" \
  -d '{
    "prompt": "一个程序员的周末日常",
    "provider": "newapi",
    "style": "blog",
    "wordCount": 600,
    "language": "zh"
  }' | python3 -m json.tool
echo ""

# ------------------------------------------
# 测试 6: 随机风格生成
# ------------------------------------------
STYLES=("news" "blog" "review" "tech" "opinion" "tutorial" "story" "lifestyle" "finance" "health")
RANDOM_STYLE=${STYLES[$RANDOM % ${#STYLES[@]}]}

echo ""
echo "🎲 [测试 6] 随机风格生成 -> ${RANDOM_STYLE}"
echo "POST /api/v1/article/generate"
echo "------------------------------------------"
curl -s -X POST "${BASE_URL}/api/v1/article/generate" \
  -H "Content-Type: application/json" \
  -H "x-api-key: ${API_KEY}" \
  -d "{
    \"prompt\": \"短视频时代年轻人的注意力危机\",
    \"provider\": \"newapi\",
    \"style\": \"${RANDOM_STYLE}\",
    \"wordCount\": 1000,
    \"language\": \"zh\"
  }" | python3 -m json.tool
echo ""

# ------------------------------------------
# 测试 7: 错误测试 - 空 prompt
# ------------------------------------------
echo ""
echo "❌ [测试 7] 错误测试 - 空 prompt"
echo "------------------------------------------"
curl -s -X POST "${BASE_URL}/api/v1/article/generate" \
  -H "Content-Type: application/json" \
  -H "x-api-key: ${API_KEY}" \
  -d '{
    "prompt": "",
    "provider": "newapi"
  }' | python3 -m json.tool
echo ""

# ------------------------------------------
# 测试 8: 错误测试 - 错误的 provider
# ------------------------------------------
echo ""
echo "❌ [测试 8] 错误测试 - 错误的 provider"
echo "------------------------------------------"
curl -s -X POST "${BASE_URL}/api/v1/article/generate" \
  -H "Content-Type: application/json" \
  -H "x-api-key: ${API_KEY}" \
  -d '{
    "prompt": "测试",
    "provider": "invalid"
  }' | python3 -m json.tool
echo ""

echo "=========================================="
echo "  全部测试完成！"
echo "=========================================="
