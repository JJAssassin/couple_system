using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CoupleLoveSystem.Api.Middlewares;

/// <summary>
/// 安全响应头中间件：补齐 HSTS / CSP / X-Frame-Options / X-Content-Type-Options 等基线安全头。
/// 放在静态文件之后、认证之前，确保所有动态响应都携带。
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private const string HstsValue = "max-age=31536000; includeSubDomains; preload";
    private const string XFrameOptionsValue = "DENY";
    private const string XContentTypeOptionsValue = "nosniff";
    private const string ReferrerPolicyValue = "strict-origin-when-cross-origin";
    private const string PermissionsPolicyValue = "geolocation=(), microphone=(), camera=()";

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        // 跳过静态文件（由 UseStaticFiles 单独处理，避免缓存头冲突）
        if (!ctx.Request.Path.StartsWithSegments("/api") &&
            !ctx.Request.Path.StartsWithSegments("/hub") &&
            !ctx.Request.Path.StartsWithSegments("/uploads"))
        {
            await _next(ctx);
            return;
        }

        var headers = ctx.Response.Headers;

        // HSTS：仅 HTTPS 响应下发（生产由 Cloudflare/Caddy 终结 TLS，Kestrel 走 HTTP 时这里跳过避免自相矛盾）
        if (ctx.Request.IsHttps)
        {
            headers.Append("Strict-Transport-Security", HstsValue);
        }

        headers.Append("X-Frame-Options", XFrameOptionsValue);
        headers.Append("X-Content-Type-Options", XContentTypeOptionsValue);
        headers.Append("Referrer-Policy", ReferrerPolicyValue);
        headers.Append("Permissions-Policy", PermissionsPolicyValue);

        // CSP：API / Hub 不需要加载外部资源，直接禁止；前端由 nginx / Caddy 单独配置（含 static + inline）。
        // 这里仅对 /api 和 /hub 下发严格策略，防止把 API 响应当 HTML 执行。
        if (ctx.Request.Path.StartsWithSegments("/api") || ctx.Request.Path.StartsWithSegments("/hub"))
        {
            headers.Append("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'");
        }

        await _next(ctx);
    }
}
