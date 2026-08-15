using CoupleLoveSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;

namespace CoupleLoveSystem.Api;

/// <summary>
/// 在每个 HTTP 请求中，根据 JWT 的 <c>cid</c> 声明把"当前情侣空间"写入 <see cref="CoupleContext.Current"/>，
/// 供 DbContext 的全局查询过滤器与 SaveChanges 盖章逻辑使用；请求结束后清空，避免串到下一个请求。
/// 放在认证之后执行，确保 User.Claims 已就绪。
/// </summary>
public class CoupleScopeMiddleware
{
    private readonly RequestDelegate _next;
    public CoupleScopeMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var cid = context.User?.FindFirst("cid")?.Value;
        CoupleContext.Current = string.IsNullOrWhiteSpace(cid) ? null : cid;
        try
        {
            await _next(context);
        }
        finally
        {
            CoupleContext.Current = null;
        }
    }
}

/// <summary>中间件扩展方法，便于在 Program.cs 中以 <c>app.UseCoupleScope()</c> 注册。</summary>
public static class CoupleScopeMiddlewareExtensions
{
    public static IApplicationBuilder UseCoupleScope(this IApplicationBuilder app)
        => app.UseMiddleware<CoupleScopeMiddleware>();
}
