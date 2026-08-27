using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/album")]
[Authorize]
public class AlbumController : BaseController
{
    private readonly AlbumService _svc;
    public AlbumController(AlbumService svc) => _svc = svc;

    [HttpGet("list")]
    public async Task<ActionResult<ApiResult<PagedResult<AlbumDto>>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(ApiResult<PagedResult<AlbumDto>>.Ok(await _svc.ListAsync(page, Math.Clamp(pageSize, 1, 100), ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResult<AlbumDto>>> Get(long id, CancellationToken ct) =>
        Ok(ApiResult<AlbumDto>.Ok(await _svc.GetAsync(id, ct)));

    [HttpPost("create")]
    public async Task<ActionResult<ApiResult<AlbumDto>>> Create([FromBody] AlbumReq req, CancellationToken ct) =>
        Ok(ApiResult<AlbumDto>.Ok(await _svc.CreateAsync(req, CurrentUserId, ct)));

    [HttpPut("update")]
    public async Task<ActionResult<ApiResult<AlbumDto>>> Update([FromQuery] long id, [FromBody] AlbumReq req, CancellationToken ct) =>
        Ok(ApiResult<AlbumDto>.Ok(await _svc.UpdateAsync(id, req, CurrentUserId, ct)));

    [HttpDelete("delete")]
    public async Task<ActionResult<ApiResult<object>>> Delete([FromQuery] long id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return Ok(ApiResults.Ok(new { }, "已删除"));
    }

    [HttpGet("image/list")]
    public async Task<ActionResult<ApiResult<List<ImageDto>>>> ImageList([FromQuery] long albumId, CancellationToken ct) =>
        Ok(ApiResult<List<ImageDto>>.Ok(await _svc.ListImagesAsync(albumId, ct)));
}
