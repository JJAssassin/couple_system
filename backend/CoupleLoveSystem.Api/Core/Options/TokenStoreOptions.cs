namespace CoupleLoveSystem.Core.Options;

/// <summary>
/// TokenStore 提供方配置。Provider=InMemory 走内存实现（默认，零依赖）；
/// Provider=Redis 走 StackExchange.Redis 接本地/生产 Redis。
/// </summary>
public class TokenStoreOptions
{
    public string Provider { get; set; } = "InMemory"; // "InMemory" | "Redis"
    public string Configuration { get; set; } = "127.0.0.1:6379";
    public string KeyPrefix { get; set; } = "auth:rt:";
}
