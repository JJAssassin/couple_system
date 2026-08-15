using Microsoft.Extensions.Logging;

namespace CoupleLoveSystem.Infrastructure.Email;

/// <summary>未配置 SMTP 时的安全降级实现：记录日志但不连网发送。
/// 保证邮件通知功能在无外部依赖时也不会报错、不影响主业务（消息照常落库）。</summary>
public class NoOpEmailSender : IEmailSender
{
    private readonly ILogger<NoOpEmailSender> _logger;
    public NoOpEmailSender(ILogger<NoOpEmailSender> logger) => _logger = logger;

    public bool Enabled => false;

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogDebug("邮件通知未启用（Email.Enabled=false 或 SMTP 未配置），跳过发往 {To} 的邮件：{Subject}", to, subject);
        return Task.CompletedTask;
    }
}
