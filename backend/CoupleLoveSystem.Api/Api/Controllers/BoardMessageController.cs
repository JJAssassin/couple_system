using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/board")]
[Authorize]
public class BoardMessageController : BaseController
{
    private readonly BoardMessageService _svc;
    public BoardMessageController(BoardMessageService svc) => _svc = svc;

    [HttpGet("list")]
    public async Task<ActionResult<ApiResult<PagedResult<BoardMessageDto>>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 200, CancellationToken ct = default) =>
        Ok(ApiResult<PagedResult<BoardMessageDto>>.Ok(await _svc.ListAsync(page, Math.Clamp(pageSize, 1, 100), CurrentUserId, ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResult<BoardMessageDto>>> Get(long id, CancellationToken ct) =>
        Ok(ApiResult<BoardMessageDto>.Ok(await _svc.GetAsync(id, CurrentUserId, ct)));

    [HttpPost("create")]
    public async Task<ActionResult<ApiResult<BoardMessageDto>>> Create([FromBody] BoardMessageReq req, CancellationToken ct) =>
        Ok(ApiResult<BoardMessageDto>.Ok(await _svc.CreateAsync(req, CurrentUserId, ct)));

    [HttpPut("update")]
    public async Task<ActionResult<ApiResult<BoardMessageDto>>> Update([FromQuery] long id, [FromBody] BoardMessageReq req, CancellationToken ct) =>
        Ok(ApiResult<BoardMessageDto>.Ok(await _svc.UpdateAsync(id, req, CurrentUserId, ct)));

    [HttpDelete("delete")]
    public async Task<ActionResult<ApiResult<object>>> Delete([FromQuery] long id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, CurrentUserId, ct);
        return Ok(ApiResults.Ok(new { }, "已删除"));
    }

    [HttpPut("pin")]
    public async Task<ActionResult<ApiResult<BoardMessageDto>>> Pin([FromBody] BoardMessageIdReq req, CancellationToken ct) =>
        Ok(ApiResult<BoardMessageDto>.Ok(await _svc.PinAsync(req.Id, CurrentUserId, ct)));

    [HttpPost("reaction")]
    public async Task<ActionResult<ApiResult<BoardMessageDto>>> Reaction([FromBody] BoardReactionReq req, CancellationToken ct) =>
        Ok(ApiResult<BoardMessageDto>.Ok(await _svc.ToggleReactionAsync(req.Id, req.EmojiKey, CurrentUserId, ct)));
}
