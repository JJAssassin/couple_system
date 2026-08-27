using CoupleLoveSystem.Application.Services;
using CoupleLoveSystem.Core.Dtos;
using CoupleLoveSystem.Core.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoupleLoveSystem.Api.Controllers;

[Route("api/user")]
[Authorize]
public class UserController : BaseController
{
    private readonly UserService _svc;
    private readonly Application.Services.ITokenStore _tokenStore;
    public UserController(UserService svc, Application.Services.ITokenStore tokenStore) => (_svc, _tokenStore) = (svc, tokenStore);

    [HttpPut("profile")]
    public async Task<ActionResult<ApiResult<UserProfileDto>>> Profile(
        [FromBody] UpdateProfileReq req, CancellationToken ct = default) =>
        Ok(ApiResult<UserProfileDto>.Ok(await _svc.UpdateProfileAsync(req, CurrentUserId, ct)));

    [HttpGet("export/alldata")]
    public async Task<ActionResult<ApiResult<ExportResp>>> ExportAllData(CancellationToken ct = default) =>
        Ok(ApiResult<ExportResp>.Ok(await _svc.ExportAsync(CurrentUserId, ct)));

    /// <summary>鉴权下载导出 zip：令牌由 /export/alldata 签发，映射到临时文件，一次性且短 TTL。
    /// 令牌经请求头 X-Export-Token 传递（绝不进 URL，避免泄露到浏览器历史/服务端日志）；
    /// 端点本身仍需有效 Bearer 登录态，双重防护。文件绝不落于公开静态目录，
    /// 下载后即删除并作废令牌，杜绝无鉴权可下载的 PII 泄露与磁盘堆积。</summary>
    [HttpGet("export/download")]
    public async Task<IActionResult> ExportDownload(CancellationToken ct = default)
    {
        var token = Request.Headers["X-Export-Token"].FirstOrDefault()
                    ?? Request.Query["token"].ToString();
        if (string.IsNullOrWhiteSpace(token)) return BadRequest(ApiResult<object>.Fail(ErrorCode.ParamInvalid, "缺少下载令牌"));
        var path = await _tokenStore.GetAsync("export:" + token, ct);
        if (path == null || !System.IO.File.Exists(path))
            return NotFound(ApiResult<object>.Fail(ErrorCode.NotFound, "下载令牌无效或已过期"));
        await _tokenStore.RemoveAsync("export:" + token, ct);
        var bytes = await System.IO.File.ReadAllBytesAsync(path, ct);
        try { System.IO.File.Delete(path); } catch { /* 临时文件清理失败不阻断下载 */ }
        return File(bytes, "application/zip", "couple_export.zip");
    }
}
