using AiGeneratorApi.Interface;
using AiGeneratorApi.Middleware;
using AiGeneratorApi.Model;
using AiGeneratorApi.Service;
using DotNetEnv;
using Microsoft.OpenApi.Models;

// 1. 加载 .env 环境变量
// 支持从仓库根目录运行（./AiGeneratorApi/.env）或从项目目录运行（./.env）。
// 生产环境建议直接注入环境变量，不需要把 .env 打包进镜像或发布目录。
LoadLocalEnvFile();

var builder = WebApplication.CreateBuilder(args);

// 2. 注入配置 (绑定 appsettings.json 或环境变量到 AIConfig 类)
builder.Services.Configure<AIConfig>(builder.Configuration.GetSection("AIConfig"));

// 3. 配置 NewAPI HttpClient（OpenAI-compatible / LiteLLM）
var configuredNewApiBaseUrl = builder.Configuration["AIConfig:NewApi:BaseUrl"];
var newApiBaseUrl = string.IsNullOrWhiteSpace(configuredNewApiBaseUrl)
    ? NewApiModelDefaults.DefaultBaseUrl
    : configuredNewApiBaseUrl;
var newApiTimeoutSeconds = builder.Configuration.GetValue<int?>("AIConfig:NewApi:RequestTimeoutSeconds") ?? 300;
builder.Services.AddHttpClient("NewApiClient", client =>
{
    client.BaseAddress = new Uri(newApiBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(newApiTimeoutSeconds, 30, 600));
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AiGeneratorApi/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
});

// 4. 注册 AI 服务 (使用 KeyedService 实现多态)
// "newapi" -> NewApiService
builder.Services.AddKeyedScoped<IAIService, NewApiService>("newapi");

// "google" -> GeminiService
// 默认禁用，只在显式配置 AIConfig:Gemini:Enabled=true 时注册，避免生产默认依赖 Google。
var googleEnabled = builder.Configuration.GetValue<bool>("AIConfig:Gemini:Enabled");
if (googleEnabled)
{
    string? proxyUrl = builder.Configuration["AIConfig:Gemini:ProxyUrl"];
    builder.Services.AddHttpClient("GeminiClient", client =>
    {
        client.Timeout = TimeSpan.FromMinutes(5); // 设置超时防止长文生成中断
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        // 如果配置了代理地址 (本地开发)，则使用代理
        if (!string.IsNullOrEmpty(proxyUrl))
        {
            Console.WriteLine($"[System] Gemini 代理已启用: {proxyUrl}");
            return new HttpClientHandler
            {
                Proxy = new System.Net.WebProxy(proxyUrl),
                UseProxy = true
            };
        }

        // 否则直连 (VPS 环境)
        Console.WriteLine("[System] Gemini 直连模式 (无代理)");
        return new HttpClientHandler();
    });

    builder.Services.AddKeyedScoped<IAIService, GeminiService>("google");
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 枚举序列化为字符串（如 "news" 而非 0），且支持忽略大小写
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase)
        );
    });
builder.Services.AddEndpointsApiExplorer();

// 5. 配置 Swagger (支持在页面右上角输入 API Key)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AI Generator API", Version = "v1" });

    // 定义安全模式
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Set API Key",
        Name = "x-api-key", // 必须和中间件里的 header 名字一致
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    // 应用安全要求
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                },
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// =============================================================
// 中间件管道配置 (顺序极其重要！)
// =============================================================

// 【第一层】IP 白名单防御
// 最先执行，如果 IP 不在白名单，直接拒绝，连 Swagger 都看不了
app.UseMiddleware<IpWhitelistMiddleware>();

// 【第二层】Swagger 文档
// 放在 API Key 认证之前，允许浏览器直接访问文档页面
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// 如果你想在 VPS 生产环境也能看 Swagger，可以把上面的 if 判断去掉，
// 或者在 appsettings.json 里把 Environment 改为 Development

// 【第三层】API Key 认证 (业务鉴权)
// 只有通过了 IP 检查，且不是访问 Swagger页面的请求，才会走到这里检查密码
app.Use(async (context, next) =>
{
    // 从环境变量获取正确的密码
    var mySecretKey = Environment.GetEnvironmentVariable("MY_API_KEY");
    
    // 安全检查：如果服务器没配密码，拒绝所有服务
    if (string.IsNullOrEmpty(mySecretKey))
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Server Error: API Key not configured.");
        return;
    }

    // 检查请求头 x-api-key
    if (!context.Request.Headers.TryGetValue("x-api-key", out var extractedApiKey) || !string.Equals(extractedApiKey, mySecretKey))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized: Invalid or missing API Key.");
        return;
    }

    await next();
});

// =============================================================

// 【第四层】业务逻辑
app.UseAuthorization();
app.MapControllers();

app.Run();

static void LoadLocalEnvFile()
{
    var candidates = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        Path.Combine(Directory.GetCurrentDirectory(), "AiGeneratorApi", ".env"),
        Path.Combine(AppContext.BaseDirectory, ".env")
    };

    foreach (var envFile in candidates.Distinct())
    {
        if (!File.Exists(envFile))
        {
            continue;
        }

        Env.Load(envFile);
        break;
    }
}
