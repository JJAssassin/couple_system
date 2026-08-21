using System.Threading;
using System.Threading.Tasks;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using CoupleLoveSystem.Tests.Infrastructure;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// BoardMessageService 集成测试（InMemory EF + 空广播器）。
/// 覆盖作者昵称解析、置顶切换、缺失项 NotFound、列表置顶优先。
/// </summary>
public class BoardMessageServiceTests
{
    private static BoardMessageService Build(out CoupleDbContext db)
    {
        db = TestDb.CreateInMemoryContext();
        return new BoardMessageService(
            new EfRepository<CoupleBoardMessage>(db),
            new EfRepository<CoupleUser>(db));
    }

    private static async Task SeedUser(CoupleDbContext db, long id, string nick)
    {
        db.Users.Add(new CoupleUser { Id = id, UserName = $"u{id}", NickName = nick });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_记录作者与昵称()
    {
        var svc = Build(out var db);
        await SeedUser(db, 1, "甲方");

        var dto = await svc.CreateAsync(new BoardMessageReq { Content = "想你" }, currentUserId: 1);

        Assert.Equal(1, dto.CreateUserId);
        Assert.Equal("甲方", dto.AuthorName);
        Assert.False(dto.Pinned);
    }

    [Fact]
    public async Task PinAsync_切换置顶状态()
    {
        var svc = Build(out _);
        var m = await svc.CreateAsync(new BoardMessageReq { Content = "重要" }, currentUserId: 1);

        var pinned = await svc.PinAsync(m.Id, currentUserId: 1);
        Assert.True(pinned.Pinned);

        var unpinned = await svc.PinAsync(m.Id, currentUserId: 1);
        Assert.False(unpinned.Pinned);
    }

    [Fact]
    public async Task GetAsync_不存在_抛出NotFound()
    {
        var svc = Build(out _);
        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetAsync(999, currentUserId: 1));
    }

    [Fact]
    public async Task ListAsync_置顶项排在最前()
    {
        var svc = Build(out _);
        var a = await svc.CreateAsync(new BoardMessageReq { Content = "A" }, currentUserId: 1);
        var b = await svc.CreateAsync(new BoardMessageReq { Content = "B" }, currentUserId: 1);
        await svc.PinAsync(b.Id, currentUserId: 1);

        var list = await svc.ListAsync(1, 50, currentUserId: 1);

        Assert.Equal(2, list.Total);
        Assert.Equal(b.Id, list.Items[0].Id);
        Assert.True(list.Items[0].Pinned);
    }

    [Fact]
    public async Task ListAsync_私密消息双方可见()
    {
        var svc = Build(out var db);
        await SeedUser(db, 1, "甲方");
        await SeedUser(db, 2, "乙方");

        // 甲方发送私密消息给乙方
        var sent = await svc.CreateAsync(
            new BoardMessageReq { Content = "给乙方的悄悄话", IsPrivate = true, ReceiverUserId = 2 },
            currentUserId: 1);

        // 乙方发送私密消息给甲方
        var received = await svc.CreateAsync(
            new BoardMessageReq { Content = "给甲方的悄悄话", IsPrivate = true, ReceiverUserId = 1 },
            currentUserId: 2);

        // 公开消息
        await svc.CreateAsync(new BoardMessageReq { Content = "公开" }, currentUserId: 1);

        // 甲方视角：应看到自己发送的私密消息 + 乙方发送给她的私密消息 + 公开消息
        var list1 = await svc.ListAsync(1, 50, currentUserId: 1);
        Assert.Equal(3, list1.Total);
        Assert.Contains(list1.Items, m => m.Id == sent.Id);
        Assert.Contains(list1.Items, m => m.Id == received.Id);

        // 乙方视角：应看到自己发送的私密消息 + 甲方发送给他的私密消息 + 公开消息
        var list2 = await svc.ListAsync(1, 50, currentUserId: 2);
        Assert.Equal(3, list2.Total);
        Assert.Contains(list2.Items, m => m.Id == sent.Id);
        Assert.Contains(list2.Items, m => m.Id == received.Id);
    }
}
