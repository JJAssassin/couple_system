using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CoupleLoveSystem.Api;
using CoupleLoveSystem.Api.Hubs;
using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Core.Options;
using CoupleLoveSystem.Infrastructure.Email;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Redis;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// 验证「定时提醒升级」：后台作业触发到期纪念日提醒 / 定时留言解锁时，
/// 除写入 SystemMessage 外，还向该情侣的 SignalR 组 couple-{cid} 推送 message 模块增量信号，
/// 使已连接客户端即时刷新未读角标（不再依赖前端被动轮询）。
/// 同时验证接收人由 CoupleId 真实查询得到（非硬编码 1/2）。
/// </summary>
public class ScheduledHostedServiceTests
{
    private const string DbName = "scheduler-push-test";
    private const string Cid = "cid-push";

    [Fact]
    public async Task DueAnniversary_And_LockedBoard_Push_Message_Signal_To_CoupleGroup()
    {
        // 录制型 Hub：断言后台作业向正确情侣组推送了 message 模块信号
        var hub = new RecordingHubContext();

        var services = new ServiceCollection();
        services.AddSingleton<IHubContext<SyncHub>>(hub);
        services.AddScoped<SyncBroadcaster>();
        services.AddLogging();
        services.AddSingleton<IDistributedJobLock, InMemoryJobLock>();
        services.AddSingleton<ILogger<ScheduledHostedService>>(NullLogger<ScheduledHostedService>.Instance);
        // 邮件通知：测试用 NoOp（未启用），仅验证 SignalR 推送与消息落库，不触发真实 SMTP
        services.AddSingleton<IEmailSender, NoOpEmailSender>();
        services.Configure<EmailOptions>(o => o.Enabled = false);
        services.AddScoped<SystemMessageEmailNotifier>();
        services.AddDbContext<CoupleDbContext>(o => o
            .UseInMemoryDatabase(DbName)
            .ReplaceService<IModelCacheKeyFactory, CoupleModelCacheKeyFactory>());
        services.AddSingleton<ScheduledHostedService>();

        var sp = services.BuildServiceProvider();

        // 种子：一对情侣（两个用户）+ 一个到期纪念日 + 一条到期定时私密留言
        using (var seedScope = sp.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<CoupleDbContext>();
            db.Users.AddRange(
                new CoupleUser { Id = 11, CoupleId = Cid, UserName = "a", NickName = "A", PasswordHash = "x", LoveStartTime = DateTime.UtcNow },
                new CoupleUser { Id = 12, CoupleId = Cid, UserName = "b", NickName = "B", PasswordHash = "x", LoveStartTime = DateTime.UtcNow });
            db.Anniversaries.Add(new CoupleAnniversary
            {
                Name = "周年", CoupleId = Cid, IsYearly = true, RemindDays = 3,
                AnniversaryType = AnniversaryType.Custom, TargetDate = new DateTime(2020, 1, 1),
                NextRemindTime = DateTime.UtcNow.AddMinutes(-1), CreateUserId = 11
            });
            db.BoardMessages.Add(new CoupleBoardMessage
            {
                CoupleId = Cid, ReceiverUserId = 11, CreateUserId = 12,
                ScheduledAt = DateTime.UtcNow.AddMinutes(-1), IsUnlocked = false, IsPrivate = true,
                Content = "给未来的你"
            });
            await db.SaveChangesAsync(CancellationToken.None);
        }

        // 取出后台托管服务（单例），反射调用私有 RunJobsAsync（无 HTTP 上下文，CoupleContext.Current 为 null）
        var svc = sp.GetRequiredService<ScheduledHostedService>();
        var method = typeof(ScheduledHostedService).GetMethod("RunJobsAsync",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var task = (Task)method.Invoke(svc, new object[] { CancellationToken.None })!;
        await task;

        // 断言 1：向该情侣组推送了 message 模块信号（reload 语义）
        Assert.Contains("couple-" + Cid, hub.AllGroups);
        var sig = Assert.IsType<SyncSignal>(hub.LastArg);
        Assert.Equal("message", sig.Module);
        Assert.Equal("reload", sig.Changes[0].Kind);

        // 断言 2：DB 层系统消息写给了真实成员（非硬编码 1/2）
        using (var readScope = sp.CreateScope())
        {
            var db = readScope.ServiceProvider.GetRequiredService<CoupleDbContext>();
            var msgs = await db.SystemMessages.IgnoreQueryFilters().ToListAsync(CancellationToken.None);
            Assert.Equal(3, msgs.Count); // 1 私密留言解锁 + 2 纪念日成员
            Assert.All(msgs, m => Assert.True(m.ReceiverUserId == 11 || m.ReceiverUserId == 12, "接收人应为真实成员 id"));
            Assert.Contains(msgs, m => m.MessageType == MessageType.Other);
            Assert.Contains(msgs, m => m.MessageType == MessageType.Anniversary);

            // 断言 3：定时私密留言已解锁
            var board = await db.BoardMessages.IgnoreQueryFilters().FirstAsync(CancellationToken.None);
            Assert.True(board.IsUnlocked);

            // 断言 4：年度纪念日已重新装填下次提醒时间（非空，避免无限刷屏）
            var ann = await db.Anniversaries.IgnoreQueryFilters().FirstAsync(CancellationToken.None);
            Assert.NotNull(ann.NextRemindTime);
        }
    }
}
