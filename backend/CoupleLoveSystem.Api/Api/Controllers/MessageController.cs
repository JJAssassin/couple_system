using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/message")]
[Authorize]
public class MessageController : BaseController
{
    private readonly MessageService _svc;
    public MessageController(MessageService svc) => _svc = svc;

    [HttpGet("list")]
    public async Task<ActionResult<ApiResult<PagedResult<SystemMessageDto>>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(ApiResult<PagedResult<SystemMessageDto>>.Ok(await _svc.ListAsync(CurrentUserId, page, pageSize, ct)));

    [HttpGet("unread/count")]
    public async Task<ActionResult<ApiResult<int>>> UnreadCount(CancellationToken ct = default) =>
        Ok(ApiResult<int>.Ok(await _svc.UnreadCountAsync(CurrentUserId, ct)));

    [HttpPut("read")]
    public async Task<ActionResult<ApiResult<SystemMessageDto>>> Read(
        [FromBody] MessageReadReq req, CancellationToken ct = default) =>
        Ok(ApiResult<SystemMessageDto>.Ok(await _svc.ReadAsync(req.Id, CurrentUserId, ct)));

    [HttpPut("read/all")]
    public async Task<ActionResult<ApiResult<int>>> ReadAll(CancellationToken ct = default) =>
        Ok(ApiResult<int>.Ok(await _svc.ReadAllAsync(CurrentUserId, ct)));
}
