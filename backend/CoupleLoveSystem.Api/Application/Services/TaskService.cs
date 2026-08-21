using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Application.Services;

/// <summary>情侣任务/打卡模块：任务模板 + 完成记录 + 连续打卡 + 积分激励。</summary>
public class TaskService
{
    private readonly ITaskRepository _taskRepo;
    private readonly IRepository<CoupleTaskRecord> _recordRepo;
    private readonly IRepository<CoupleUser> _userRepo;

    public TaskService(ITaskRepository taskRepo, IRepository<CoupleTaskRecord> recordRepo, IRepository<CoupleUser> userRepo)
    {
        _taskRepo = taskRepo;
        _recordRepo = recordRepo;
        _userRepo = userRepo;
    }

    // —— 任务模板 ——
    public async Task<PagedResult<TaskTemplateDto>> ListTemplatesAsync(int page, int pageSize, bool? isActive = null, CancellationToken ct = default)
    {
        var paged = await _taskRepo.PagedTemplatesAsync(page, pageSize, isActive, ct);
        var items = paged.Items.Select(t => MapTemplate(t)).ToList();
        return new PagedResult<TaskTemplateDto> { Items = items, Total = paged.Total, Page = paged.Page, PageSize = paged.PageSize };
    }

    public async Task<TaskTemplateDto> CreateTemplateAsync(TaskTemplateReq req, long currentUserId, CancellationToken ct = default)
    {
        var t = new CoupleTaskTemplate
        {
            Title = req.Title,
            Description = req.Description,
            Icon = req.Icon,
            Points = req.Points > 0 ? req.Points : 10,
            TaskType = req.TaskType,
            Frequency = req.Frequency,
            IsActive = true,
            CreateUserId = currentUserId,
            CreateTime = DateTime.UtcNow,
        };
        await _taskRepo.AddAsync(t, ct);
        await _taskRepo.SaveChangesAsync(ct);
        return MapTemplate(t);
    }

    public async Task<TaskTemplateDto> UpdateTemplateAsync(long id, TaskTemplateReq req, long currentUserId, CancellationToken ct = default)
    {
        var t = await _taskRepo.GetByIdAsync(id, ct) ?? throw new NotFoundException("任务模板不存在");
        t.Title = req.Title;
        t.Description = req.Description;
        t.Icon = req.Icon;
        t.Points = req.Points > 0 ? req.Points : 10;
        t.TaskType = req.TaskType;
        t.Frequency = req.Frequency;
        t.UpdateUserId = currentUserId;
        t.UpdateTime = DateTime.UtcNow;
        _taskRepo.Update(t);
        await _taskRepo.SaveChangesAsync(ct);
        return MapTemplate(t);
    }

