using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    /// <summary>从 JWT 的 sub/NameIdentifier claim 取当前用户 Id（绝不取前端参数）。</summary>
    protected long CurrentUserId =>
        long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id)
            ? id : throw new UnauthorizedException("未登录或登录态失效");

    /// <summary>从 JWT 的 role claim 取当前用户角色（男方/女方），用于双人互评等场景。</summary>
    protected RoleType CurrentRole =>
        Enum.TryParse<RoleType>(User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value, out var r)
            ? r : throw new UnauthorizedException("未登录或登录态失效");
}
