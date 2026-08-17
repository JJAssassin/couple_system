using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/budget")]
[Authorize]
public class BudgetController : BaseController
{
    private readonly BudgetService _svc;
    public BudgetController(BudgetService svc) => _svc = svc;

    [HttpGet("monthly")]
    public async Task<ActionResult<ApiResult<MonthlyBudgetDto>>> Monthly(
        [FromQuery] int year, [FromQuery] int month, CancellationToken ct = default) =>
        Ok(ApiResult<MonthlyBudgetDto>.Ok(await _svc.GetMonthlyAsync(year, month, ct)));

    [HttpGet("current")]
    public async Task<ActionResult<ApiResult<MonthlyBudgetDto>>> Current(CancellationToken ct = default) =>
        Ok(ApiResult<MonthlyBudgetDto>.Ok(await _svc.GetCurrentAsync(ct)));

    [HttpGet("list")]
    public async Task<ActionResult<ApiResult<List<BudgetDto>>>> List(
        [FromQuery] int year, [FromQuery] int month, CancellationToken ct = default) =>
        Ok(ApiResult<List<BudgetDto>>.Ok(await _svc.ListAsync(year, month, ct)));

    [HttpPost("set")]
    public async Task<ActionResult<ApiResult<BudgetDto>>> Set([FromBody] BudgetSetReq req, CancellationToken ct = default) =>
        Ok(ApiResult<BudgetDto>.Ok(await _svc.SetAsync(req, CurrentUserId, ct)));

    [HttpDelete("delete")]
    public async Task<ActionResult<ApiResult<object>>> Delete([FromQuery] long id, CancellationToken ct = default)
    {
        await _svc.DeleteAsync(id, CurrentUserId, ct);
        return Ok(ApiResults.Ok(new { }, "已删除"));
    }
}
