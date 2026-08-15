using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CoupleLoveSystem.Api.Middlewares;

/// <summary>防御性脱敏：一旦某请求仍把令牌放进 ?access_token=，立即从查询串摘除，
/// 避免 Kestrel / 网关 / 反向代理把 JWT 写进访问日志。
/// 握手方案本身已不再使用 URL 令牌，此为纵深防御；置于认证中间件之前生效。</summary>
public class AccessTokenScrubMiddleware
{
    private readonly RequestDelegate _next;
    public AccessTokenScrubMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (ctx.Request.Query.ContainsKey("access_token"))
        {
            var kept = ctx.Request.Query
                .Where(kv => kv.Key != "access_token")
                .SelectMany(kv => kv.Value.Select(v => new KeyValuePair<string, string?>(kv.Key, v)));
            ctx.Request.QueryString = QueryString.Create(kept);
        }
        await _next(ctx);
    }
}
