using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;

namespace CoupleLoveSystem.Application.Services;

/// <summary>站内系统消息 → 邮件通知桥接：根据消息接收人(ReceiverUserId)查其邮箱，
/// 在 SMTP 启用且邮箱非空时发送一封浪漫主题的提醒邮件；无邮箱/未启用则安全跳过。
/// 依赖 CoupleDbContext（scoped），使用 IgnoreQueryFilters 以跨情侣空间正确解析接收人，
/// 不受后台托管服务等无 HTTP 上下文（CoupleContext.Current 为 null）影响。</summary>
public class SystemMessageEmailNotifier
{
    private readonly CoupleDbContext _db;
    private readonly IEmailSender _sender;

    public SystemMessageEmailNotifier(CoupleDbContext db, IEmailSender sender)
    {
        _db = db;
        _sender = sender;
    }

    public async Task NotifyAsync(CoupleSystemMessage msg, CancellationToken ct = default)
    {
        if (msg == null || !_sender.Enabled) return;
        if (msg.ReceiverUserId <= 0) return;

        var receiver = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == msg.ReceiverUserId && !u.IsDeleted, ct);
        if (receiver?.Email == null) return;

        await _sender.SendAsync(receiver.Email, msg.Title ?? "来自我们的小世界", BuildBody(msg), ct);
    }

    private static string BuildBody(CoupleSystemMessage msg)
    {
        var title = System.Net.WebUtility.HtmlEncode(msg.Title ?? "来自我们的小世界");
        var content = System.Net.WebUtility.HtmlEncode(msg.Content ?? "");
        return $"""
            <div style="font-family:-apple-system,'PingFang SC','Microsoft YaHei',sans-serif;max-width:480px;margin:0 auto;padding:24px;background:#fff7f9;border-radius:16px;">
              <h2 style="color:#e0738a;margin:0 0 12px;">{title}</h2>
              <p style="color:#5b4b4b;font-size:15px;line-height:1.7;">{content}</p>
              <p style="color:#b89b9b;font-size:12px;margin-top:24px;">—— 我们的小世界 · 来自 TA 的温柔提醒</p>
            </div>
            """;
    }
}
