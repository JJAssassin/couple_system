using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/letter")]
[Authorize]
public class LetterController : BaseController
{
    private readonly LetterService _svc;
    public LetterController(LetterService svc) => _svc = svc;

    [HttpGet("list")]
    public async Task<ActionResult<ApiResult<List<LetterDto>>>> List(CancellationToken ct = default) =>
        Ok(ApiResult<List<LetterDto>>.Ok(await _svc.ListAsync(CurrentUserId, ct)));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ApiResult<LetterDto>>> Get(long id, CancellationToken ct) =>
        Ok(ApiResult<LetterDto>.Ok(await _svc.GetAsync(id, CurrentUserId, ct)));

    [HttpPost("create")]
    public async Task<ActionResult<ApiResult<LetterDto>>> Create([FromBody] LetterReq req, CancellationToken ct) =>
        Ok(ApiResult<LetterDto>.Ok(await _svc.CreateAsync(req, CurrentUserId, ct)));

    [HttpDelete("delete")]
    public async Task<ActionResult<ApiResult<object>>> Delete([FromQuery] long id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, CurrentUserId, ct);
        return Ok(ApiResults.Ok(new { }, "已删除"));
    }
}
