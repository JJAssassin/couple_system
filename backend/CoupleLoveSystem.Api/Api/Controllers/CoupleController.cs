using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/couple")]
[Authorize]
public class CoupleController : BaseController
{
    private readonly CoupleService _svc;
    public CoupleController(CoupleService svc) => _svc = svc;

    [HttpGet("setting")]
    public async Task<ActionResult<ApiResult<CoupleSettingDto>>> Setting(CancellationToken ct) =>
        Ok(ApiResult<CoupleSettingDto>.Ok(await _svc.GetSettingAsync(ct)));

    [HttpPost("lovestart")]
    public async Task<ActionResult<ApiResult<CoupleSettingDto>>> LoveStart([FromBody] SetLoveStartReq req, CancellationToken ct) =>
        Ok(ApiResult<CoupleSettingDto>.Ok(await _svc.SetLoveStartAsync(req.LoveStartTime, CurrentUserId, ct)));

    [HttpPut("setting")]
    public async Task<ActionResult<ApiResult<CoupleSettingDto>>> Update([FromBody] UpdateCoupleSettingReq req, CancellationToken ct) =>
        Ok(ApiResult<CoupleSettingDto>.Ok(await _svc.UpdateSettingAsync(req, CurrentUserId, ct)));
}
