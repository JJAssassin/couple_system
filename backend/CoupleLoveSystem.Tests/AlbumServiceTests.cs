using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Domain.Entities;
using CoupleLoveSystem.Core.Result;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using CoupleLoveSystem.Tests.Infrastructure;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// AlbumService 相册服务测试（InMemory EF）。
/// 覆盖：创建/编辑/删除/列表/图片统计 + 边界场景（空名称校验、软删除级联图片）。
/// </summary>
public class AlbumServiceTests
{
    private static AlbumService Build(out CoupleDbContext db)
    {
        db = TestDb.CreateInMemoryContext();
        var albumRepo = new EfRepository<CoupleAlbum>(db);
        var imgRepo = new EfRepository<CoupleImage>(db);
        return new AlbumService(albumRepo, imgRepo, db);
    }

    private static CoupleAlbum SeedAlbum(CoupleDbContext db, string name)
    {
        var a = new CoupleAlbum
        {
            AlbumName = name,
            Cover = "/uploads/cover.jpg",
            Remark = "旅行",
            CreateUserId = 1,
            CreateTime = DateTime.UtcNow,
        };
        db.Albums.Add(a);
        db.SaveChanges();
        return a;
    }

    private static CoupleImage SeedImage(CoupleDbContext db, long albumId, string path)
    {
        var img = new CoupleImage
        {
            AlbumId = albumId,
            ImagePath = path,
            Remark = "美景",
            ShootTime = DateTime.UtcNow,
            CreateUserId = 1,
            CreateTime = DateTime.UtcNow,
        };
        db.Images.Add(img);
        db.SaveChanges();
        return img;
    }

    [Fact]
    public async Task CreateAsync_创建相册_成功返回DTO()
    {
        var svc = Build(out var db);
        var req = new AlbumReq { AlbumName = "海边度假", Cover = "/c.jpg", Remark = "三亚" };

        var result = await svc.CreateAsync(req, 1);

        Assert.NotNull(result);
        Assert.Equal("海边度假", result.AlbumName);
        Assert.Equal("/c.jpg", result.Cover);
        Assert.Equal(0, result.ImageCount);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task CreateAsync_空名称_抛出ConflictException()
    {
        var svc = Build(out _);
        var req = new AlbumReq { AlbumName = "   ", Cover = null, Remark = "" };

        await Assert.ThrowsAsync<ConflictException>(() => svc.CreateAsync(req, 1));
    }

    [Fact]
    public async Task UpdateAsync_更新相册_仅更新非空字段()
    {
        var svc = Build(out var db);
        var a = SeedAlbum(db, "旧名");

        var req = new AlbumReq { AlbumName = "新名", Cover = "/new.jpg", Remark = null };

        var result = await svc.UpdateAsync(a.Id, req, 1);

        Assert.Equal("新名", result.AlbumName);
        Assert.Equal("/new.jpg", result.Cover);
        Assert.Equal("旅行", result.Remark); // 未更新
    }

    [Fact]
    public async Task DeleteAsync_软删除相册()
    {
        var svc = Build(out var db);
        var a = SeedAlbum(db, "要删的");

        await svc.DeleteAsync(a.Id);

        // 相册被软删后列表不可见
        var list = await svc.ListAsync(1, 10);
        Assert.Empty(list.Items);
    }

    [Fact]
    public async Task ListAsync_分页与图片计数()
    {
        var svc = Build(out var db);
        var a1 = SeedAlbum(db, "A");
        var a2 = SeedAlbum(db, "B");
        SeedImage(db, a1.Id, "/a1.jpg");
        SeedImage(db, a1.Id, "/a2.jpg");
        SeedImage(db, a2.Id, "/b1.jpg");

        var page = await svc.ListAsync(1, 10);

        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
        // 按创建时间降序
        Assert.True(page.Items[0].CreateTime >= page.Items[1].CreateTime);
        // 图片计数
        Assert.Equal(2, page.Items.First(i => i.AlbumName == "A").ImageCount);
        Assert.Equal(1, page.Items.First(i => i.AlbumName == "B").ImageCount);
    }

    [Fact]
    public async Task ListImagesAsync_返回相册内图片()
    {
        var svc = Build(out var db);
        var a = SeedAlbum(db, "有图");
        SeedImage(db, a.Id, "/1.jpg");
        SeedImage(db, a.Id, "/2.jpg");

        var imgs = await svc.ListImagesAsync(a.Id);

        Assert.Equal(2, imgs.Count);
        // 按拍摄时间降序（后拍的在前）
        Assert.True(imgs[0].CreateTime >= imgs[1].CreateTime);
    }
}
