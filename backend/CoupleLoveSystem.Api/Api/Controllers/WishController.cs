using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/wish")]
[Authorize]
public class WishController : BaseController
{
    private readonly WishService _svc;
    public WishController(WishService svc) => _svc = svc;

    [HttpGet("list")]
    public async Task<ActionResult<ApiResult<PagedResult<WishDto>>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        Ok(ApiResult<PagedResult<WishDto>>.Ok(await _svc.ListAsync(page, pageSize, CurrentUserId, ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResult<WishDto>>> Get(long id, CancellationToken ct) =>
        Ok(ApiResult<WishDto>.Ok(await _svc.GetAsync(id, CurrentUserId, ct)));

    [HttpPost("create")]
    public async Task<ActionResult<ApiResult<WishDto>>> Create([FromBody] WishReq req, CancellationToken ct) =>
        Ok(ApiResult<WishDto>.Ok(await _svc.CreateAsync(req, CurrentUserId, ct)));

    [HttpPut("update")]
    public async Task<ActionResult<ApiResult<WishDto>>> Update([FromQuery] long id, [FromBody] WishReq req, CancellationToken ct) =>
        Ok(ApiResult<WishDto>.Ok(await _svc.UpdateAsync(id, req, CurrentUserId, ct)));

    [HttpDelete("delete")]
    public async Task<ActionResult<ApiResult<object>>> Delete([FromQuery] long id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, CurrentUserId, ct);
        return Ok(ApiResults.Ok(new { }, "已删除"));
    }

    [HttpPut("claim")]
    public async Task<ActionResult<ApiResult<WishDto>>> Claim([FromBody] WishClaimReq req, CancellationToken ct) =>
        Ok(ApiResult<WishDto>.Ok(await _svc.ClaimAsync(req.Id, CurrentUserId, ct)));

    [HttpPut("complete")]
    public async Task<ActionResult<ApiResult<WishDto>>> Complete([FromBody] WishCompleteReq req, CancellationToken ct) =>
        Ok(ApiResult<WishDto>.Ok(await _svc.CompleteAsync(req, CurrentUserId, ct)));

    [HttpPost("reorder")]
    public async Task<ActionResult<ApiResult<object>>> Reorder([FromBody] WishReorderReq req, CancellationToken ct)
    {
        await _svc.ReorderAsync(req.Ids, CurrentUserId, ct);
        return Ok(ApiResults.Ok(new { }, "已更新顺序"));
    }
}
