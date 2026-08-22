using System;
using System.Collections.Generic;
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
/// DiaryService 日记服务测试（InMemory EF）。
/// 覆盖：CRUD、分页排序、私密可见性控制、评论、XSS 净化。
/// </summary>
public class DiaryServiceTests
{
    private static DiaryService Build(out CoupleDbContext db)
    {
        db = TestDb.CreateInMemoryContext();
        var diaryRepo = new EfRepository<CoupleDiary>(db);
        var commentRepo = new EfRepository<CoupleDiaryComment>(db);
        var html = new HtmlSanitizerService();
        return new DiaryService(diaryRepo, commentRepo, db, html);
    }

    private static CoupleDiary SeedDiary(CoupleDbContext db, long userId, bool isPrivate = false)
    {
        var d = new CoupleDiary
        {
            Title = "美好的一天",
            Content = "<p>今天很开心</p>",
            MoodTag = "happy",
            MoodScore = 9,
            PermissionType = isPrivate ? PermissionType.PrivateSelf : PermissionType.Public,
            DiaryDate = new DateTime(2026, 8, 15),
            CreateUserId = userId,
            CreateTime = DateTime.UtcNow,
        };
        db.Diaries.Add(d);
        db.SaveChanges();
        return d;
    }

    [Fact]
    public async Task CreateAsync_创建公开日记_成功返回()
    {
        var svc = Build(out _);
        var req = new DiaryReq
        {
            Title = "第一次约会",
            Content = "<b>紧张又期待</b>",
            MoodTag = "excited",
            MoodScore = 10,
            PermissionType = PermissionType.Public,
            Weather = "晴",
            DiaryDate = new DateTime(2026, 8, 15),
        };

        var result = await svc.CreateAsync(req, 1);

        Assert.NotNull(result);
        Assert.Equal("第一次约会", result.Title);
        Assert.Equal(10, result.MoodScore);
        Assert.Equal(PermissionType.Public, result.PermissionType);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task CreateAsync_XSS内容_被HtmlSanitizer净化()
    {
        var svc = Build(out _);
        var req = new DiaryReq
        {
            Title = "XSS测试",
            Content = "<script>alert('xss')</script><p>安全</p>",
            MoodTag = "test",
            MoodScore = 5,
            PermissionType = PermissionType.Public,
            Weather = null,
            DiaryDate = new DateTime(2026, 8, 15),
        };

        var result = await svc.CreateAsync(req, 1);

        Assert.DoesNotContain("<script>", result.Content);
        Assert.Contains("<p>安全</p>", result.Content);
    }

    [Fact]
    public async Task ListAsync_分页与排序_最新日记在前()
    {
        var svc = Build(out var db);
        SeedDiary(db, 1);
        SeedDiary(db, 1);
        SeedDiary(db, 1);

        var page = await svc.ListAsync(1, 2, 1);

        Assert.Equal(2, page.Items.Count);
        Assert.Equal(3, page.Total);
        Assert.True(page.Items[0].DiaryDate >= page.Items[1].DiaryDate);
    }

    [Fact]
    public async Task GetAsync_私密日记_作者本人可访问()
    {
        var svc = Build(out var db);
        var d = SeedDiary(db, 1, isPrivate: true);

        var result = await svc.GetAsync(d.Id, 1);

        Assert.NotNull(result);
        Assert.Equal(d.Title, result.Title);
    }

    [Fact]
    public async Task GetAsync_私密日记_他人访问抛出Forbidden()
    {
        var svc = Build(out var db);
        var d = SeedDiary(db, 1, isPrivate: true);

        await Assert.ThrowsAsync<ForbiddenException>(() => svc.GetAsync(d.Id, 2));
    }

    [Fact]
    public async Task UpdateAsync_修改日记_内容与评分更新()
    {
        var svc = Build(out var db);
        var d = SeedDiary(db, 1);

        var req = new DiaryReq
        {
            Title = "更新后标题",
            Content = "<p>新内容</p>",
            MoodTag = "joyful",
            MoodScore = 8,
            PermissionType = PermissionType.Public,
            Weather = "多云",
            DiaryDate = d.DiaryDate,
        };

        var result = await svc.UpdateAsync(d.Id, req, 1);

        Assert.Equal("更新后标题", result.Title);
        Assert.Equal(8, result.MoodScore);
        Assert.Equal("joyful", result.MoodTag);
    }

    [Fact]
    public async Task DeleteAsync_软删除日记()
    {
        var svc = Build(out var db);
        var d = SeedDiary(db, 1);

        await svc.DeleteAsync(d.Id, 1);

        var list = await svc.ListAsync(1, 10, 1);
        Assert.Empty(list.Items);
    }

    [Fact]
    public async Task AddCommentAsync_添加评论_成功返回()
    {
        var svc = Build(out var db);
        var d = SeedDiary(db, 1);

        var result = await svc.AddCommentAsync(new DiaryCommentReq { DiaryId = d.Id, Content = "写得真好！" }, 2);

        Assert.NotNull(result);
        Assert.Equal("写得真好！", result.Content);
        Assert.Equal(2, result.CreateUserId);
    }

    [Fact]
    public async Task ListCommentsAsync_返回日记评论列表()
    {
        var svc = Build(out var db);
        var d = SeedDiary(db, 1);
        await svc.AddCommentAsync(new DiaryCommentReq { DiaryId = d.Id, Content = "评论1" }, 2);
        await svc.AddCommentAsync(new DiaryCommentReq { DiaryId = d.Id, Content = "评论2" }, 2);

        var comments = await svc.ListCommentsAsync(d.Id, 1);

        Assert.Equal(2, comments.Count);
        Assert.Equal("评论1", comments[0].Content);
        Assert.Equal("评论2", comments[1].Content);
    }
}
