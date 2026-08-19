using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

/// <summary>恋爱数据统计：年度报告等聚合视图。</summary>
[Route("api/stats")]
[Authorize]
public class StatsController : BaseController
{
    private readonly YearReportService _yearReport;
    public StatsController(YearReportService yearReport) => _yearReport = yearReport;

    /// <summary>年度恋爱报告：GET /api/stats/yearreport?year=2026（缺省为当前年）</summary>
    [HttpGet("yearreport")]
    public async Task<ActionResult<ApiResult<YearReportDto>>> YearReport([FromQuery] int? year, CancellationToken ct)
    {
        var y = year ?? DateTime.UtcNow.Year;
        return Ok(ApiResult<YearReportDto>.Ok(await _yearReport.GetYearReportAsync(y, ct)));
    }
}
