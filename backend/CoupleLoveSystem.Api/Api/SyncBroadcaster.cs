using CoupleLoveSystem.Api.Hubs;
using CoupleLoveSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;

namespace CoupleLoveSystem.Api;

/// <summary>单条增量变更：kind ∈ created / updated / deleted / reload；id 为变更实体的主键（reload 时为 null）。
/// Payload 为变更实体的标量投影（仅基础类型/字符串/枚举/日期/decimal，剔除导航与集合），供前端就地 upsert；reload / deleted 时为 null。</summary>
public sealed record SyncChange(string Kind, long? Id, object? Payload = null);

/// <summary>
/// 实时同步信号：携带模块名与该模块在一次保存中的全部增量变更。
/// 前端据此做局部刷新（有 id）或全量重载（kind=reload）。
/// </summary>
public sealed record SyncSignal(string Module, IReadOnlyList<SyncChange> Changes);

/// <summary>封装 SignalR 广播：向「当前写操作的情侣」组推送"数据已变更"通知，驱动前端实时刷新。</summary>
public class SyncBroadcaster
{
    private readonly IHubContext<SyncHub> _hub;
    public SyncBroadcaster(IHubContext<SyncHub> hub) => _hub = hub;

    /// <summary>向当前情侣组推送"数据已变更"通知（全量重载语义）。fire-and-forget 且吞掉异常：
    /// 实时广播失败（无人连接 / Hub 未就绪）绝不应影响业务写操作本身。
    /// 目标组由 CoupleContext.Current（本次请求的情侣空间）决定，确保只推给同一对情侣，杜绝跨情侣串台。</summary>
    public async Task NotifyAsync(string module, CancellationToken ct = default)
        => await NotifySignalAsync(new SyncSignal(module, new[] { new SyncChange("reload", null) }), ct: ct);

    /// <summary>向情侣组推送结构化增量信号（携带 eventType 与 id）。fire-and-forget 且吞掉异常。
    /// coupleId 优先于 CoupleContext.Current：拦截器在异步阶段调用时传入保存时捕获的 OperatingCoupleId，
    /// 避免 AsyncLocal 在 EF 内部 await 续体丢失导致误推到 anon 组（前端收不到实时更新）。</summary>
    public async Task NotifySignalAsync(SyncSignal signal, string? coupleId = null, CancellationToken ct = default)
    {
        try
        {
            var cid = coupleId ?? CoupleContext.Current;
            await _hub.Clients.Group(SyncHub.GroupForCouple(cid)).SendAsync("Sync", signal, ct);
        }
        catch
        {
            // 忽略广播异常，保证主流程不受影响
        }
    }
}
