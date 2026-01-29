using AiGeneratorApi.Interface;
using AiGeneratorApi.Model;
using AiGeneratorApi.Service;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);
Env.Load();
// 1. 绑定配置 (对应 appsettings.json)
builder.Services.Configure<AIConfig>(builder.Configuration.GetSection("AIConfig"));

// 2. 注册 Keyed Services (策略模式)
// "google" -> GeminiService
builder.Services.AddKeyedScoped<IAIService, GeminiService>("google");
// "openai" -> OpenAIService
builder.Services.AddKeyedScoped<IAIService, OpenAIService>("openai");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();