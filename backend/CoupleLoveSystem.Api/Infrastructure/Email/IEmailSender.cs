namespace CoupleLoveSystem.Infrastructure.Email;

/// <summary>邮件发送抽象。Enabled=false 的实现（如 NoOpEmailSender）不会真正发信，
/// 仅用于无 SMTP 配置的开发/测试环境，保证邮件通知功能「永远可用、默认可关」。</summary>
public interface IEmailSender
{
    /// <summary>是否真的会发送邮件。未启用/未配置时为 false，通知桥接会据此直接跳过。</summary>
    bool Enabled { get; }

    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
