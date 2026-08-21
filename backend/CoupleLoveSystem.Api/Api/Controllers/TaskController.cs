using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/task")]
[Authorize]
public class TaskController : BaseController
{
    private readonly TaskService _svc;
    public TaskController(TaskService svc) => _svc = svc;

    /// <summary>任务模板列表（分页）</summary>
    [HttpGet("templates")]
    public async Task<ActionResult<ApiResult<PagedResult<TaskTemplateDto>>>> ListTemplates(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] bool? isActive = null, CancellationToken ct = default) =>
        Ok(ApiResult<PagedResult<TaskTemplateDto>>.Ok(await _svc.ListTemplatesAsync(page, pageSize, isActive, ct)));

    /// <summary>创建任务模板</summary>
    [HttpPost("templates")]
    public async Task<ActionResult<ApiResult<TaskTemplateDto>>> CreateTemplate([FromBody] TaskTemplateReq req, CancellationToken ct) =>
        Ok(ApiResult<TaskTemplateDto>.Ok(await _svc.CreateTemplateAsync(req, CurrentUserId, ct)));

    /// <summary>更新任务模板</summary>
    [HttpPut("templates/{id:long}")]
    public async Task<ActionResult<ApiResult<TaskTemplateDto>>> UpdateTemplate(long id, [FromBody] TaskTemplateReq req, CancellationToken ct) =>
        Ok(ApiResult<TaskTemplateDto>.Ok(await _svc.UpdateTemplateAsync(id, req, CurrentUserId, ct)));

    /// <summary>启用 / 停用任务模板</summary>
    [HttpPut("templates/{id:long}/toggle")]
    public async Task<ActionResult<ApiResult<object>>> ToggleTemplate(long id, CancellationToken ct)
    {
        await _svc.ToggleTemplateAsync(id, CurrentUserId, ct);
        return Ok(ApiResults.Ok(new { }, "已更新"));
    }

    /// <summary>删除任务模板</summary>
    [HttpDelete("templates/{id:long}")]
    public async Task<ActionResult<ApiResult<object>>> DeleteTemplate(long id, CancellationToken ct)
    {
        await _svc.DeleteTemplateAsync(id, CurrentUserId, ct);
        return Ok(ApiResults.Ok(new { }, "已删除"));
    }

    /// <summary>打卡</summary>
    [HttpPost("checkin")]
    public async Task<ActionResult<ApiResult<TaskRecordDto>>> CheckIn([FromBody] TaskRecordReq req, CancellationToken ct) =>
        Ok(ApiResult<TaskRecordDto>.Ok(await _svc.CheckInAsync(req.TemplateId, CurrentUserId, req.Remark, ct)));

    /// <summary>撤销打卡</summary>
    [HttpDelete("records/{recordId:long}")]
    public async Task<ActionResult<ApiResult<object>>> CancelCheckIn(long recordId, CancellationToken ct)
    {
        await _svc.CancelCheckInAsync(recordId, CurrentUserId, ct);
        return Ok(ApiResults.Ok(new { }, "已撤销"));
    }

    /// <summary>我的任务统计</summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResult<TaskStatsDto>>> Stats(CancellationToken ct) =>
        Ok(ApiResult<TaskStatsDto>.Ok(await _svc.GetStatsAsync(CurrentUserId, ct)));

    /// <summary>最近打卡记录</summary>
    [HttpGet("records")]
    public async Task<ActionResult<ApiResult<List<TaskRecordDto>>>> Recent([FromQuery] int take = 20, CancellationToken ct = default) =>
        Ok(ApiResult<List<TaskRecordDto>>.Ok(await _svc.ListRecentAsync(CurrentUserId, take, ct)));
}
