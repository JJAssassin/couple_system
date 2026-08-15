using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Options;
using CoupleLoveSystem.Infrastructure.Persistence;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CoupleLoveSystem.Application.Services;

public class AuthService
{
    private readonly CoupleDbContext _db;
    private readonly ITokenStore _tokens;
    private readonly JwtOptions _jwt;

    private readonly JwtKeyResolver? _keyResolver;

    public AuthService(CoupleDbContext db, ITokenStore tokens, IOptions<JwtOptions> jwt, JwtKeyResolver? keyResolver = null)
    {
        _db = db; _tokens = tokens; _jwt = jwt.Value; _keyResolver = keyResolver;
    }

    public async Task<LoginResp> LoginAsync(LoginReq req, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == req.UserName && !u.IsDeleted, ct)
            ?? throw new UnauthorizedException("用户名或密码错误");
        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedException("用户名或密码错误");

        var access = IssueAccessToken(user.Id, user.RoleType, user.CoupleId);
        var refresh = Guid.NewGuid().ToString("N");
        var ttl = TimeSpan.FromDays(_jwt.RefreshExpireDays);
        await _tokens.SetAsync($"rt:{user.Id}", refresh, ttl, ct);
        await _tokens.SetAsync($"rti:{refresh}", user.Id.ToString(), ttl, ct); // 反向索引 token→userId，O(1) 反查

        return new LoginResp
        {
            AccessToken = access,
            RefreshToken = refresh,
            ExpiresIn = _jwt.AccessExpireMinutes * 60,
            UserProfile = ToProfile(user)
        };
    }

    public async Task<LoginResp> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var userId = await FindUserIdByRefreshAsync(refreshToken, ct)
            ?? throw new UnauthorizedException("RefreshToken 失效，请重新登录");

        // 轮换：作废旧 token，签发新 token——防止 RefreshToken 被盗后长期可用
        await _tokens.RemoveAsync($"rt:{userId}", ct);
        await _tokens.RemoveAsync($"rti:{refreshToken}", ct);

        var user = await _db.Users.FirstAsync(u => u.Id == userId, ct);
        var access = IssueAccessToken(user.Id, user.RoleType, user.CoupleId);
        var newRt = Guid.NewGuid().ToString("N");
        var ttl = TimeSpan.FromDays(_jwt.RefreshExpireDays);
        await _tokens.SetAsync($"rt:{userId}", newRt, ttl, ct);
        await _tokens.SetAsync($"rti:{newRt}", userId.ToString(), ttl, ct);

        return new LoginResp
        {
            AccessToken = access,
            RefreshToken = newRt,
            ExpiresIn = _jwt.AccessExpireMinutes * 60,
            UserProfile = ToProfile(user)
        };
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var userId = await FindUserIdByRefreshAsync(refreshToken, ct);
        if (userId == null) return;
        await _tokens.RemoveAsync($"rt:{userId}", ct);
        await _tokens.RemoveAsync($"rti:{refreshToken}", ct);
    }

    // 反向索引 rti:{token}→userId，O(1) 反查，消除原全表扫描
    private async Task<long?> FindUserIdByRefreshAsync(string refresh, CancellationToken ct)
    {
        var uid = await _tokens.GetAsync($"rti:{refresh}", ct);
        return long.TryParse(uid, out var id) ? id : null;
    }

    private string IssueAccessToken(long userId, RoleType role, string? coupleId)
    {
        // 优先使用 RSA 非对称签名；仅在无密钥解析器（如单测直接构造）时回退 HMAC
        var creds = _keyResolver != null
            ? _keyResolver.SigningCredentials
            : new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret)), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("sub", userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim("cid", coupleId ?? string.Empty) // 当前情侣空间标识，供全局隔离过滤使用
        };
        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer, audience: _jwt.Audience, claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.AccessExpireMinutes), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static UserProfileDto ToProfile(Core.Entities.CoupleUser u) => new()
    {
        Id = u.Id, NickName = u.NickName, Avatar = u.Avatar, RoleType = u.RoleType, LoveStartTime = u.LoveStartTime
    };

    /// <summary>密码哈希（复用 BCrypt）。新密码落库统一走这里。</summary>
    public static string HashPassword(string plain) => BCrypt.Net.BCrypt.HashPassword(plain);

    /// <summary>密码校验（复用 BCrypt）。登录与改密共用。</summary>
    public static bool VerifyPassword(string plain, string hash) => BCrypt.Net.BCrypt.Verify(plain, hash);
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string msg) : base(msg) { }
}
