using System.Net;

namespace AiGeneratorApi.Middleware;

public class IpWhitelistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IpWhitelistMiddleware> _logger;

    public IpWhitelistMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<IpWhitelistMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var whitelistConfig = _configuration["IP_WHITELIST"];
        
        // 如果配置是 *，则允许所有人访问（开发模式）
        if (whitelistConfig == "*")
        {
            await _next(context);
            return;
        }

        var allowedIps = whitelistConfig?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
        
        // 获取客户端 IP (兼容 Docker/Nginx 转发)
        var remoteIp = context.Connection.RemoteIpAddress;
        
        // 尝试从 X-Forwarded-For 获取真实 IP (如果在 Nginx 后面)
        if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            var forwardedIp = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedIp) && IPAddress.TryParse(forwardedIp, out var ip))
            {
                remoteIp = ip;
            }
        }

        if (remoteIp != null)
        {
            // 将 IP 转为字符串比较
            var ipString = remoteIp.ToString();
            
            // 如果是 IPv6 本地回环，转为 IPv4 方便比较
            if (ipString == "::1") ipString = "127.0.0.1";

            // 检查是否在白名单中
            if (!allowedIps.Contains(ipString))
            {
                _logger.LogWarning($"[Security] 拦截未授权 IP: {ipString}");
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync($"Access Denied: Your IP {ipString} is not allowed.");
                return;
            }
        }

        await _next(context);
    }
}