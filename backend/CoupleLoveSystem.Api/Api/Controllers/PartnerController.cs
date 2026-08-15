using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/partner")]
[Authorize]
public class PartnerController : BaseController
{
    private readonly PartnerService _svc;
    public PartnerController(PartnerService svc) => _svc = svc;

    [HttpGet("status")]
    public async Task<ActionResult<ApiResult<BindStatusDto>>> Status(CancellationToken ct) =>
        Ok(ApiResult<BindStatusDto>.Ok(await _svc.GetStatusAsync(CurrentUserId, ct)));

    [HttpPost("invite")]
    public async Task<ActionResult<ApiResult<InviteDto>>> Invite(CancellationToken ct) =>
        Ok(ApiResult<InviteDto>.Ok(await _svc.CreateInviteAsync(CurrentUserId, ct)));

    [HttpPost("join")]
    public async Task<ActionResult<ApiResult<PartnerInfoDto>>> Join([FromBody] JoinReq req, CancellationToken ct) =>
        Ok(ApiResult<PartnerInfoDto>.Ok(await _svc.JoinAsync(req.Code, CurrentUserId, ct)));

    [HttpPost("unbind")]
    public async Task<ActionResult<ApiResult<object>>> Unbind(CancellationToken ct)
    {
        await _svc.UnbindAsync(CurrentUserId, ct);
        return Ok(ApiResult<object>.Ok(new { }, "已解除绑定，你们的数据仍然保留"));
    }
}

public class JoinReq
{
    public string Code { get; set; } = string.Empty;
}
