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
}
