using System;
using System.Linq;
using System.Threading.Tasks;
using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using CoupleLoveSystem.Tests.Infrastructure;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// DatePlanService 约会计划服务测试（InMemory EF）。
/// 覆盖：创建/编辑/删除/列表/统计 + 边界场景（空统计、软删除隔离、分页）。
/// </summary>
public class DatePlanServiceTests
{
    private static DatePlanService Build(out CoupleDbContext db)
    {
        db = TestDb.CreateInMemoryContext();
        return new DatePlanService(new EfRepository<CoupleDateRecord>(db), db);
    }

    private static CoupleDateRecord Seed(CoupleDbContext db, DateTime planTime, bool completed = false, int? score = null)
    {
        var d = new CoupleDateRecord
        {
            PlanTime = planTime,
            IsCompleted = completed,
            RealTime = completed ? planTime.AddHours(2) : null,
            Location = "咖啡厅",
            Budget = 200,
            RealCost = 180,
            ExperienceScore = score,
            Remark = "很开心",
            CreateUserId = 1,
            CreateTime = DateTime.UtcNow,
        };
        db.DateRecords.Add(d);
        db.SaveChanges();
        return d;
    }

    [Fact]
    public async Task CreateAsync_创建约会记录_返回DTO并设置创建时间()
    {
        var svc = Build(out var db);
        var req = new DateRecordReq
        {
            IsCompleted = false,
            PlanTime = new DateTime(2026, 9, 1, 19, 0, 0),
            Location = "电影院",
            Budget = 150,
            RealCost = null,
            ExperienceScore = null,
            Remark = "新片上映",
        };

        var result = await svc.CreateAsync(req, 1);

        Assert.NotNull(result);
        Assert.Equal("电影院", result.Location);
        Assert.Equal(150m, result.Budget);
        Assert.False(result.IsCompleted);
        Assert.True(result.Id > 0);
        Assert.NotNull(result.CreateTime);
    }

    [Fact]
    public async Task CreateAsync_标记完成且未给实际时间_自动填充UtcNow()
    {
        var svc = Build(out var db);
        var before = DateTime.UtcNow;
        var req = new DateRecordReq
        {
            IsCompleted = true,
            PlanTime = new DateTime(2026, 9, 1, 19, 0, 0),
            RealTime = null,
            Location = "晚餐",
            Budget = 300,
            RealCost = 280,
            ExperienceScore = 5,
            Remark = "",
        };

        var result = await svc.CreateAsync(req, 1);

        Assert.True(result.IsCompleted);
        Assert.NotNull(result.RealTime);
        Assert.True(result.RealTime >= before);
    }

    [Fact]
    public async Task UpdateAsync_修改未完成记录_仅更新给定字段()
    {
        var svc = Build(out var db);
        var d = Seed(db, new DateTime(2026, 9, 1), false);

        var req = new DateRecordReq
        {
            IsCompleted = true,
            PlanTime = new DateTime(2026, 9, 2, 19, 0, 0),
            RealTime = new DateTime(2026, 9, 2, 21, 0, 0),
            Location = "升级版餐厅",
            Budget = 400,
            RealCost = 380,
            ExperienceScore = 9,
            Remark = "太棒了",
        };

        var result = await svc.UpdateAsync(d.Id, req, 1);

        Assert.True(result.IsCompleted);
        Assert.Equal("升级版餐厅", result.Location);
        Assert.Equal(400m, result.Budget);
        Assert.Equal(9, result.ExperienceScore);
        Assert.Equal(new DateTime(2026, 9, 2, 21, 0, 0), result.RealTime);
    }

    [Fact]
    public async Task UpdateAsync_标记完成且RealTime为空_自动填充UtcNow()
    {
        var svc = Build(out var db);
        var d = Seed(db, new DateTime(2026, 9, 1), false);

        var before = DateTime.UtcNow;
        var req = new DateRecordReq
        {
            IsCompleted = true,
            PlanTime = d.PlanTime,
            RealTime = null,
            Location = d.Location,
            Budget = d.Budget,
            RealCost = d.RealCost,
            ExperienceScore = d.ExperienceScore,
            Remark = d.Remark,
        };

        var result = await svc.UpdateAsync(d.Id, req, 1);

        Assert.NotNull(result.RealTime);
        Assert.True(result.RealTime >= before);
    }

    [Fact]
    public async Task DeleteAsync_软删除约会记录()
    {
        var svc = Build(out var db);
        var d = Seed(db, new DateTime(2026, 9, 1));

        await svc.DeleteAsync(d.Id, 1);

        var list = await svc.ListAsync(1, 10);
        Assert.Empty(list.Items);
    }

    [Fact]
    public async Task ListAsync_分页与排序_最新在前()
    {
        var svc = Build(out var db);
        Seed(db, new DateTime(2026, 8, 1));
        Seed(db, new DateTime(2026, 8, 5));
        Seed(db, new DateTime(2026, 8, 10));

        var page1 = await svc.ListAsync(1, 2);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(3, page1.Total);
        // 降序：最新日期在前
        Assert.True(page1.Items[0].PlanTime >= page1.Items[1].PlanTime);

        var page2 = await svc.ListAsync(2, 2);
        Assert.Single(page2.Items);
    }

    [Fact]
    public async Task StatsAsync_计算完成数及平均评分()
    {
        var svc = Build(out var db);
        Seed(db, new DateTime(2026, 8, 1), completed: true, score: 8);
        Seed(db, new DateTime(2026, 8, 2), completed: true, score: 6);
        Seed(db, new DateTime(2026, 8, 3), completed: false, score: null);

        var stats = await svc.StatsAsync();

        Assert.Equal(2, stats.TotalDates);
        Assert.Equal(7.0, stats.AvgScore);
    }

    [Fact]
    public async Task StatsAsync_无已完成记录_返回0()
    {
        var svc = Build(out var db);
        Seed(db, new DateTime(2026, 8, 1), completed: false);

        var stats = await svc.StatsAsync();

        Assert.Equal(0, stats.TotalDates);
        Assert.Equal(0, stats.AvgScore);
    }

    [Fact]
    public async Task GetAsync_不存在记录_抛出NotFoundException()
    {
        var svc = Build(out _);

        await Assert.ThrowsAsync<NotFoundException>(() => svc.GetAsync(999));
    }

    [Fact]
    public async Task Map_正确映射实体到DTO()
    {
        var now = DateTime.UtcNow;
        var entity = new CoupleDateRecord
        {
            Id = 1,
            IsCompleted = true,
            PlanTime = new DateTime(2026, 9, 1),
            RealTime = new DateTime(2026, 9, 1, 21, 0, 0),
            Location = "公园",
            Budget = 100,
            RealCost = 80,
            ExperienceScore = 7,
            Remark = "散步",
            CreateUserId = 2,
            CreateTime = now,
        };

        var dto = DatePlanService.Map(entity);

        Assert.Equal(1, dto.Id);
        Assert.Equal("公园", dto.Location);
        Assert.Equal(100m, dto.Budget);
        Assert.Equal(7, dto.ExperienceScore);
        Assert.Equal(now, dto.CreateTime);
    }
}
