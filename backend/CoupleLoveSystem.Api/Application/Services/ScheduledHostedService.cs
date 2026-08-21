using CoupleLoveSystem.Api;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoupleLoveSystem.Application.Services;

/// <summary>
/// 定时任务托管服务（替代设计文档中的独立 Quartz 项目，零额外依赖、随 API 生命周期）。
/// 每分钟轮询一次：① 解锁到达时间的定时私密留言并生成通知；② 触发到期纪念日提醒并重新装填下次提醒时间。
/// 所有时间以服务器时间(UTC)为准，绝不信任前端传递的时间。
/// </summary>
public class ScheduledHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledHostedService> _logger;
    private readonly IDistributedJobLock _lock;
    private Timer? _timer;

    public ScheduledHostedService(IServiceScopeFactory scopeFactory, ILogger<ScheduledHostedService> logger, IDistributedJobLock jobLock)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _lock = jobLock;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 启动即跑一次，之后每分钟一轮（小数据量足够；大数据量改为每日固定时刻）
        _timer = new Timer(_ => _ = RunJobsAsync(stoppingToken), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        return Task.CompletedTask;
    }

    private async Task RunJobsAsync(CancellationToken ct)
    {
        // 防重入 + 跨实例互斥：抢不到锁说明本实例上一轮仍在跑，或别的实例正在跑，直接跳过本轮。
        if (!await _lock.TryAcquireAsync(TimeSpan.FromMinutes(2), ct))
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CoupleDbContext>();
            var sync = scope.ServiceProvider.GetRequiredService<SyncBroadcaster>();
            var notifier = scope.ServiceProvider.GetRequiredService<SystemMessageEmailNotifier>();
            var notify = new System.Collections.Generic.HashSet<string>();
            var toNotify = new System.Collections.Generic.List<CoupleSystemMessage>();
            var now = DateTime.UtcNow;

            // 1) 定时留言解锁（后台作业无 HTTP 上下文，忽略情侣空间过滤以处理全部情侣的到期留言）
            var locked = await db.BoardMessages.IgnoreQueryFilters()
                .Where(m => !m.IsDeleted && m.IsPrivate && m.ScheduledAt != null && m.ScheduledAt <= now && !m.IsUnlocked)
                .ToListAsync(ct);
            foreach (var msg in locked)
            {
                msg.IsUnlocked = true;
                if (msg.CoupleId != null) notify.Add(msg.CoupleId);
                var sysMsg = new CoupleSystemMessage
                {
                    ReceiverUserId = msg.ReceiverUserId ?? 0,
                    Title = "私密留言已解锁",
                    Content = "你有一条定时私密留言可以查看啦～",
                    MessageType = MessageType.Other,
                    IsRead = false,
                    CreateUserId = msg.CreateUserId,
                    CreateTime = now
                };
                db.SystemMessages.Add(sysMsg);
                toNotify.Add(sysMsg);
            }

            // 2) 纪念日提醒（双方都会收到；忽略情侣空间过滤，处理全部情侣的到期提醒）
            var due = await db.Anniversaries.IgnoreQueryFilters()
                .Where(a => !a.IsDeleted && a.NextRemindTime != null && a.NextRemindTime <= now)
                .ToListAsync(ct);
            foreach (var a in due)
            {
                // 按情侣空间查询真实成员，避免硬编码 1/2 导致非 demo 情侣收不到提醒
                var memberIds = await db.Users.IgnoreQueryFilters()
                    .Where(u => u.CoupleId == a.CoupleId)
                    .Select(u => u.Id)
                    .ToListAsync(ct);
                foreach (var uid in memberIds)
                {
                    var annMsg = new CoupleSystemMessage
                    {
                        ReceiverUserId = uid,
                        Title = "纪念日提醒",
                        Content = $"「{a.Name}」还有 {a.RemindDays} 天就要到啦",
                        MessageType = MessageType.Anniversary,
                        IsRead = false,
                        CreateUserId = a.CreateUserId,
                        CreateTime = now
                    };
                    db.SystemMessages.Add(annMsg);
                    toNotify.Add(annMsg);
                }
                if (a.CoupleId != null) notify.Add(a.CoupleId);
                // 重新装填下一次提醒时间：
                // - 每年重复：滚动到下一次周年（当前周年 +1 年）后减提前天数；
                // - 一次性：提醒过这一次后不再重复（置 null），避免过期纪念日被逐年推进无限刷屏。
                if (a.IsYearly)
                {
                    var occ = a.ComputeNextOccurrence()!.Value; // 本次即将/刚发生的周年
                    var nextOcc = new DateTime(occ.Year + 1, occ.Month, occ.Day);
                    a.NextRemindTime = nextOcc.AddDays(-a.RemindDays);
                }
                else
                {
                    a.NextRemindTime = null;
                }
            }

            if (locked.Count + due.Count > 0)
            {
                await db.SaveChangesAsync(ct);
                // 实时推送：向该情侣组主动广播「message 模块有更新」，已连接客户端即时刷新未读角标，
                // 不再依赖前端被动轮询（消除「定时提醒靠前端被动刷新」的已知边界）。fire-and-forget。
                if (sync != null)
                {
                    foreach (var cid in notify)
                        await sync.NotifySignalAsync(new SyncSignal("message", new[] { new SyncChange("reload", null) }), cid, ct);
                }
                // 邮件通知（最佳努力）：在核心链路（落库 + 实时推送）完成之后再发，
                // 且各自包 try/catch，确保任何邮件失败都不会连累站内消息落库与实时推送。
                foreach (var m in toNotify)
                {
                    try { await notifier.NotifyAsync(m, ct); }
                    catch { /* 邮件异常已在 notifier/sender 内部记录，不影响主流程 */ }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "定时任务执行失败");
        }
        finally
        {
            await _lock.ReleaseAsync(ct);
        }
    }

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
