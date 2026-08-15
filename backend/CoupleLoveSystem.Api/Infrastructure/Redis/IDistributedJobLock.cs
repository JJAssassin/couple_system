namespace CoupleLoveSystem.Infrastructure.Redis;

/// <summary>
/// 定时任务分布式锁抽象：保证同一时刻只有一个实例执行后台作业（提醒/解锁），
/// 避免多实例部署时重复发提醒，也防止单实例内定时器重叠重入。
/// 抢锁成功（TryAcquireAsync 返回 true）后才执行，完成后 ReleaseAsync 释放。
/// </summary>
public interface IDistributedJobLock
{
    /// <summary>尝试获取锁；ttl 内未释放则自动过期，防止持有者崩溃导致死锁。返回 true 表示抢到。</summary>
    Task<bool> TryAcquireAsync(TimeSpan ttl, CancellationToken ct = default);

    /// <summary>释放锁。</summary>
    Task ReleaseAsync(CancellationToken ct = default);
}
