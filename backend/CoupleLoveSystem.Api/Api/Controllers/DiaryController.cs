using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/diary")]
[Authorize]
public class DiaryController : BaseController
{
    private readonly DiaryService _svc;
    public DiaryController(DiaryService svc) => _svc = svc;

    [HttpGet("list")]
    public async Task<ActionResult<ApiResult<PagedResult<DiaryDto>>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? author = null, CancellationToken ct = default) =>
        Ok(ApiResult<PagedResult<DiaryDto>>.Ok(await _svc.ListAsync(page, pageSize, CurrentUserId, author, ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResult<DiaryDto>>> Get(long id, CancellationToken ct) =>
        Ok(ApiResult<DiaryDto>.Ok(await _svc.GetAsync(id, CurrentUserId, ct)));

    [HttpPost("create")]
    public async Task<ActionResult<ApiResult<DiaryDto>>> Create([FromBody] DiaryReq req, CancellationToken ct) =>
        Ok(ApiResult<DiaryDto>.Ok(await _svc.CreateAsync(req, CurrentUserId, ct)));

    [HttpPut("update")]
    public async Task<ActionResult<ApiResult<DiaryDto>>> Update([FromQuery] long id, [FromBody] DiaryReq req, CancellationToken ct) =>
        Ok(ApiResult<DiaryDto>.Ok(await _svc.UpdateAsync(id, req, CurrentUserId, ct)));

    [HttpDelete("delete")]
    public async Task<ActionResult<ApiResult<object>>> Delete([FromQuery] long id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, CurrentUserId, ct);
        return Ok(ApiResults.Ok(new { }, "已删除"));
    }

    [HttpGet("comment/list")]
    public async Task<ActionResult<ApiResult<List<DiaryCommentDto>>>> CommentList([FromQuery] long diaryId, CancellationToken ct) =>
        Ok(ApiResult<List<DiaryCommentDto>>.Ok(await _svc.ListCommentsAsync(diaryId, CurrentUserId, ct)));

    [HttpPost("comment/create")]
    public async Task<ActionResult<ApiResult<DiaryCommentDto>>> CommentCreate([FromBody] DiaryCommentReq req, CancellationToken ct) =>
        Ok(ApiResult<DiaryCommentDto>.Ok(await _svc.AddCommentAsync(req, CurrentUserId, ct)));
}
