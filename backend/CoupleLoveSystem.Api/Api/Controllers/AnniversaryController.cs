using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/anniversary")]
[Authorize]
public class AnniversaryController : BaseController
{
    private readonly AnniversaryService _svc;
    public AnniversaryController(AnniversaryService svc) => _svc = svc;

    [HttpGet("list")]
    public async Task<ActionResult<ApiResult<PagedResult<AnniversaryDto>>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(ApiResult<PagedResult<AnniversaryDto>>.Ok(await _svc.ListAsync(page, pageSize, CurrentUserId, ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResult<AnniversaryDto>>> Get(long id, CancellationToken ct) =>
        Ok(ApiResult<AnniversaryDto>.Ok(await _svc.GetAsync(id, CurrentUserId, ct)));

    [HttpPost("create")]
    public async Task<ActionResult<ApiResult<AnniversaryDto>>> Create([FromBody] AnniversaryReq req, CancellationToken ct) =>
        Ok(ApiResult<AnniversaryDto>.Ok(await _svc.CreateAsync(req, CurrentUserId, ct)));

    [HttpPut("update")]
    public async Task<ActionResult<ApiResult<AnniversaryDto>>> Update([FromQuery] long id, [FromBody] AnniversaryReq req, CancellationToken ct) =>
        Ok(ApiResult<AnniversaryDto>.Ok(await _svc.UpdateAsync(id, req, CurrentUserId, ct)));

    [HttpDelete("delete")]
    public async Task<ActionResult<ApiResult<object>>> Delete([FromQuery] long id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, CurrentUserId, ct);
        return Ok(ApiResults.Ok(new { }, "已删除"));
    }
}
