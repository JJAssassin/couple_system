using System;
using System.Linq;
using System.Threading.Tasks;
using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using CoupleLoveSystem.Tests.Infrastructure;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// ConflictService 矛盾记录服务测试（InMemory EF）。
/// 覆盖：CRUD、分页、软删除、Map 映射。
/// </summary>
public class ConflictServiceTests
{
    private static ConflictService Build(out CoupleDbContext db)
    {
        db = TestDb.CreateInMemoryContext();
        return new ConflictService(new EfRepository<CoupleConflict>(db), db);
    }

    private static CoupleConflict Seed(CoupleDbContext db, string summary, ConflictLevel level = ConflictLevel.Small)
    {
        var c = new CoupleConflict
        {
            OccurTime = new DateTime(2026, 8, 10),
            Summary = summary,
            ConflictLevel = level,
            MyThoughtA = "我觉得...",
            MyThoughtB = "TA觉得...",
            ReconcileWay = "沟通",
            CreateUserId = 1,
            CreateTime = DateTime.UtcNow,
        };
        db.Conflicts.Add(c);
        db.SaveChanges();
        return c;
    }

    [Fact]
    public async Task CreateAsync_创建矛盾记录_返回完整DTO()
    {
        var svc = Build(out _);
        var req = new ConflictReq
        {
            OccurTime = new DateTime(2026, 8, 10),
            Summary = "因为家务分配",
            ConflictLevel = ConflictLevel.Small,
            MyThoughtA = "我做了很多",
            MyThoughtB = "TA觉得我忽略了TA",
            ReconcileTime = new DateTime(2026, 8, 10, 21, 0, 0),
            ReconcileWay = "一起做",
            ReflectA = "以后多沟通",
            ReflectB = "直接说出来",
            RuleConclusion = "分工明确",
        };

        var result = await svc.CreateAsync(req, 1);

        Assert.NotNull(result);
        Assert.Equal("因为家务分配", result.Summary);
        Assert.Equal(ConflictLevel.Small, result.ConflictLevel);
        Assert.Equal("一起做", result.ReconcileWay);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task ListAsync_分页与排序_最近矛盾在前()
    {
        var svc = Build(out var db);
        Seed(db, "早上的矛盾", ConflictLevel.Small);
        Seed(db, "晚上的矛盾", ConflictLevel.Serious);

        var page = await svc.ListAsync(1, 10);

        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(1, page.Page);
        Assert.Equal(10, page.PageSize);
    }

    [Fact]
    public async Task GetAsync_不存在记录_抛出NotFoundException()
    {
        var svc = Build(out _);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetAsync(999));
    }

    [Fact]
    public async Task UpdateAsync_更新矛盾记录_字段全部更新()
    {
        var svc = Build(out var db);
        var c = Seed(db, "旧矛盾");

        var req = new ConflictReq
        {
            OccurTime = c.OccurTime,
            Summary = "更新后的矛盾",
            ConflictLevel = ConflictLevel.Serious,
            MyThoughtA = "A",
            MyThoughtB = "B",
            ReconcileTime = new DateTime(2026, 8, 11),
            ReconcileWay = "道歉",
            ReflectA = "反思A",
            ReflectB = "反思B",
            RuleConclusion = "结论",
        };

        var result = await svc.UpdateAsync(c.Id, req, 1);

        Assert.Equal("更新后的矛盾", result.Summary);
        Assert.Equal(ConflictLevel.Serious, result.ConflictLevel);
        Assert.Equal("道歉", result.ReconcileWay);
    }

    [Fact]
    public async Task DeleteAsync_软删除矛盾记录()
    {
        var svc = Build(out var db);
        var c = Seed(db, "要删的");

        await svc.DeleteAsync(c.Id, 1);

        var list = await svc.ListAsync(1, 10);
        Assert.Empty(list.Items);
    }

    [Fact]
    public async Task Map_正确映射实体到DTO()
    {
        var now = DateTime.UtcNow;
        var entity = new CoupleConflict
        {
            Id = 1,
            OccurTime = new DateTime(2026, 8, 1),
            Summary = "测试",
            ConflictLevel = ConflictLevel.Middle,
            MyThoughtA = "A",
            MyThoughtB = "B",
            ReconcileWay = "和好",
            CreateUserId = 3,
            CreateTime = now,
        };

        var dto = ConflictService.Map(entity);

        Assert.Equal(1, dto.Id);
        Assert.Equal("测试", dto.Summary);
        Assert.Equal(ConflictLevel.Middle, dto.ConflictLevel);
        Assert.Equal(now, dto.CreateTime);
    }
}
