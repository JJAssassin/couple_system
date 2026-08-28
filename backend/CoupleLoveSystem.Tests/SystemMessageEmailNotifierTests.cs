using System.Threading;
using System.Threading.Tasks;
using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Infrastructure.Email;
using CoupleLoveSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoupleLoveSystem.Tests;

public class SystemMessageEmailNotifierTests
{
    private sealed class FakeEmailSender : IEmailSender
    {
        public bool Enabled { get; set; } = true;
        public List<(string To, string Subject, string Body)> Sent { get; } = new();
        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            Sent.Add((to, subject, htmlBody));
            return Task.CompletedTask;
        }
    }

    private static CoupleDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<CoupleDbContext>()
            .UseInMemoryDatabase("email-notify-" + System.Guid.NewGuid())
            .Options;
        return new CoupleDbContext(options);
    }

    [Fact]
    public async Task 接收人有邮箱_且启用_则发往正确收件人()
    {
        using var db = NewDb();
        db.Users.Add(new CoupleUser { Id = 1, UserName = "u1", NickName = "TA", Email = "ta@example.com", CoupleId = "c1" });
        await db.SaveChangesAsync();

        var sender = new FakeEmailSender { Enabled = true };
        var notifier = new SystemMessageEmailNotifier(db, sender);
        var msg = new CoupleSystemMessage
        {
            ReceiverUserId = 1,
            Title = "纪念日提醒",
            Content = "还有 3 天就到啦",
            MessageType = MessageType.Anniversary,
            IsRead = false,
            CreateUserId = 1
        };

        await notifier.NotifyAsync(msg);

        Assert.Single(sender.Sent);
        Assert.Equal("ta@example.com", sender.Sent[0].To);
        Assert.Equal("纪念日提醒", sender.Sent[0].Subject);
    }

    [Fact]
    public async Task 接收人无邮箱_则不发()
    {
        using var db = NewDb();
        db.Users.Add(new CoupleUser { Id = 1, UserName = "u1", NickName = "TA", Email = null, CoupleId = "c1" });
        await db.SaveChangesAsync();

        var sender = new FakeEmailSender { Enabled = true };
        var notifier = new SystemMessageEmailNotifier(db, sender);
        await notifier.NotifyAsync(new CoupleSystemMessage { ReceiverUserId = 1, Title = "x", Content = "y", MessageType = MessageType.Other });

        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task 未启用_则不发()
    {
        using var db = NewDb();
        db.Users.Add(new CoupleUser { Id = 1, UserName = "u1", NickName = "TA", Email = "ta@example.com", CoupleId = "c1" });
        await db.SaveChangesAsync();

        var sender = new FakeEmailSender { Enabled = false };
        var notifier = new SystemMessageEmailNotifier(db, sender);
        await notifier.NotifyAsync(new CoupleSystemMessage { ReceiverUserId = 1, Title = "x", Content = "y", MessageType = MessageType.Other });

        Assert.Empty(sender.Sent);
    }
}
