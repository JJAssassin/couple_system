namespace CoupleLoveSystem.Core.Options;

public class JwtOptions
{
    public string Secret { get; set; } = string.Empty;   // >=32 字节随机串，存 User Secrets/环境变量
    public int AccessExpireMinutes { get; set; } = 120;  // 2h
    public int RefreshExpireDays { get; set; } = 7;
    public string Issuer { get; set; } = "CoupleLove";
    public string Audience { get; set; } = "CoupleLoveClient";
}
