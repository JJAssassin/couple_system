using CoupleLoveSystem.Core.Result;
using System.Text.Json;

namespace CoupleLoveSystem.Api.Middlewares;

/// <summary>最外层异常中间件：把业务异常转成统一 ApiResult，绝不向客户端暴露原始堆栈。</summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next; _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            // 响应已经开始发送（流式、SignalR 升级、或已写出部分主体）→ 无法再改写状态码/主体，
            // 再写会抛 InvalidOperationException 把 500 变成进程级崩溃。仅记录日志后安全返回。
            if (ctx.Response.HasStarted)
            {
                _logger.LogError(ex, "未处理异常（响应已开始，无法改写）：{Path}", ctx.Request.Path);
                return;
            }

            int code = ex switch
            {
                ForbiddenException => ErrorCode.Forbidden,
                NotFoundException => ErrorCode.NotFound,
                ConflictException => ErrorCode.Conflict,
                UnauthorizedException => ErrorCode.Unauthorized,
                RateLimitedException => ErrorCode.TooManyRequests,
                _ => ErrorCode.ServerError
            };

            if (code == ErrorCode.ServerError)
                _logger.LogError(ex, "未处理异常：{Path}", ctx.Request.Path);

            // 服务端错误绝不向客户端泄露内部细节（表名/列名/路径等），仅返回通用文案；详细异常已记入日志。
            // 业务异常（Forbidden/NotFound/Conflict/Unauthorized/TooManyRequests）的 Message 为开发者可控文案，可原样返回。
            var safeMessage = code == ErrorCode.ServerError
                ? "服务器开小差了，请稍后再试"
                : ex.Message;

            try
            {
                ctx.Response.StatusCode = code;
                ctx.Response.ContentType = "application/json; charset=utf-8";
                var body = ApiResult<object>.Fail(code, safeMessage);
                await ctx.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }));
            }
            catch (Exception writeEx)
            {
                // 写出阶段异常（如客户端已断开）绝不向上抛，避免覆盖原始异常并引发次级崩溃
                _logger.LogError(writeEx, "异常响应写出失败：{Path}", ctx.Request.Path);
            }
        }
    }
}
