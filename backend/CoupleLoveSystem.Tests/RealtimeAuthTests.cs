using CoupleLoveSystem.Api;
using CoupleLoveSystem.Api.Hubs;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Realtime;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CoupleLoveSystem.Tests;

#region 录制型 IHubContext（验证广播目标组 / 方法 / 参数）
public class RecordingHubContext : IHubContext<SyncHub>
{
    public string? LastGroup;
    public string? LastMethod;
    public object? LastArg;
    public List<string> AllGroups { get; } = new();
    public IHubClients Clients { get; }
    public IGroupManager Groups { get; } = new NoopGroupMgr();
    public RecordingHubContext() => Clients = new RecordingClients(this);
}

public class RecordingClients : IHubClients
{
    private readonly RecordingHubContext _owner;
    public RecordingClients(RecordingHubContext owner) => _owner = owner;
    private IClientProxy Proxy => new RecordingClient(_owner);
    public IClientProxy All => Proxy;
    public IClientProxy AllExcept(IReadOnlyList<string> ids) => Proxy;
    public IClientProxy Client(string id) => Proxy;
    public IClientProxy Clients(IReadOnlyList<string> ids) => Proxy;
    public IClientProxy Group(string groupName) { _owner.AllGroups.Add(groupName); _owner.LastGroup = groupName; return Proxy; }
    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> ids) => Proxy;
    public IClientProxy User(string userId) => Proxy;
    public IClientProxy Users(IReadOnlyList<string> userIds) => Proxy;
    public IClientProxy Groups(IReadOnlyList<string> groupNames) => Proxy;
}

public class RecordingClient : IClientProxy
{
    private readonly RecordingHubContext _owner;
    public RecordingClient(RecordingHubContext owner) => _owner = owner;
    public Task SendAsync(string method, object? arg1, CancellationToken ct = default)
    { _owner.LastMethod = method; _owner.LastArg = arg1; return Task.CompletedTask; }
    public Task SendAsync(string method, object? arg1, object? arg2, CancellationToken ct = default) => SendAsync(method, arg1, ct);
    public Task SendAsync(string method, object? arg1, object? arg2, object? arg3, CancellationToken ct = default) => SendAsync(method, arg1, ct);
    public Task SendAsync(string method, IEnumerable<object?> args, CancellationToken ct = default) => SendAsync(method, (object?)null, ct);
    public Task SendCoreAsync(string method, object?[] args, CancellationToken ct = default) => SendAsync(method, args.Length > 0 ? args[0] : null, ct);
}

public class NoopGroupMgr : IGroupManager
{
    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken ct = default) => Task.CompletedTask;
    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken ct = default) => Task.CompletedTask;
}
#endregion

public class ConnectionIdentityStoreTests
{
    [Fact]
    public void Bind_Unbind_And_OnlineSnapshot_PerCouple()
    {
        var store = new ConnectionIdentityStore();
        store.Bind("c1", 1, "cid-A");
        store.Bind("c2", 2, "cid-A");
        store.Bind("c3", 3, "cid-B");

        var snap = store.OnlineSnapshot();
        Assert.Equal(2, snap["cid-A"]);
        Assert.Equal(1, snap["cid-B"]);

        var bound = store.TryGet("c1");
        Assert.NotNull(bound);
        Assert.Equal(1L, bound.Value.userId);
        Assert.Equal("cid-A", bound.Value.coupleId);

        store.Unbind("c1");
        Assert.Null(store.TryGet("c1"));
        Assert.Equal(1, store.OnlineSnapshot()["cid-A"]);
    }
}

public class SyncBroadcasterTests
{
    [Fact]
    public async Task NotifyAsync_Targets_CoupleGroup_By_CurrentContext()
    {
        var hub = new RecordingHubContext();
        var broadcaster = new SyncBroadcaster(hub);

        CoupleContext.Current = "cid-A";
        await broadcaster.NotifyAsync("Diary");
        Assert.Equal("couple-cid-A", hub.LastGroup);
        Assert.Equal("Sync", hub.LastMethod);
        var sigA = Assert.IsType<SyncSignal>(hub.LastArg);
        Assert.Equal("Diary", sigA.Module);
        Assert.Equal("reload", sigA.Changes[0].Kind);
        Assert.Null(sigA.Changes[0].Id);

        CoupleContext.Current = "cid-B";
        await broadcaster.NotifyAsync("Letter");
        Assert.Equal("couple-cid-B", hub.LastGroup);
        var sigB = Assert.IsType<SyncSignal>(hub.LastArg);
        Assert.Equal("Letter", sigB.Module);
    }

    [Fact]
    public async Task NotifyAsync_NullContext_Uses_AnonGroup()
    {
        var hub = new RecordingHubContext();
        var broadcaster = new SyncBroadcaster(hub);
        CoupleContext.Current = null;
        await broadcaster.NotifyAsync("Home");
        Assert.Equal("couple-anon", hub.LastGroup);
        var sig = Assert.IsType<SyncSignal>(hub.LastArg);
        Assert.Equal("Home", sig.Module);
    }
}
