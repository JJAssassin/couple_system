using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Options;
using CoupleLoveSystem.Infrastructure.Persistence;
using CoupleLoveSystem.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// 验证 RefreshToken 轮换：刷新后旧 token 失效、新 token 可用。
/// 用 EF InMemory 提供 CoupleDbContext 夹具，ITokenStore 用 InMemory 实现。
/// </summary>
public class AuthRefreshRotationTests
{
    private static CoupleDbContext NewDb()
    {
        // 每个用例独立的内存库，避免固定库名导致跨用例数据污染/主键冲突
        var opt = new DbContextOptionsBuilder<CoupleDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new CoupleDbContext(opt);
        db.Users.Add(new CoupleUser
        {
            Id = 1,
            UserName = "partner_a",
            NickName = "A",
            PasswordHash = AuthService.HashPassword("123456"),
            RoleType = RoleType.PartnerA,
            IsDeleted = false
        });
        db.SaveChanges();
        return db;
    }

    private static AuthService NewSvc(CoupleDbContext db, ITokenStore store)
        => new(db, store, Options.Create(new JwtOptions
        {
            Secret = "test-secret-test-secret-test-secret-123456",
            AccessExpireMinutes = 120,
            RefreshExpireDays = 7,
            Issuer = "CoupleLove",
            Audience = "CoupleLoveClient"
        }), new LoginRateLimiter(new FakeCacheService()));

    [Fact]
    public async Task Refresh_Rotates_And_Invalidates_Old()
    {
        var db = NewDb();
        var store = new InMemoryTokenStore();
        var svc = NewSvc(db, store);

        var login = await svc.LoginAsync(new LoginReq { UserName = "partner_a", Password = "123456" }, "127.0.0.1");
        var oldRt = login.RefreshToken;

        var refreshed = await svc.RefreshAsync(oldRt);
        Assert.NotEqual(oldRt, refreshed.RefreshToken); // 已轮换

        // 旧 token 应失效
        await Assert.ThrowsAsync<UnauthorizedException>(() => svc.RefreshAsync(oldRt));
        // 新 token 仍可用
        var again = await svc.RefreshAsync(refreshed.RefreshToken);
        Assert.NotEqual(refreshed.RefreshToken, again.RefreshToken);
    }

    [Fact]
    public async Task ReLogin_Revokes_Previous_Refresh()
    {
        var db = NewDb();
        var store = new InMemoryTokenStore();
        var svc = NewSvc(db, store);

        var first = await svc.LoginAsync(new LoginReq { UserName = "partner_a", Password = "123456" }, "127.0.0.1");
        // 同账号再次登录：应吊销第一次签发的 refresh（P1-1）
        var second = await svc.LoginAsync(new LoginReq { UserName = "partner_a", Password = "123456" }, "127.0.0.1");
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);

        // 旧 refresh 现在应失效（否则攻击者可凭旧令牌长期刷新/劫持）
        await Assert.ThrowsAsync<UnauthorizedException>(() => svc.RefreshAsync(first.RefreshToken));
        // 新 refresh 仍可用
        var again = await svc.RefreshAsync(second.RefreshToken);
        Assert.NotEqual(second.RefreshToken, again.RefreshToken);
    }

    [Fact]
    public async Task SoftDeleted_User_Cannot_Refresh()
    {
        var db = NewDb();
        var store = new InMemoryTokenStore();
        var svc = NewSvc(db, store);

        var login = await svc.LoginAsync(new LoginReq { UserName = "partner_a", Password = "123456" }, "127.0.0.1");
        // 软删该用户（模拟注销）
        var user = db.Users.Single(u => u.UserName == "partner_a");
        user.IsDeleted = true;
        db.SaveChanges();

        // 软删后 refresh 应失效，返回 401 而非 500（P1-2）
        await Assert.ThrowsAsync<UnauthorizedException>(() => svc.RefreshAsync(login.RefreshToken));
    }
}
