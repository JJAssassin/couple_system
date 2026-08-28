using System;
using System.Threading;
using System.Threading.Tasks;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using CoupleLoveSystem.Tests.Infrastructure;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// WishService 集成测试（InMemory EF + 空广播器）。
/// 覆盖创建者记录(CreateUserId)、认领状态流转、完成逻辑与名称解析、缺失项 NotFound。
/// </summary>
public class WishServiceTests
{
    private static WishService Build(out CoupleDbContext db)
    {
        db = TestDb.CreateInMemoryContext();
        return new WishService(
            new EfRepository<CoupleWish>(db),
            new EfRepository<CoupleUser>(db),
            db);
    }

    private static async Task SeedUser(CoupleDbContext db, long id, string nick)
    {
        db.Users.Add(new CoupleUser { Id = id, UserName = $"u{id}", NickName = nick });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_记录创建者Id_默认状态为未开始()
    {
        var svc = Build(out _);

        var dto = await svc.CreateAsync(
            new WishReq { Title = "一起去旅行", WishType = WishType.Target, Priority = 1 },
            currentUserId: 1);

        Assert.Equal(1, dto.CreateUserId);
        Assert.Equal(WishStatus.NotStart, dto.Status);
        Assert.True(dto.Id > 0);
    }

    [Fact]
    public async Task ClaimAsync_设置认领人_且未开始流转为进行中()
    {
        var svc = Build(out var db);
        await SeedUser(db, 2, "乙方");
        var created = await svc.CreateAsync(new WishReq { Title = "想要花" }, currentUserId: 1);

        var claimed = await svc.ClaimAsync(created.Id, currentUserId: 2);

        Assert.Equal(2, claimed.ClaimUserId);
        Assert.Equal(WishStatus.Doing, claimed.Status);
        Assert.Equal("乙方", claimed.ClaimUserName);
    }

    [Fact]
    public async Task ClaimAsync_已在进行中_状态保持不变()
    {
        var svc = Build(out var db);
        var created = await svc.CreateAsync(new WishReq { Title = "想要书" }, currentUserId: 1);
        await svc.ClaimAsync(created.Id, currentUserId: 2); // -> Doing

        var again = await svc.ClaimAsync(created.Id, currentUserId: 2);

        Assert.Equal(WishStatus.Doing, again.Status);
        Assert.Equal(2, again.ClaimUserId);
    }

    [Fact]
    public async Task CompleteAsync_未认领时补齐认领人_并置为已完成()
    {
        var svc = Build(out _);
        var created = await svc.CreateAsync(new WishReq { Title = "想要惊喜" }, currentUserId: 1);

        var done = await svc.CompleteAsync(
            new WishCompleteReq { Id = created.Id, CompleteRemark = "已送达" },
            currentUserId: 2);

        Assert.Equal(WishStatus.Completed, done.Status);
        Assert.Equal(2, done.ClaimUserId);
        Assert.NotNull(done.CompleteTime);
        Assert.Equal("已送达", done.CompleteRemark);
    }

    [Fact]
    public async Task GetAsync_不存在_抛出NotFound()
    {
        var svc = Build(out _);
        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetAsync(999, currentUserId: 1));
    }
}
