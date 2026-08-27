using CoupleLoveSystem.Api.Hubs;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CoupleLoveSystem.Api.Controllers;

/// <summary>SignalR 握手认证：前端匿名连上 WebSocket 后，携带 JWT 上报 connectionId，
/// 后端据此登记身份并把该连接加入对应情侣组，从而无需把 JWT 放进 ?access_token= 查询串。</summary>
[ApiController]
[Route("api/sync")]
[Authorize]
public class SyncAuthController : ControllerBase
{
    private readonly IHubContext<SyncHub> _hub;
    private readonly IConnectionIdentityStore _identities;
    public SyncAuthController(IHubContext<SyncHub> hub, IConnectionIdentityStore identities)
        => (_hub, _identities) = (hub, identities);

    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate([FromBody] SyncAuthReq req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req?.ConnectionId))
            return BadRequest(ApiResults.Fail(ErrorCode.ParamInvalid, "connectionId 不能为空"));

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        var coupleId = User.FindFirst("cid")?.Value;
        if (!long.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        // 幂等重绑：先移除旧情侣组，再绑定新身份并加入新组。
        // 绑定/解绑后前端会拿重签的新令牌重新握手，使连接从 anon 组（或旧情侣组）迁移到最新情侣组，
        // 否则实时推送仍会落到旧组，导致「刚绑定却收不到对方实时更新」。
        var prev = _identities.TryGet(req.ConnectionId);
        if (prev is not null && prev.Value.coupleId != coupleId)
            await _hub.Groups.RemoveFromGroupAsync(req.ConnectionId, SyncHub.GroupForCouple(prev.Value.coupleId), ct);

        _identities.Bind(req.ConnectionId, userId, coupleId);
        await _hub.Groups.AddToGroupAsync(req.ConnectionId, SyncHub.GroupForCouple(coupleId), ct);
        return Ok(ApiResults.Ok(new { online = false }, "已绑定实时同步通道"));
    }

    [HttpPost("deauthenticate")]
    public async Task<IActionResult> Deauthenticate([FromBody] SyncAuthReq req, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(req?.ConnectionId))
        {
            var coupleId = _identities.TryGet(req.ConnectionId)?.coupleId ?? User.FindFirst("cid")?.Value;
            _identities.Unbind(req.ConnectionId);
            await _hub.Groups.RemoveFromGroupAsync(req.ConnectionId, SyncHub.GroupForCouple(coupleId), ct);
        }
        return Ok(ApiResults.Ok(new { }, "已解除实时同步通道"));
    }
}

public record SyncAuthReq(string? ConnectionId);
