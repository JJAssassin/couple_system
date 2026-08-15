using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/footprint")]
[Authorize]
public class FootprintController : BaseController
{
    private readonly FootprintService _svc;
    public FootprintController(FootprintService svc) => _svc = svc;

    [HttpGet("list")]
    public async Task<ActionResult<ApiResult<List<FootprintDto>>>> List(CancellationToken ct = default) =>
        Ok(ApiResult<List<FootprintDto>>.Ok(await _svc.ListAsync(ct)));

    [HttpPost("create")]
    public async Task<ActionResult<ApiResult<FootprintDto>>> Create([FromBody] FootprintReq req, CancellationToken ct = default) =>
        Ok(ApiResult<FootprintDto>.Ok(await _svc.CreateAsync(req, CurrentUserId, ct)));

    [HttpDelete("delete")]
    public async Task<ActionResult<ApiResult<object>>> Delete([FromQuery] long id, CancellationToken ct = default)
    {
        await _svc.DeleteAsync(id, CurrentUserId, ct);
        return Ok(ApiResults.Ok(new { }, "已删除"));
    }

    [HttpPut("increment/{id:long}")]
    public async Task<ActionResult<ApiResult<FootprintDto>>> Increment(long id, CancellationToken ct = default) =>
        Ok(ApiResult<FootprintDto>.Ok(await _svc.IncrementAsync(id, CurrentUserId, ct)));

    [HttpPut("update/{id:long}")]
    public async Task<ActionResult<ApiResult<FootprintDto>>> Update(long id, [FromBody] FootprintReq req, CancellationToken ct = default) =>
        Ok(ApiResult<FootprintDto>.Ok(await _svc.UpdateAsync(id, req, CurrentUserId, ct)));
}
