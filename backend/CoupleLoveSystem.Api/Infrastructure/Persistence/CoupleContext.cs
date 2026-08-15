using System.Threading;

namespace CoupleLoveSystem.Infrastructure.Persistence;

/// <summary>
/// 当前请求所属「情侣空间」的异步流局部存储。
/// 由 <see cref="CoupleScopeMiddleware"/> 在每个 HTTP 请求中根据 JWT 的 <c>cid</c> 声明写入，
/// 请求结束时清空。全局查询过滤器与 SaveChanges 拦截器都读取它来实现按情侣隔离，
/// 使用 AsyncLocal 可确保同一逻辑请求内的异步流转都能正确取到值，且不会串到其它请求。
/// </summary>
public static class CoupleContext
{
    private static readonly AsyncLocal<string?> _current = new();

    /// <summary>当前情侣空间标识；未绑定/匿名请求为 null。</summary>
    public static string? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
