using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Entities;
using CoupleLoveSystem.Core.Options;
using CoupleLoveSystem.Infrastructure.Persistence;
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
        var opt = new DbContextOptionsBuilder<CoupleDbContext>().UseInMemoryDatabase("rotation-test").Options;
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
        }));

    [Fact]
    public async Task Refresh_Rotates_And_Invalidates_Old()
    {
        var db = NewDb();
        var store = new InMemoryTokenStore();
        var svc = NewSvc(db, store);

        var login = await svc.LoginAsync(new LoginReq { UserName = "partner_a", Password = "123456" });
        var oldRt = login.RefreshToken;

        var refreshed = await svc.RefreshAsync(oldRt);
        Assert.NotEqual(oldRt, refreshed.RefreshToken); // 已轮换

        // 旧 token 应失效
        await Assert.ThrowsAsync<UnauthorizedException>(() => svc.RefreshAsync(oldRt));
        // 新 token 仍可用
        var again = await svc.RefreshAsync(refreshed.RefreshToken);
        Assert.NotEqual(refreshed.RefreshToken, again.RefreshToken);
    }
}
