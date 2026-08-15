using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoupleLoveSystem.Api;
using CoupleLoveSystem.Api.Hubs;
using CoupleLoveSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CoupleLoveSystem.Tests.Infrastructure;

/// <summary>
/// 测试基础设施：
/// 1) NoopHubContext —— 实现 IHubContext&lt;SyncHub&gt; 的空实现，使 SyncBroadcaster 可在无 SignalR 环境下构造；
///    广播是 fire-and-forget 且被生产代码吞掉异常，测试里直接 no-op 即可。
/// 2) CreateInMemoryContext —— 每次调用用唯一库名创建全新的 InMemory CoupleDbContext，避免测试间数据串扰。
///    注意：全局情侣隔离过滤器依赖 CoupleContext.Current（AsyncLocal）；测试保持为 null 时，
///    插入的实体 CoupleId 留空，会被过滤器的 “eqNull” 分支放行，从而正常读回。
/// </summary>

public class NoopHubContext : IHubContext<SyncHub>
{
    public IHubClients Clients { get; } = new NoopClients();
    public IGroupManager Groups { get; } = new NoopGroups();
}

public class NoopClients : IHubClients
{
    private readonly IClientProxy _proxy = new NoopClient();
    public IClientProxy All => _proxy;
    public IClientProxy AllExcept(IReadOnlyList<string> connectionIds) => _proxy;
    public IClientProxy Client(string connectionId) => _proxy;
    public IClientProxy Clients(IReadOnlyList<string> connectionIds) => _proxy;
    public IClientProxy Group(string groupName) => _proxy;
    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> connectionIds) => _proxy;
    public IClientProxy User(string userId) => _proxy;
    public IClientProxy Users(IReadOnlyList<string> userIds) => _proxy;
    // IHubClients<IClientProxy> 还要求按组集合广播
    public IClientProxy Groups(IReadOnlyList<string> groupNames) => _proxy;
}

public class NoopClient : IClientProxy
{
    public Task SendAsync(string method, object? arg1, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SendAsync(string method, object? arg1, object? arg2, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SendAsync(string method, object? arg1, object? arg2, object? arg3, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SendAsync(string method, IEnumerable<object?> args, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public class NoopGroups : IGroupManager
{
    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public static class TestDb
{
    public static CoupleDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CoupleDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ReplaceService<IModelCacheKeyFactory, CoupleModelCacheKeyFactory>()
            .Options;
        return new CoupleDbContext(options);
    }

    /// <summary>构造一个空广播器，供需要 SyncBroadcaster 的服务在测试中使用。</summary>
    public static SyncBroadcaster NoopBroadcaster() => new(new NoopHubContext());
}
