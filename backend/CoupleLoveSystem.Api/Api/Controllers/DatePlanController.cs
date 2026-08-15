using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/daterecord")]
[Authorize]
public class DatePlanController : BaseController
{
    private readonly DatePlanService _svc;
    public DatePlanController(DatePlanService svc) => _svc = svc;

    [HttpGet("list")]
    public async Task<ActionResult<ApiResult<PagedResult<DateRecordDto>>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(ApiResult<PagedResult<DateRecordDto>>.Ok(await _svc.ListAsync(page, pageSize, ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResult<DateRecordDto>>> Get(long id, CancellationToken ct = default) =>
        Ok(ApiResult<DateRecordDto>.Ok(await _svc.GetAsync(id, ct)));

    [HttpPost("create")]
    public async Task<ActionResult<ApiResult<DateRecordDto>>> Create([FromBody] DateRecordReq req, CancellationToken ct = default) =>
        Ok(ApiResult<DateRecordDto>.Ok(await _svc.CreateAsync(req, CurrentUserId, ct)));

    [HttpPut("update")]
    public async Task<ActionResult<ApiResult<DateRecordDto>>> Update([FromQuery] long id, [FromBody] DateRecordReq req, CancellationToken ct = default) =>
        Ok(ApiResult<DateRecordDto>.Ok(await _svc.UpdateAsync(id, req, CurrentUserId, ct)));

    [HttpDelete("delete")]
    public async Task<ActionResult<ApiResult<object>>> Delete([FromQuery] long id, CancellationToken ct = default)
    {
        await _svc.DeleteAsync(id, CurrentUserId, ct);
        return Ok(ApiResults.Ok(new { }, "已删除"));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResult<DateStatsDto>>> Stats(CancellationToken ct = default) =>
        Ok(ApiResult<DateStatsDto>.Ok(await _svc.StatsAsync(ct)));
}
