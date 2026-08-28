using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/todo")]
[Authorize]
public class TodoController : BaseController
{
    private readonly TodoService _svc;
    public TodoController(TodoService svc) => _svc = svc;

    [HttpGet("list")]
    public async Task<ActionResult<ApiResult<PagedResult<TodoDto>>>> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 200, CancellationToken ct = default) =>
        Ok(ApiResult<PagedResult<TodoDto>>.Ok(await _svc.ListAsync(page, pageSize, CurrentUserId, ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResult<TodoDto>>> Get(long id, CancellationToken ct) =>
        Ok(ApiResult<TodoDto>.Ok(await _svc.GetAsync(id, CurrentUserId, ct)));

    [HttpPost("create")]
    public async Task<ActionResult<ApiResult<TodoDto>>> Create([FromBody] TodoReq req, CancellationToken ct) =>
        Ok(ApiResult<TodoDto>.Ok(await _svc.CreateAsync(req, CurrentUserId, ct)));

    [HttpPut("update")]
    public async Task<ActionResult<ApiResult<TodoDto>>> Update([FromQuery] long id, [FromBody] TodoReq req, CancellationToken ct) =>
        Ok(ApiResult<TodoDto>.Ok(await _svc.UpdateAsync(id, req, CurrentUserId, ct)));

    [HttpDelete("delete")]
    public async Task<ActionResult<ApiResult<object>>> Delete([FromQuery] long id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, CurrentUserId, ct);
        return Ok(ApiResults.Ok(new { }, "已删除"));
    }

    [HttpPut("toggle")]
    public async Task<ActionResult<ApiResult<TodoDto>>> Toggle([FromBody] TodoIdReq req, CancellationToken ct) =>
        Ok(ApiResult<TodoDto>.Ok(await _svc.ToggleAsync(req.Id, CurrentUserId, ct)));

    [HttpPut("assign")]
    public async Task<ActionResult<ApiResult<TodoDto>>> Assign([FromBody] TodoAssignReq req, CancellationToken ct) =>
        Ok(ApiResult<TodoDto>.Ok(await _svc.AssignAsync(req, CurrentUserId, ct)));

    [HttpPost("reorder")]
    public async Task<ActionResult<ApiResult<object>>> Reorder([FromBody] TodoReorderReq req, CancellationToken ct)
    {
        await _svc.ReorderAsync(req.Ids, CurrentUserId, ct);
        return Ok(ApiResults.Ok(new { }, "已更新顺序"));
    }
}
