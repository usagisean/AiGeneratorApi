# ===============================
# 1. 编译阶段
# ===============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 复制所有文件
COPY . .

# 还原依赖并发布
# 注意：这里会编译出 AiGeneratorApi.dll
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# ===============================
# 2. 运行阶段
# ===============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# 从编译阶段复制发布好的文件
COPY --from=build /app/publish .

# 创建一个空的 json 占位文件 (Action 会用真实的密钥覆盖它)
RUN echo "{}" > /app/gcp-key.json

# 暴露容器内部端口
EXPOSE 8080

# 启动命令
ENTRYPOINT ["dotnet", "AiGeneratorApi.dll"]