using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/quote")]
[Authorize]
public class QuoteController : BaseController
{
    private readonly QuoteService _svc;
    public QuoteController(QuoteService svc) => _svc = svc;

    [HttpGet("today")]
    public async Task<ActionResult<ApiResult<DailyQuoteDto>>> Today(CancellationToken ct = default) =>
        Ok(ApiResult<DailyQuoteDto>.Ok(await _svc.GetDailyAsync(ct)));
}
