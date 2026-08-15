using CoupleLoveSystem.Application.Services;
using Xunit;

namespace CoupleLoveSystem.Tests;

/// <summary>
/// 验证密码哈希/校验（BCrypt）复用逻辑，与 DbSeeder、改密接口保持一致。
/// </summary>
public class AuthServiceTests
{
    [Fact]
    public void HashPassword_VerifyPassword_往返一致()
    {
        var hash = AuthService.HashPassword("123456");
        Assert.NotEqual("123456", hash); // 明文不明文存储
        Assert.True(AuthService.VerifyPassword("123456", hash));
        Assert.False(AuthService.VerifyPassword("wrong", hash));
    }

    [Fact]
    public void HashPassword_同明文两次哈希不同_防彩虹表()
    {
        var h1 = AuthService.HashPassword("secret");
        var h2 = AuthService.HashPassword("secret");
        Assert.NotEqual(h1, h2); // BCrypt 自带盐
        Assert.True(AuthService.VerifyPassword("secret", h1));
        Assert.True(AuthService.VerifyPassword("secret", h2));
    }
}
