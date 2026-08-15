using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/user")]
[Authorize]
public class UserController : BaseController
{
    private readonly UserService _svc;
    public UserController(UserService svc) => _svc = svc;

    [HttpPut("profile")]
    public async Task<ActionResult<ApiResult<UserProfileDto>>> Profile(
        [FromBody] UpdateProfileReq req, CancellationToken ct = default) =>
        Ok(ApiResult<UserProfileDto>.Ok(await _svc.UpdateProfileAsync(req, CurrentUserId, ct)));

    [HttpGet("export/alldata")]
    public async Task<ActionResult<ApiResult<ExportResp>>> ExportAllData(CancellationToken ct = default) =>
        Ok(ApiResult<ExportResp>.Ok(await _svc.ExportAsync(CurrentUserId, ct)));
}
