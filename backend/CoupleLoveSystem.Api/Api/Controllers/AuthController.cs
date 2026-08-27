using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/auth")]
public class AuthController : BaseController
{
    private readonly AuthService _auth;
    public AuthController(AuthService auth) => _auth = auth;

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResult<LoginResp>>> Login([FromBody] LoginReq req, CancellationToken ct)
    {
        // 客户端 IP（经 CF 隧道时取转发头；取不到则仅账号维度限速兜底）
        var ip = HttpContext.Request.Headers["CF-Connecting-IP"].ToString();
        if (string.IsNullOrWhiteSpace(ip))
            ip = HttpContext.Request.Headers["X-Forwarded-For"].ToString();
        if (string.IsNullOrWhiteSpace(ip))
            ip = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? string.Empty;
        var resp = await _auth.LoginAsync(req, ip, ct);
        return Ok(ApiResult<LoginResp>.Ok(resp));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResult<LoginResp>>> Refresh(CancellationToken ct)
    {
        // refresh 仅经 HttpOnly Cookie cl_rt 传递（前端 JS 不可读、不可写），不再接受 body 传入，
        // 杜绝 XSS 凭 body 调用刷新（评审 #2）。缺失 Cookie 即 401。
        var rt = Request.Cookies["cl_rt"];
        if (string.IsNullOrEmpty(rt))
            return Unauthorized(ApiResult<LoginResp>.Fail(ErrorCode.ParamInvalid, "缺少刷新令牌"));
        var resp = await _auth.RefreshAsync(rt, ct);
        return Ok(ApiResult<LoginResp>.Ok(resp));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResult<object>>> Logout(CancellationToken ct)
    {
        var rt = Request.Cookies["cl_rt"];
        if (!string.IsNullOrEmpty(rt))
            await _auth.LogoutAsync(rt, ct);
        return Ok(ApiResults.Ok(new { }, "已退出"));
    }
}
