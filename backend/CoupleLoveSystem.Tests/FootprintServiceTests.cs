using System;
using System.Threading.Tasks;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using CoupleLoveSystem.Tests.Infrastructure;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// FootprintService 的集成测试（InMemory EF + 空广播器）。
/// 覆盖：空名称校验、默认表情、计数 +1、属性更新。
/// </summary>
public class FootprintServiceTests
{
    private static FootprintService Build(out CoupleDbContext db)
    {
        db = TestDb.CreateInMemoryContext();
        return new FootprintService(new EfRepository<CoupleFootprint>(db), db);
    }

    [Fact]
    public async Task CreateAsync_名称为空_抛冲突异常()
    {
        var svc = Build(out _);
        await Assert.ThrowsAsync<ConflictException>(() =>
            svc.CreateAsync(new FootprintReq { Title = "   " }, currentUserId: 1));
    }

    [Fact]
    public async Task CreateAsync_正常创建_计数归零且默认表情()
    {
        var svc = Build(out _);
        var dto = await svc.CreateAsync(new FootprintReq { Title = "抱抱", Emoji = "" }, currentUserId: 1);

        Assert.True(dto.Id > 0);
        Assert.Equal("抱抱", dto.Title);
        Assert.Equal(0, dto.Count);
        Assert.Equal("✨", dto.Emoji);   // 空表情回退默认
    }

    [Fact]
    public async Task CreateAsync_名称超长被截断到30()
    {
        var svc = Build(out _);
        var longTitle = new string('A', 50);
        var dto = await svc.CreateAsync(new FootprintReq { Title = longTitle }, currentUserId: 1);
        Assert.Equal(30, dto.Title.Length);
    }

    [Fact]
    public async Task IncrementAsync_计数加1并刷新时间()
    {
        var svc = Build(out var db);
        var dto = await svc.CreateAsync(new FootprintReq { Title = "亲亲" }, currentUserId: 1);
        Assert.Equal(0, dto.Count);

        var after = await svc.IncrementAsync(dto.Id, currentUserId: 1);
        Assert.Equal(1, after.Count);
        Assert.NotNull(after.LastIncrementTime);
    }

    [Fact]
    public async Task IncrementAsync_连续两次_计数累加()
    {
        var svc = Build(out _);
        var dto = await svc.CreateAsync(new FootprintReq { Title = "电影" }, currentUserId: 1);
        await svc.IncrementAsync(dto.Id, 1);
        var after = await svc.IncrementAsync(dto.Id, 1);
        Assert.Equal(2, after.Count);
    }

    [Fact]
    public async Task UpdateAsync_修改名称与表情()
    {
        var svc = Build(out _);
        var dto = await svc.CreateAsync(new FootprintReq { Title = "旧名", Emoji = "✨" }, currentUserId: 1);
        var updated = await svc.UpdateAsync(dto.Id, new FootprintReq { Title = "新名", Emoji = "❤️" }, currentUserId: 1);
        Assert.Equal("新名", updated.Title);
        Assert.Equal("❤️", updated.Emoji);
    }

    [Fact]
    public async Task IncrementAsync_不存在的足迹_抛未找到()
    {
        var svc = Build(out _);
        await Assert.ThrowsAsync<NotFoundException>(() => svc.IncrementAsync(999, currentUserId: 1));
    }
}
