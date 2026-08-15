using System.Collections.Concurrent;

namespace CoupleLoveSystem.Infrastructure.Realtime;

/// <summary>SignalR 连接 ↔ 用户/情侣 身份绑定（握手方案）。
/// 前端匿名建立 WebSocket，再携带 JWT 调 /api/sync/authenticate 上报 connectionId，
/// 后端在此登记并把它加入对应情侣组，从而彻底避免把 JWT 放在 ?access_token= 查询串（防令牌泄露 + 日志泄露）。
/// 单实例内存实现；多实例部署需改用 Redis 并接入 SignalR 背板（本 Demo 不展开）。</summary>
public interface IConnectionIdentityStore
{
    void Bind(string connectionId, long userId, string? coupleId);
    void Unbind(string connectionId);
    (long userId, string? coupleId)? TryGet(string connectionId);
    /// <summary>各情侣当前在线连接数快照（仅含已握手绑定的连接）。</summary>
    IReadOnlyDictionary<string, int> OnlineSnapshot();
}

public sealed class ConnectionIdentityStore : IConnectionIdentityStore
{
    private readonly ConcurrentDictionary<string, (long userId, string? coupleId)> _map = new();

    public void Bind(string connectionId, long userId, string? coupleId)
        => _map[connectionId] = (userId, coupleId);

    public void Unbind(string connectionId)
        => _map.TryRemove(connectionId, out _);

    public (long userId, string? coupleId)? TryGet(string connectionId)
        => _map.TryGetValue(connectionId, out var v) ? v : null;

    public IReadOnlyDictionary<string, int> OnlineSnapshot()
    {
        var d = new Dictionary<string, int>();
        foreach (var kv in _map)
        {
            // null/空归一为 "anon"，与 SyncHub.GroupForCouple 保持一致，避免组名冲突
            var cid = kv.Value.coupleId ?? "anon";
            d.TryGetValue(cid, out var n);
            d[cid] = n + 1;
        }
        return d;
    }
}
