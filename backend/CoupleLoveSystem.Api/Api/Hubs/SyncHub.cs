using CoupleLoveSystem.Infrastructure.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CoupleLoveSystem.Api.Hubs;

/// <summary>实时同步中枢（握手认证方案，不再使用 ?access_token=）。
/// 前端匿名建立 WebSocket → 调 /api/sync/authenticate 上报 connectionId → 后端登记并加入对应情侣组 couple-{cid}。
/// 仅情侣组内互发 Presence / Sync，杜绝跨情侣串台，也避免 JWT 经 URL 泄露到浏览器历史 / 服务端日志 / 代理日志。</summary>
[AllowAnonymous]
public class SyncHub : Hub
{
    private readonly IConnectionIdentityStore _identities;
    public SyncHub(IConnectionIdentityStore identities) => _identities = identities;

    /// <summary>情侣组名；null/空也归一化，避免组名冲突。</summary>
    public static string GroupForCouple(string? coupleId) => $"couple-{coupleId ?? "anon"}";

    public override async Task OnConnectedAsync()
    {
        // 匿名连入，身份由 HTTP 握手接口建立；此处先发一次 Presence（此时尚无情侣归属）。
        await BroadcastPresence();
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? ex)
    {
        var bound = _identities.TryGet(Context.ConnectionId);
        _identities.Unbind(Context.ConnectionId);
        if (bound is not null)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupForCouple(bound.Value.coupleId));
        await BroadcastPresence();
        await base.OnDisconnectedAsync(ex);
    }

    /// <summary>心跳：客户端定时调用，刷新对方在线状态。</summary>
    public Task Ping() => BroadcastPresence();

    private async Task BroadcastPresence()
    {
        // 每个情侣分别统计在线连接数（>1 视为对方也在线），仅向其情侣组内推送。
        foreach (var (cid, count) in _identities.OnlineSnapshot())
        {
            var online = count > 1;
            await Clients.Group(GroupForCouple(cid)).SendAsync("Presence", new { online });
        }
    }
}
