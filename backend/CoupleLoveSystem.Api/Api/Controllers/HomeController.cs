using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/home")]
[Authorize]
public class HomeController : BaseController
{
    private readonly HomeService _home;
    public HomeController(HomeService home) => _home = home;

    [HttpGet("loveinfo")]
    public async Task<ActionResult<ApiResult<LoveInfoDto>>> LoveInfo(CancellationToken ct) =>
        Ok(ApiResult<LoveInfoDto>.Ok(await _home.GetLoveInfoAsync(CurrentUserId, ct)));

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResult<DashboardDataDto>>> Dashboard(CancellationToken ct) =>
        Ok(ApiResult<DashboardDataDto>.Ok(await _home.GetDashboardAsync(CurrentUserId, ct)));

    [HttpGet("nearestanniversary")]
    public async Task<ActionResult<ApiResult<List<AnniversaryDto>>>> Nearest([FromQuery] int take = 3, CancellationToken ct = default) =>
        Ok(ApiResult<List<AnniversaryDto>>.Ok(await _home.GetNearestAsync(take, ct)));
}
