using System;
using System.Linq;
using System.Threading.Tasks;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Enums;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// 安全隔离集成测试：验证全局 CoupleId 查询过滤器 + SaveChanges 自动盖章，
/// 确保任意情侣只能读到自己空间的数据，跨情侣数据不可见（核心安全属性），
/// 同时验证软删除对本情侣同样隐藏、匿名上下文不泄漏任何情侣隐私数据。
///
/// 每个「情侣身份」使用独立的 DbContext（与生产一致：每请求独立 scope），
/// 但共享同一 InMemory 库名以便两份情侣数据同库、从而证明跨情侣隔离。
/// 读路径统一用 AsNoTracking 的 Query()，避免变更跟踪器身份映射的副作用。
/// </summary>
public class SecurityIsolationTests
{
    private const string CidA = "iso-cid-A";
    private const string CidB = "iso-cid-B";

    private static DbContextOptions<CoupleDbContext> SharedOptions(string dbName)
        => new DbContextOptionsBuilder<CoupleDbContext>()
            .UseInMemoryDatabase(dbName)
            .ReplaceService<IModelCacheKeyFactory, CoupleModelCacheKeyFactory>()
            .Options;

    [Fact]
    public async Task 全局过滤器_只返回本情侣数据_跨情侣不可见()
    {
        var dbName = "sec-iso-" + Guid.NewGuid();
        try
        {
            // 同一库内种下两份情侣数据
            CoupleContext.Current = null;
            await using (var seed = new CoupleDbContext(SharedOptions(dbName)))
            {
                SeedTwoCouples(seed);
                await seed.SaveChangesAsync();
            }

            // 以情侣 A 身份查询
            CoupleContext.Current = CidA;
            await using (var a = new CoupleDbContext(SharedOptions(dbName)))
            {
                var diariesA = await new EfRepository<CoupleDiary>(a).Query().ToListAsync();
                var wishesA = await new EfRepository<CoupleWish>(a).Query().ToListAsync();
                var albumsA = await new EfRepository<CoupleAlbum>(a).Query().ToListAsync();

                Assert.Equal(2, diariesA.Count);
                Assert.All(diariesA, d => Assert.Equal(CidA, d.CoupleId));
                Assert.Single(wishesA);
                Assert.Equal(CidA, wishesA[0].CoupleId);
                Assert.Single(albumsA);
                Assert.Equal(CidA, albumsA[0].CoupleId);
            }

            // 以情侣 B 身份查询：结果完全镜像，且绝不含 A 的任何一行
            CoupleContext.Current = CidB;
            await using (var b = new CoupleDbContext(SharedOptions(dbName)))
            {
                var diariesB = await new EfRepository<CoupleDiary>(b).Query().ToListAsync();
                Assert.Equal(2, diariesB.Count);
                Assert.All(diariesB, d => Assert.Equal(CidB, d.CoupleId));
                Assert.DoesNotContain(diariesB, d => d.CoupleId == CidA);
            }
        }
        finally
        {
            CoupleContext.Current = null;
        }
    }

    [Fact]
    public async Task 软删除_对本情侣同样隐藏()
    {
        var dbName = "sec-soft-" + Guid.NewGuid();
        try
        {
            CoupleContext.Current = null;
            await using (var seed = new CoupleDbContext(SharedOptions(dbName)))
            {
                SeedTwoCouples(seed);
                await seed.SaveChangesAsync();
            }

            CoupleContext.Current = CidA;
            await using (var db = new CoupleDbContext(SharedOptions(dbName)))
            {
                var repo = new EfRepository<CoupleDiary>(db);
                var all = await repo.Query().ToListAsync();
                var first = all[0];

                repo.SoftDelete(first); // IsDeleted = true
                await repo.SaveChangesAsync();

                var remaining = await repo.Query().ToListAsync();
                Assert.Single(remaining); // 软删除后只剩 1 条
                Assert.DoesNotContain(remaining, d => d.Id == first.Id);
            }
        }
        finally
        {
            CoupleContext.Current = null;
        }
    }

    [Fact]
    public async Task 匿名上下文_只可见CoupleId为空行_不泄漏情侣数据()
    {
        var dbName = "sec-anon-" + Guid.NewGuid();
        try
        {
            CoupleContext.Current = null;
            await using (var seed = new CoupleDbContext(SharedOptions(dbName)))
            {
                SeedTwoCouples(seed);
                // 额外放一条 CoupleId=null 的「系统级宽容行」（模拟对所有人可见的内容）
                seed.Diaries.Add(new CoupleDiary { Title = "sys", Content = "c", CoupleId = null });
                await seed.SaveChangesAsync();
            }

            CoupleContext.Current = null; // 匿名
            await using (var db = new CoupleDbContext(SharedOptions(dbName)))
            {
                var diaries = await new EfRepository<CoupleDiary>(db).Query().ToListAsync();
                // 仅能看到 CoupleId==null 的宽容行，绝不可见 A/B 的隐私数据
                Assert.Single(diaries);
                Assert.Null(diaries[0].CoupleId);
                Assert.Equal("sys", diaries[0].Title);
            }
        }
        finally
        {
            CoupleContext.Current = null;
        }
    }

    private static void SeedTwoCouples(CoupleDbContext db)
    {
        // 情侣 A
        db.Diaries.Add(new CoupleDiary { Title = "A1", Content = "c", CoupleId = CidA, CreateUserId = 1 });
        db.Diaries.Add(new CoupleDiary { Title = "A2", Content = "c", CoupleId = CidA, CreateUserId = 1 });
        db.Wishes.Add(new CoupleWish { WishType = WishType.Common, Title = "AW", CoupleId = CidA, CreateUserId = 1 });
        db.Albums.Add(new CoupleAlbum { AlbumName = "AA", CoupleId = CidA, CreateUserId = 1 });
        // 情侣 B
        db.Diaries.Add(new CoupleDiary { Title = "B1", Content = "c", CoupleId = CidB, CreateUserId = 2 });
        db.Diaries.Add(new CoupleDiary { Title = "B2", Content = "c", CoupleId = CidB, CreateUserId = 2 });
        db.Wishes.Add(new CoupleWish { WishType = WishType.Common, Title = "BW", CoupleId = CidB, CreateUserId = 2 });
        db.Albums.Add(new CoupleAlbum { AlbumName = "BA", CoupleId = CidB, CreateUserId = 2 });
    }
}
