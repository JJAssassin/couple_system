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
/// TodoService 集成测试（InMemory EF + 空广播器）。
/// 覆盖创建者记录、勾选完成流转（记录完成人/时间）、指派责任人名称解析、缺失项 NotFound、列表排序。
/// </summary>
public class TodoServiceTests
{
    private static TodoService Build(out CoupleDbContext db)
    {
        db = TestDb.CreateInMemoryContext();
        return new TodoService(
            new EfRepository<CoupleTodo>(db),
            new EfRepository<CoupleUser>(db));
    }

    private static async Task SeedUser(CoupleDbContext db, long id, string nick)
    {
        db.Users.Add(new CoupleUser { Id = id, UserName = $"u{id}", NickName = nick });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_记录创建者Id_默认未完成()
    {
        var svc = Build(out _);

        var dto = await svc.CreateAsync(
            new TodoReq { Title = "买菜", Priority = 1, Category = "家务" },
            currentUserId: 1);

        Assert.Equal(1, dto.CreateUserId);
        Assert.False(dto.IsDone);
        Assert.True(dto.Id > 0);
    }

    [Fact]
    public async Task ToggleAsync_第一次勾选_记录完成人与时间()
    {
        var svc = Build(out var db);
        await SeedUser(db, 2, "乙方");
        var created = await svc.CreateAsync(new TodoReq { Title = "倒垃圾" }, currentUserId: 1);

        var done = await svc.ToggleAsync(created.Id, currentUserId: 2);

        Assert.True(done.IsDone);
        Assert.Equal(2, done.DoneUserId);
        Assert.Equal("乙方", done.DoneUserName);
        Assert.NotNull(done.DoneTime);
    }

    [Fact]
    public async Task ToggleAsync_再次勾选_取消完成并清空完成人()
    {
        var svc = Build(out _);
        var created = await svc.CreateAsync(new TodoReq { Title = "遛狗" }, currentUserId: 1);
        await svc.ToggleAsync(created.Id, currentUserId: 2); // -> 完成

        var undone = await svc.ToggleAsync(created.Id, currentUserId: 2); // -> 取消

        Assert.False(undone.IsDone);
        Assert.Null(undone.DoneUserId);
        Assert.Null(undone.DoneTime);
    }

    [Fact]
    public async Task AssignAsync_设置责任人并解析昵称()
    {
        var svc = Build(out var db);
        await SeedUser(db, 3, "丙方");
        var created = await svc.CreateAsync(new TodoReq { Title = "缴水电费" }, currentUserId: 1);

        var assigned = await svc.AssignAsync(
            new TodoAssignReq { Id = created.Id, AssigneeUserId = 3 },
            currentUserId: 1);

        Assert.Equal(3, assigned.AssigneeUserId);
        Assert.Equal("丙方", assigned.AssigneeName);
    }

    [Fact]
    public async Task GetAsync_不存在_抛出NotFound()
    {
        var svc = Build(out _);
        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetAsync(999, currentUserId: 1));
    }

    [Fact]
    public async Task ListAsync_未完成排在已完成之前_且按优先级降序()
    {
        var svc = Build(out _);
        var low = await svc.CreateAsync(new TodoReq { Title = "低优先", Priority = 3 }, currentUserId: 1);
        var high = await svc.CreateAsync(new TodoReq { Title = "高优先", Priority = 1 }, currentUserId: 1);
        await svc.ToggleAsync(low.Id, currentUserId: 1); // low 完成

        var list = await svc.ListAsync(1, 50, currentUserId: 1);

        Assert.Equal(2, list.Total);
        // 第一个应为未完成的 high（未完成优先 + 优先级高）
        Assert.Equal(high.Id, list.Items[0].Id);
        Assert.True(list.Items[0].IsDone == false);
        Assert.True(list.Items[1].IsDone == true);
    }
}
