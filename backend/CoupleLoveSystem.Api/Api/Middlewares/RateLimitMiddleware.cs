using System.Security.Claims;
using System.Text.Json;
using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Result;

namespace CoupleLoveSystem.Api.Middlewares;

/// <summary>
/// 速率限制中间件（审计 P2-1/2/3）。按请求路径映射到策略，超限返回 429 + 统一 ApiResult 错误体
/// （与 GlobalExceptionMiddleware 风格一致）。置于认证之后，便于导出类端点按 JWT sub（用户）维度限速；
/// 匿名刷新端点按客户端 IP 限速。限速状态由 ApiRateLimiter 经 ICacheService 持久化（多实例共享）。
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;

    public RateLimitMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, ApiRateLimiter limiter)
    {
        var (policy, partition) = Resolve(ctx);
        if (policy != null)
        {
            if (!await limiter.TryAsync(policy, partition, ctx.RequestAborted))
            {
                if (ctx.Response.HasStarted) { await _next(ctx); return; }
                ctx.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                ctx.Response.ContentType = "application/json; charset=utf-8";
                var body = ApiResult<object>.Fail(ErrorCode.TooManyRequests, "请求过于频繁，请稍后再试");
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
                return;
            }
        }
        await _next(ctx);
    }

    /// <summary>将请求路径/方法映射到 (策略, 分区键)。未匹配限速的端点返回 (null, "") 放行。</summary>
    private static (string? Policy, string Partition) Resolve(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? string.Empty;
        var method = ctx.Request.Method;

        // P2-1：匿名刷新端点，按 IP 限速
        if (method == "POST" && path.Equals("/api/auth/refresh", StringComparison.OrdinalIgnoreCase))
            return ("refresh", ClientIp(ctx));

        // P2-2：绑定/加入，按 IP 限速
        if (method == "POST" && (path.Equals("/api/partner/invite", StringComparison.OrdinalIgnoreCase)
                              || path.Equals("/api/partner/join", StringComparison.OrdinalIgnoreCase)))
            return ("join", ClientIp(ctx));

        // P2-3：导出（完整 zip / CSV），资源消耗大，按用户（JWT sub）限速
        if ((method == "GET" && path.Equals("/api/user/export/alldata", StringComparison.OrdinalIgnoreCase))
         || (method == "GET" && path.Equals("/api/account/export", StringComparison.OrdinalIgnoreCase)))
            return ("export", UserId(ctx) ?? ClientIp(ctx));

        return (null, string.Empty);
    }

    private static string ClientIp(HttpContext ctx)
    {
        var ip = ctx.Request.Headers["CF-Connecting-IP"].ToString();
        if (string.IsNullOrWhiteSpace(ip)) ip = ctx.Request.Headers["X-Forwarded-For"].ToString();
        if (string.IsNullOrWhiteSpace(ip)) ip = ctx.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown";
        return ip;
    }

    private static string? UserId(HttpContext ctx)
    {
        var id = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return string.IsNullOrWhiteSpace(id) ? null : id;
    }
}
