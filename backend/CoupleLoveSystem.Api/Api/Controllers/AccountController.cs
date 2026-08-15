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
        Ok(ApiResult<PagedResult<AccountRecordDto>>.Ok(await _svc.ListAsync(page, pageSize, CurrentUserId, ct)));

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
}
