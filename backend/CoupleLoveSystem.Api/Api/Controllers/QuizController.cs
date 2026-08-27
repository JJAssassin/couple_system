using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/quiz")]
[Authorize]
public class QuizController : BaseController
{
    private readonly QuizService _svc;
    public QuizController(QuizService svc) => _svc = svc;

    // ---------- 题库 ----------

    [HttpGet("questions")]
    public async Task<ActionResult<ApiResult<List<QuizQuestionDto>>>> Questions(CancellationToken ct) =>
        Ok(ApiResult<List<QuizQuestionDto>>.Ok(await _svc.ListQuestionsAsync(ct)));

    [HttpPost("question/create")]
    public async Task<ActionResult<ApiResult<QuizQuestionDto>>> CreateQuestion([FromBody] QuizQuestionReq req, CancellationToken ct) =>
        Ok(ApiResult<QuizQuestionDto>.Ok(await _svc.CreateQuestionAsync(req, CurrentUserId, ct)));

    [HttpDelete("question/delete")]
    public async Task<ActionResult<ApiResult<object>>> DeleteQuestion([FromQuery] long id, CancellationToken ct)
    {
        await _svc.DeleteQuestionAsync(id, CurrentUserId, ct);
        return Ok(ApiResults.Ok(new { }, "已删除"));
    }

    // ---------- 对局 ----------

    [HttpGet("rounds")]
    public async Task<ActionResult<ApiResult<PagedResult<QuizRoundDto>>>> Rounds(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken ct = default) =>
        Ok(ApiResult<PagedResult<QuizRoundDto>>.Ok(await _svc.ListRoundsAsync(page, Math.Clamp(pageSize, 1, 100), CurrentUserId, ct)));

    [HttpGet("round/{id:long}")]
    public async Task<ActionResult<ApiResult<QuizRoundDto>>> Round(long id, CancellationToken ct) =>
        Ok(ApiResult<QuizRoundDto>.Ok(await _svc.GetRoundAsync(id, CurrentUserId, ct)));

    [HttpPost("start")]
    public async Task<ActionResult<ApiResult<QuizRoundDto>>> Start([FromBody] QuizStartReq req, CancellationToken ct) =>
        Ok(ApiResult<QuizRoundDto>.Ok(await _svc.StartRoundAsync(req, CurrentUserId, ct)));

    [HttpPut("answer")]
    public async Task<ActionResult<ApiResult<QuizRoundDto>>> Answer([FromBody] QuizAnswerReq req, CancellationToken ct) =>
        Ok(ApiResult<QuizRoundDto>.Ok(await _svc.AnswerAsync(req, CurrentUserId, ct)));

    [HttpDelete("round/delete")]
    public async Task<ActionResult<ApiResult<object>>> DeleteRound([FromQuery] long id, CancellationToken ct)
    {
        await _svc.DeleteRoundAsync(id, CurrentUserId, ct);
        return Ok(ApiResults.Ok(new { }, "已删除"));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResult<QuizStatsDto>>> Stats(CancellationToken ct) =>
        Ok(ApiResult<QuizStatsDto>.Ok(await _svc.GetStatsAsync(CurrentUserId, ct)));
}
