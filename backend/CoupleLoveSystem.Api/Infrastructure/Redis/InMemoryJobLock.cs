using System.Threading;
using System.Threading.Tasks;

namespace CoupleLoveSystem.Infrastructure.Redis;

/// <summary>
/// 进程内互斥锁：单实例部署（TokenStore=InMemory）时使用。用 Interlocked 保证同一进程内不重叠执行。
/// 跨进程无效——但单实例场景已足够；多实例部署应配置 TokenStore=Redis 以启用 RedisJobLock。
/// </summary>
public sealed class InMemoryJobLock : IDistributedJobLock
{
    private int _held;

    public Task<bool> TryAcquireAsync(TimeSpan ttl, CancellationToken ct = default)
        => Task.FromResult(Interlocked.CompareExchange(ref _held, 1, 0) == 0);

    public Task ReleaseAsync(CancellationToken ct = default)
    {
        Interlocked.Exchange(ref _held, 0);
        return Task.CompletedTask;
    }
}
