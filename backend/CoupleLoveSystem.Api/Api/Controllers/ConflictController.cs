using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/conflict")]
[Authorize]
public class ConflictController : BaseController
{
    private readonly ConflictService _svc;
    public ConflictController(ConflictService svc) => _svc = svc;

    [HttpGet("list")]
    public async Task<ActionResult<ApiResult<PagedResult<ConflictDto>>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(ApiResult<PagedResult<ConflictDto>>.Ok(await _svc.ListAsync(page, Math.Clamp(pageSize, 1, 100), ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResult<ConflictDto>>> Get(long id, CancellationToken ct) =>
        Ok(ApiResult<ConflictDto>.Ok(await _svc.GetAsync(id, ct)));

    [HttpPost("create")]
    public async Task<ActionResult<ApiResult<ConflictDto>>> Create([FromBody] ConflictReq req, CancellationToken ct) =>
        Ok(ApiResult<ConflictDto>.Ok(await _svc.CreateAsync(req, CurrentUserId, ct)));

    [HttpPut("update")]
    public async Task<ActionResult<ApiResult<ConflictDto>>> Update([FromQuery] long id, [FromBody] ConflictReq req, CancellationToken ct) =>
        Ok(ApiResult<ConflictDto>.Ok(await _svc.UpdateAsync(id, req, CurrentUserId, ct)));

    [HttpDelete("delete")]
    public async Task<ActionResult<ApiResult<object>>> Delete([FromQuery] long id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, CurrentUserId, ct);
        return Ok(ApiResults.Ok(new { }, "已删除"));
    }
}
