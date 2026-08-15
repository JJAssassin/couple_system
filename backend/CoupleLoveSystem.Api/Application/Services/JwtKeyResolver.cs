using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CoupleLoveSystem.Application.Services;

/// <summary>
/// 统一解析 JWT 的签名/验签密钥，落实安全加固（评审 #4）：
/// 优先级：
///   1) 环境变量 <c>JWT_RSA_PRIVATE_KEY</c>（PEM）—— 生产推荐，密钥绝不入库；
///   2) 文件 <c>keys/jwt-private.pem</c>（相对 ContentRoot 或上级目录，已在 .gitignore 忽略）；
///   3) 配置 <c>Jwt:Secret</c>（HMAC 对称）—— 仅开发/测试回退，密钥不入库；
///   4) 开发期且无任何密钥：生成临时内存 RSA 密钥（重启失效），保证本地可跑。
/// 生产环境强制要求 RSA（1 或 2），否则启动即失败，杜绝对称密钥泄露与内存降级。
/// 同一实例同时供 AuthService 签名与 JwtBearer 验签使用，确保密钥一致。
/// </summary>
public sealed class JwtKeyResolver
{
    public SigningCredentials SigningCredentials { get; }
    public SecurityKey ValidationKey { get; }
    public bool UseRsa { get; }

    public JwtKeyResolver(IConfiguration configuration, IHostEnvironment env, ILogger<JwtKeyResolver> logger)
    {
        // 1) 环境变量优先
        var rsaPem = Environment.GetEnvironmentVariable("JWT_RSA_PRIVATE_KEY");

        // 2) 文件：从 ContentRoot 逐级向上查找 keys/jwt-private.pem（本项目从 bin 目录启动，
        //    密钥在源码目录 keys/ 下，需向上若干级；逐层回退最稳妥，不依赖具体目录深度）。
        if (string.IsNullOrWhiteSpace(rsaPem))
        {
            var dir = new DirectoryInfo(env.ContentRootPath);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "keys", "jwt-private.pem");
                if (File.Exists(candidate))
                {
                    rsaPem = File.ReadAllText(candidate);
                    logger.LogInformation("JWT 使用 RSA 非对称签名（密钥文件：{Path}）", candidate);
                    break;
                }
                dir = dir.Parent;
            }
        }
        else
        {
            logger.LogInformation("JWT 使用 RSA 非对称签名（密钥来源：环境变量 JWT_RSA_PRIVATE_KEY）");
        }

        if (!string.IsNullOrWhiteSpace(rsaPem))
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(rsaPem);
            var key = new RsaSecurityKey(rsa) { KeyId = "couple-rsa" };
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
            ValidationKey = key;
            UseRsa = true;
            return;
        }

        // 3) 无 RSA 密钥
        var secret = configuration["Jwt:Secret"];
        if (env.IsProduction())
        {
            throw new InvalidOperationException(
                "生产环境必须配置 RSA 私钥（环境变量 JWT_RSA_PRIVATE_KEY 或文件 keys/jwt-private.pem），禁止回退到对称密钥。");
        }

        if (!string.IsNullOrWhiteSpace(secret))
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            ValidationKey = key;
            UseRsa = false;
            logger.LogWarning("JWT 回退到对称 HMAC（仅限开发/测试；生产必须改用 RSA）。");
            return;
        }

        // 4) 开发期且无任何密钥：临时内存 RSA 密钥（重启失效）
        var ep = RSA.Create(2048);
        var ekey = new RsaSecurityKey(ep) { KeyId = "ephemeral" };
        SigningCredentials = new SigningCredentials(ekey, SecurityAlgorithms.RsaSha256);
        ValidationKey = ekey;
        UseRsa = true;
        logger.LogWarning("未配置任何 JWT 密钥，已生成临时内存 RSA 密钥（重启后失效），仅供开发使用。");
    }
}
