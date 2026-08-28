using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Options;
using CoupleLoveSystem.Infrastructure.Persistence;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;
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
    private readonly LoginRateLimiter _rateLimiter;
    private readonly IHttpContextAccessor? _http;

    private readonly JwtKeyResolver? _keyResolver;

    public AuthService(CoupleDbContext db, ITokenStore tokens, IOptions<JwtOptions> jwt, LoginRateLimiter rateLimiter, IHttpContextAccessor? http = null, JwtKeyResolver? keyResolver = null)
    {
        _db = db; _tokens = tokens; _jwt = jwt.Value; _rateLimiter = rateLimiter; _http = http; _keyResolver = keyResolver;
    }

    public async Task<LoginResp> LoginAsync(LoginReq req, string? clientIp, CancellationToken ct = default)
    {
        // 防爆破：IP + 账号双维度固定窗口限速（超限 429）
        await _rateLimiter.CheckAsync(clientIp ?? string.Empty, req.UserName, ct);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == req.UserName && !u.IsDeleted, ct)
            ?? throw new UnauthorizedException("用户名或密码错误");
        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        {
            await _rateLimiter.RecordFailAsync(clientIp ?? string.Empty, req.UserName, ct);
            throw new UnauthorizedException("用户名或密码错误");
        }

        // 登录成功：清空该账号的失败计数
        await _rateLimiter.ResetAsync(req.UserName, ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<LoginResp> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var userId = await FindUserIdByRefreshAsync(refreshToken, ct)
            ?? throw new UnauthorizedException("RefreshToken 失效，请重新登录");

        // 严格轮换（P1-1）：呈交的 refresh 必须是该用户「当前有效」的 refresh。
        // 旧令牌重放直接拒绝，避免攻击者用旧令牌把合法用户踢下线并劫持会话。
        var current = await _tokens.GetAsync($"rt:{userId}", ct);
        if (current != refreshToken)
            throw new UnauthorizedException("RefreshToken 已失效，请重新登录");

        // 软删用户禁止刷新（P1-2）：注销后 refresh 应立即失效
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct)
            ?? throw new UnauthorizedException("RefreshToken 已失效，请重新登录");

        // 轮换：作废当前 token，签发新 token——防止 RefreshToken 被盗后长期可用
        await _tokens.RemoveAsync($"rt:{userId}", ct);
        await _tokens.RemoveAsync($"rti:{refreshToken}", ct);
        return await IssueTokensAsync(user, ct);
    }

    /// <summary>
    /// 为指定用户签发全新 access + refresh（refresh 写入 ITokenStore 并建立反向索引）。
    /// 复用于登录、刷新、以及绑定/解绑后的令牌重签——保证 cid 声明与库中当前 CoupleId 一致，
    /// 避免「绑定成功却空库」（旧 token 的 cid 仍为旧值，被全局过滤器挡掉真实数据）。
    /// </summary>
    public async Task<LoginResp> IssueTokensForUserAsync(long userId, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstAsync(u => u.Id == userId && !u.IsDeleted, ct);
        return await IssueTokensAsync(user, ct);
    }

    private async Task<LoginResp> IssueTokensAsync(CoupleUser user, CancellationToken ct)
    {
        // 吊销该用户此前持有的 refresh（含反向索引）：重新登录/重签后旧令牌立即失效，杜绝 7 天重放窗口（P1-1）
        var priorRefresh = await _tokens.GetAsync($"rt:{user.Id}", ct);
        if (priorRefresh != null)
        {
            await _tokens.RemoveAsync($"rti:{priorRefresh}", ct);
            await _tokens.RemoveAsync($"rt:{user.Id}", ct);
        }

        var access = IssueAccessToken(user.Id, user.RoleType, user.CoupleId);
        var refresh = Guid.NewGuid().ToString("N");
        var ttl = TimeSpan.FromDays(_jwt.RefreshExpireDays);
        await _tokens.SetAsync($"rt:{user.Id}", refresh, ttl, ct);
        await _tokens.SetAsync($"rti:{refresh}", user.Id.ToString(), ttl, ct); // 反向索引 token→userId，O(1) 反查

        // 媒体访问 Cookie（HttpOnly）：供 /uploads 静态资源鉴权网关读取（P2-4）。
        // <img src> 无法在请求头带 Bearer，故用 HttpOnly Cookie 在同源下自动携带；仅当处于真实 HTTP 请求上下文时写入。
        // 刷新令牌 Cookie（HttpOnly，禁止 JS 读取）：前端不再持久化 refreshToken，杜绝 XSS 窃取长生命周期凭据（评审 #2）。
        // 浏览器在 /auth/refresh、/auth/logout 同源请求时自动携带。Secure=false 兼容本机 http 与 https 隧道两种访问；
        // 若全站统一 https，可改为 Secure = app.Environment.IsProduction()。
        var httpCtx = _http?.HttpContext;
        if (httpCtx?.Response.HasStarted == false)
        {
            httpCtx.Response.Cookies.Append("cl_at", access, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = httpCtx.Request.IsHttps,
                MaxAge = TimeSpan.FromMinutes(_jwt.AccessExpireMinutes),
                Path = "/"
            });
            httpCtx.Response.Cookies.Append("cl_rt", refresh, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = false,
                MaxAge = ttl,
                Path = "/"
            });
        }

        return new LoginResp
        {
            AccessToken = access,
            RefreshToken = string.Empty, // 不再经 JSON 返回；refresh 仅存于 HttpOnly Cookie cl_rt
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
        // 注销时清除媒体访问 Cookie 与刷新 Cookie，避免令牌失效后 Cookie 仍可用（P2-4 / 评审 #2）
        var httpCtx = _http?.HttpContext;
        if (httpCtx?.Response.HasStarted == false)
        {
            httpCtx.Response.Cookies.Delete("cl_at");
            httpCtx.Response.Cookies.Delete("cl_rt");
        }
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

    private static UserProfileDto ToProfile(Domain.Entities.CoupleUser u) => new()
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
