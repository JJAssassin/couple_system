using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/timeline")]
[Authorize]
public class TimelineController : BaseController
{
    private readonly TimelineService _svc;
    public TimelineController(TimelineService svc) => _svc = svc;

    [HttpGet("list")]
    public async Task<ActionResult<ApiResult<List<TimelineItemDto>>>> List(
        [FromQuery] int? year = null, [FromQuery] int? month = null, CancellationToken ct = default) =>
        Ok(ApiResult<List<TimelineItemDto>>.Ok(await _svc.ListAsync(year, month, CurrentUserId, ct)));
}
