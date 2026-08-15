namespace CoupleLoveSystem.Core.Options;

/// <summary>SMTP 邮件通知配置（绑定自 appsettings 的 "Email" 段）。
/// Enabled=false（默认）时系统走 NoOpEmailSender，仅记录日志、不连 SMTP，
/// 保证「无外部依赖也能安全运行」。要真正发信需同时 Enabled=true 且填写 SmtpHost 等。</summary>
public class EmailOptions
{
    public bool Enabled { get; set; } = false;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string FromAddress { get; set; } = "noreply@our-little-world.app";
    public string FromName { get; set; } = "我们的小世界";
}