    public async Task ToggleTemplateAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var t = await _taskRepo.GetByIdAsync(id, ct) ?? throw new NotFoundException("任务模板不存在");
        t.IsActive = !t.IsActive;
        t.UpdateUserId = currentUserId;
        t.UpdateTime = DateTime.UtcNow;
        _taskRepo.Update(t);
        await _taskRepo.SaveChangesAsync(ct);
    }

    public async Task DeleteTemplateAsync(long id, long currentUserId, CancellationToken ct = default)
    {
        var t = await _taskRepo.GetByIdAsync(id, ct) ?? throw new NotFoundException("任务模板不存在");
        _taskRepo.SoftDelete(t);
        await _taskRepo.SaveChangesAsync(ct);
    }

    // —— 任务打卡 ——
    public async Task<TaskRecordDto> CheckInAsync(long templateId, long currentUserId, string? remark, CancellationToken ct = default)
    {
        var tpl = await _taskRepo.GetByIdAsync(templateId, ct) ?? throw new NotFoundException("任务模板不存在");
        if (!tpl.IsActive) throw new ConflictException("该任务已停用");

        // 检查今天是否已打卡（同一模板同一用户同一天只记一次）
        var today = DateTime.UtcNow.Date;
        var already = await _recordRepo.Query()
            .FirstOrDefaultAsync(r => r.TemplateId == templateId && r.UserId == currentUserId && r.CompleteDate == today, ct);
        if (already != null) throw new ConflictException("今天已经打卡过啦");

        var record = new CoupleTaskRecord
        {
            TemplateId = templateId,
            UserId = currentUserId,
            CompleteDate = today,
            EarnedPoints = tpl.Points,
            Remark = remark,
            CreateUserId = currentUserId,
            CreateTime = DateTime.UtcNow,
        };
        await _recordRepo.AddAsync(record, ct);
        await _recordRepo.SaveChangesAsync(ct);
        return MapRecord(record, tpl.Title, tpl.Icon);
    }

    public async Task CancelCheckInAsync(long recordId, long currentUserId, CancellationToken ct = default)
    {
        var r = await _recordRepo.GetByIdAsync(recordId, ct) ?? throw new NotFoundException("打卡记录不存在");
        if (r.UserId != currentUserId) throw new ForbiddenException("只能撤销自己的打卡");
        _recordRepo.SoftDelete(r);
        await _recordRepo.SaveChangesAsync(ct);
    }

    // —— 统计 ——
    public async Task<TaskStatsDto> GetStatsAsync(long currentUserId, CancellationToken ct = default)
    {
        var records = await _recordRepo.Query()
            .Where(r => r.UserId == currentUserId && !r.IsDeleted)
            .OrderByDescending(r => r.CompleteDate)
            .ToListAsync(ct);

        var totalPoints = records.Sum(r => r.EarnedPoints);
        var totalCheckIns = records.Count;
        var templates = await _taskRepo.Query().Where(t => t.IsActive).ToListAsync(ct);
        var activeCount = templates.Count;

        // 连续打卡天数：从今天往回数，遇到断档即停止
        var streak = 0;
        var d = DateTime.UtcNow.Date;
        foreach (var r in records)
        {
            if (r.CompleteDate == d) { streak++; d = d.AddDays(-1); }
            else if (r.CompleteDate < d) break;
        }

        // 今日已打卡任务
        var today = DateTime.UtcNow.Date;
        var todayTemplates = records.Where(r => r.CompleteDate == today).Select(r => r.TemplateId).ToHashSet();

        return new TaskStatsDto
        {
            TotalPoints = totalPoints,
            TotalCheckIns = totalCheckIns,
            StreakDays = streak,
            ActiveTaskCount = activeCount,
            TodayCheckedInCount = todayTemplates.Count,
        };
    }

    public async Task<List<TaskRecordDto>> ListRecentAsync(long currentUserId, int take = 20, CancellationToken ct = default)
    {
        var records = await _recordRepo.Query()
            .Where(r => r.UserId == currentUserId && !r.IsDeleted)
            .OrderByDescending(r => r.CompleteDate)
            .Take(take)
            .ToListAsync(ct);

        var templateIds = records.Select(r => r.TemplateId).Distinct().ToArray();
        var templates = await _taskRepo.Query().Where(t => templateIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, ct);
        return records.Select(r => MapRecord(r, templates.GetValueOrDefault(r.TemplateId)?.Title, templates.GetValueOrDefault(r.TemplateId)?.Icon)).ToList();
    }

    private static TaskTemplateDto MapTemplate(CoupleTaskTemplate t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        Icon = t.Icon,
        Points = t.Points,
        TaskType = t.TaskType,
        Frequency = t.Frequency,
        IsActive = t.IsActive,
        CreateTime = t.CreateTime,
    };

    private static TaskRecordDto MapRecord(CoupleTaskRecord r, string? title, string? icon) => new()
    {
        Id = r.Id,
        TemplateId = r.TemplateId,
        TemplateTitle = title ?? "未知任务",
        TemplateIcon = icon,
        UserId = r.UserId,
        CompleteDate = r.CompleteDate,
        EarnedPoints = r.EarnedPoints,
        Remark = r.Remark,
        CreateTime = r.CreateTime,
    };
}
