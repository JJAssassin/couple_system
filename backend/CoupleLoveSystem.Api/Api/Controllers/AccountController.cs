using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/account")]
[Authorize]
public class AccountController : BaseController
{
    private readonly AccountService _svc;
    public AccountController(AccountService svc) => _svc = svc;

    [HttpGet("list")]
    public async Task<ActionResult<ApiResult<PagedResult<AccountRecordDto>>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(ApiResult<PagedResult<AccountRecordDto>>.Ok(await _svc.ListAsync(page, Math.Clamp(pageSize, 1, 100), CurrentUserId, ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResult<AccountRecordDto>>> Get(long id, CancellationToken ct = default) =>
        Ok(ApiResult<AccountRecordDto>.Ok(await _svc.GetAsync(id, CurrentUserId, ct)));

    [HttpPost("create")]
    public async Task<ActionResult<ApiResult<AccountRecordDto>>> Create([FromBody] AccountRecordReq req, CancellationToken ct = default) =>
        Ok(ApiResult<AccountRecordDto>.Ok(await _svc.CreateAsync(req, CurrentUserId, ct)));

    [HttpPut("update")]
    public async Task<ActionResult<ApiResult<AccountRecordDto>>> Update([FromQuery] long id, [FromBody] AccountRecordReq req, CancellationToken ct = default) =>
        Ok(ApiResult<AccountRecordDto>.Ok(await _svc.UpdateAsync(id, req, CurrentUserId, ct)));

    [HttpDelete("delete")]
    public async Task<ActionResult<ApiResult<object>>> Delete([FromQuery] long id, CancellationToken ct = default)
    {
        await _svc.DeleteAsync(id, CurrentUserId, ct);
        return Ok(ApiResults.Ok(new { }, "已删除"));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResult<AccountSummaryDto>>> Summary(CancellationToken ct = default) =>
        Ok(ApiResult<AccountSummaryDto>.Ok(await _svc.SummaryAsync(CurrentUserId, ct)));

    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResult<AccountStatisticsDto>>> Statistics(
        [FromQuery] int year, [FromQuery] int month, CancellationToken ct = default) =>
        Ok(ApiResult<AccountStatisticsDto>.Ok(await _svc.StatisticsAsync(year, month, CurrentUserId, ct)));

    /// <summary>导出某月账单为 CSV（UTF-8 BOM，Excel 直接打开）。返回文件流而非 ApiResult 包装。</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] int year, [FromQuery] int month, CancellationToken ct = default)
    {
        var records = await _svc.RecordsInMonthAsync(year, month, ct);
        var csv = AccountService.ExportCsv(year, month, records);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv; charset=utf-8", $"couple-account-{year:D4}-{month:D2}.csv");
    }
}
