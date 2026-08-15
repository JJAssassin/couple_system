# CoupleLoveSystem.Api（后端切片 · 可运行地基）

技术栈：ASP.NET Core 8 + EF Core 8 + Pomelo(MySql) + JWT + Serilog + BCrypt + HtmlSanitizer。

## 本切片已实现
- 分层结构：`Core`(实体/枚举/DTO/Result/Options) · `Infrastructure`(DbContext/Repository) · `Application`(Service/Filter) · `Api`(Controller/Middleware)。
- 统一返回 `ApiResult<T>` + 错误码 + 全局异常中间件（Forbidden/NotFound/Conflict/Unauthorized → 对应 HTTP 码）。
- JWT 双 Token：登录/刷新/登出；RefreshToken 走 `ITokenStore`（当前为内存实现，上线替换为 Redis）。
- **后端兜底权限过滤** `PermissionFilter`（`IProtectable` + `WhereVisible`/`EnsureVisible`），绝不信任前端 ID。
- 实体 14 张表 + 逻辑删除全局过滤 + 索引/外键配置（见 `CoupleDbContext`）。
- 三个完整模块：**认证 / 首页(恋爱天数·看板·就近纪念日) / 纪念日(增删改查+分页)**。
- 开发期种子：自动建表 + 创建双账号 `partner_a` / `partner_b`，密码均为 `123456`。

## 运行前置
- .NET 8 SDK
- MySQL 8.4（本机 `bind-address=127.0.0.1`），建库 `couple_love`（字符集 utf8mb4）
- 改 `appsettings.json`：`ConnectionStrings.MySql` 账号密码、`Jwt.Secret` 为 ≥32 字节随机串

```bash
dotnet run --project CoupleLoveSystem.Api
# Swagger: http://localhost:5199/swagger
```

## 生产化待办（不在本切片）
- 用 `dotnet ef migrations add Init && Update-Database` 替代 `EnsureCreated` 种子。
- `ITokenStore` 换 Redis 实现（按文档 `auth:rt:{userId}:{deviceId}`）。
- 接入 Quartz（纪念日提醒/书信解锁/凌晨统计）、文件上传、导出、其余 8 个模块、FluentValidation 校验、CORS 白名单、HTTPS/HSTS、富文本 HtmlSanitizer 净化。
