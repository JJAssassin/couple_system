using System.Net;
using System.Net.Mail;
using CoupleLoveSystem.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoupleLoveSystem.Infrastructure.Email;

/// <summary>基于 BCL System.Net.Mail.SmtpClient 的 SMTP 发送实现（零额外依赖，不引入 MailKit 等新包）。
/// 仅当 Email 配置启用且 SmtpHost 非空时由 Program.cs 注册。任何发送异常都被吞掉并记录，
/// 绝不因邮件失败影响主业务流程（如系统消息已落库、实时推送已发出）。</summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _opt;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> opt, ILogger<SmtpEmailSender> logger)
    {
        _opt = opt.Value;
        _logger = logger;
    }

    public bool Enabled => true;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        try
        {
            using var mail = new MailMessage
            {
                From = new MailAddress(_opt.FromAddress, _opt.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            mail.To.Add(to);

            var client = new SmtpClient(_opt.SmtpHost, _opt.SmtpPort)
            {
                EnableSsl = _opt.EnableSsl,
                Credentials = new NetworkCredential(_opt.SmtpUser, _opt.SmtpPassword)
            };
            await client.SendMailAsync(mail);

            _logger.LogInformation("已通过 SMTP 向 {To} 发送邮件通知：{Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP 发送邮件至 {To} 失败（不影响站内消息落库与实时推送）", to);
        }
    }
}
