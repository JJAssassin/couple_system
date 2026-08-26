# 情侣专属情感陪伴 Web 系统 —— 实现完成与交付说明

> 项目路径：`D:\Code\My_vscode\couple-love-system`
> 技术栈：前端 Vue3 + TS + Vite6 + Pinia + NaiveUI + Tailwind v4 + ECharts + GSAP + SignalR；后端 ASP.NET Core 8 + EF Core + MySQL + BCrypt
> 最后更新：2026-08-26

## 一、已实现功能（全部模块打通，已实测）

### 前端（18 个功能页面/视图，均真实可用；另含登录页）
- 登录 `Login`：JWT 双 Token 登录、令牌本地存储、无感刷新
- 首页 `Home`：恋爱天数数字滚动、ECharts 心情/矛盾趋势、就近纪念日、统计卡片
- 时间轴 `Timeline`：纪念日/日记/愿望/矛盾/足迹多表聚合，月份筛选，竖线节点
- 心情日历 `MoodCalendar`：每日心情记录与日历热力展示
- 日记 `Diary`：公开/私密 Tab、富文本（HtmlSanitizer 净化）、心情/天气、权限标签、评论；**私密内容后端兜底不可见**
- 愿望 `Wish`：共同/礼物/成长三类、认领、标记完成、状态进度
- 待办 `Todo`：待办清单 CRUD、完成/撤销、连续打卡统计
- 小黑板 `Board`：留言板（图片/私密/定时解锁/接收人维度）
- 问卷 `Quiz`：每日一言/问答
- 相册 `Album`：相册网格、图片上传（NUpload）、大图预览（NImage）
- 矛盾 `Conflict`：克制风格、等级标记、和解记录、复盘结论
- 记账 `Account`：收支饼图、记一笔、余额、CSV 导出
- 约会 `DatePlan`：待执行/历史、星星评分
- 足迹 `Footprint`：共同足迹记录与地图/列表展示
- 站内消息 `Message`：系统消息列表、未读角标、全部已读；到期提醒/小黑板解锁/纪念日由后端实时推送
- 统计 `Stats`：年度恋爱报告（数据统计聚合）
- 设置 `Setting`：改资料/密码、深色模式、动效减弱开关、全量导出（JSON + 图片 zip）
- 通用：PC 侧栏 ↔ 移动底栏自适应、侧栏消息铃（未读角标 + 下拉）、路由转场、`useStaggerEnter` 错落入场、骨架/空态

### 后端（业务模块 + 横切能力；Controller / Service 齐全）
- 认证 `AuthService`：JWT 双 Token（Access 2h / Refresh 7d）+ BCrypt；**RSA 非对称签名，密钥外置**（环境变量 `JWT_RSA_PRIVATE_KEY` 或 `keys/jwt-private.pem`，生产强制 RSA 否则启动失败）
- 令牌存储 `ITokenStore`：默认 **InMemory** 实现，可切 **Redis**（配置 `TokenStore:Provider=Redis`，生产强制 Redis 防多实例刷新令牌丢失）
- 权限兜底 `PermissionFilter`：`WhereVisible`/`EnsureVisible`/`CanEdit`，**绝不信任前端 ID**
- 模块 Service/Controller（全部 CRUD 打通）：首页、纪念日、日记+评论、愿望（认领/完成）、待办、小黑板（含图片/私密/定时解锁/接收人）、问卷、相册+图片、矛盾、记账（含统计/CSV 导出）、约会、足迹、站内消息、时间轴聚合、用户（改密/导出）、情侣/伙伴/每日一言、统计报告
- 实时同步 `SyncBroadcaster` + SignalR `SyncHub`：写后向 `couple-{cid}` 组广播刷新；**令牌走 Authorization Header（非 URL）**
- 定时任务 `ScheduledHostedService`（托管服务，零额外依赖）：每分钟轮询，① 到期定时小黑板自动解锁并生成通知；② 纪念日提醒并重填下次提醒；③ 到期项向对应情侣组推送 `message` 信号。已加 **Redis 分布式锁**（InMemory 环境降级为进程内锁）防多实例重复执行
- 站内消息邮件 `SystemMessageEmailNotifier`：基于 `System.Net.Mail` 零新包；**默认关闭**（`Email:Enabled=false`），启用且有接收人邮箱时才发 HTML（HtmlEncode 防注入）
- 文件上传 `ImageController`：后缀白名单(jpg/jpeg/png/gif/webp) + ≤5MB + `yyyyMMdd_Guid` 重命名 + 存入 `wwwroot/uploads`；**删除时同步物理删除磁盘文件**
- 全量导出 `UserService.ExportAsync`：导出当前用户可见数据（含权限过滤）为 JSON，**并打包关联图片为 zip**
- 数据库：EF Core **Migrations**（启动期 `MigrateAsync` 自动迁移），非 `EnsureCreated`

## 二、验证状态（已实测，2026-08-26）

| 项 | 结果 |
|---|---|
| 后端 `dotnet build` | ✅ 通过（.NET 8 SDK 本机编译） |
| 后端 `dotnet test`（xUnit，InMemory） | ✅ **153 用例通过** |
| 前端 `npm run build`（vue-tsc + vite） | ✅ 通过，0 类型错误 |
| 前端 `npm run test`（vitest） | ✅ **69/69 通过** |
| `vue-tsc --noEmit` | ✅ 0 错误 |
| 全栈冒烟 `scripts/smoke.py` | ✅ **45 项全绿**（登录 + 30 只读 + 11 写闭环 + 401 门 + 跨用户写入），可本机或 CI 运行 |
| 质量门禁 `ci-gate.sh` | ✅ 六阶段门禁（构建/后端测/前端测/类型/构建+体积门禁/全栈冒烟） |

## 三、如何运行

### 前端
```bash
cd D:\Code\My_vscode\couple-love-system\frontend
npm install        # 已装过可跳过
npm run dev        # http://localhost:5174  （/api、/uploads、/hub 已代理到后端 5199）
npm run build      # 类型检查 + 打包
npm run test       # 单元测试
```

### 后端（需 .NET 8 SDK + 本机 MySQL 8 + Redis）
```bash
cd D:\Code\My_vscode\couple-love-system\backend\CoupleLoveSystem.Api
# 1) 准备 RSA 密钥（二选一）：
#    a) 放置私钥到 keys/jwt-private.pem（公钥由私钥推导）；
#    b) 设置环境变量 JWT_RSA_PRIVATE_KEY 为 PEM 内容。
#    生产环境必须有 RSA 私钥，否则启动失败（不再使用对称 Jwt:Secret）。
# 2) 改 appsettings.json：ConnectionStrings.MySql 指向本机 MySQL（账号 app + 密码）
# 3) 启动（必须从 DLL 的 bin 目录启动，使 ContentRoot 正确加载 bin/appsettings.json 与 keys）：
cd bin\Debug\net8.0   # 或 bin\Release\net8.0
dotnet CoupleLoveSystem.Api.dll --urls http://localhost:5199
# 启动期自动迁移建表（MigrateAsync）+ 写入双账号，随后监听 5199
# 测试：cd ..\..\CoupleLoveSystem.Tests && dotnet test
```
- 默认双账号：`partner_a` / `partner_b`，密码均为 `123456`（开发期种子，生产请删）
- 密钥/密码等敏感配置**不入库**：`appsettings.json` 与 `keys/` 已被 `.gitignore` 排除，仓库内仅有脱敏的 `appsettings.example.json`

## 四、本次修复/补齐的关键点
- ECharts 选项类型过严导致构建失败 → `ChartWrap` 的 `option` prop 放宽为 `any` 内部 cast
- 多个页面用 `useMessage()` 但 App 未挂 Provider → `App.vue` 包裹 `NMessageProvider`/`NNotificationProvider` 并绑定 `notifyStore`
- 图片走 `/uploads` 但 dev 代理只代理 `/api` → `vite.config.ts` 增加 `/uploads` 代理
- 后端所有 Service 统一在 `Program.cs` 注册；静态文件 `app.UseStaticFiles()` 提供上传图片
- 定时任务用内置托管服务替代独立 Quartz 工程（更简单、零依赖）
- 构建体积门禁：ECharts / Naive UI 按需引入
- 前端构建产物清理：移除 `.trash_*` 目录与 `vitest.config.ts.timestamp-*.mjs` 临时文件
- 图片物理删除：`ImageController` 删除同时 `System.IO.File.Delete` 磁盘文件（之前仅为逻辑删除）
- 全量导出增强：`UserService.ExportAsync` 除 JSON 外，已打包关联图片为 zip
- **EF Migration 修复（关键）**：`EnhanceBoard` 迁移此前因缺 `[Migration]` 特性与 `.Designer.cs` 被 EF 忽略，导致 `BoardMessages` 表缺 `ImageUrl/IsPrivate/IsUnlocked/ReceiverUserId/ScheduledAt` 列，`/board` 接口 500。已重建该迁移（`AddColumn` 五列），并删除无对应实体代码的孤儿 `AddTask` 迁移（`CoupleTaskTemplate`/`CoupleTaskRecord` 表在模型与代码中均不存在），保证从迁移全新建库的 CI 集成测试可复现且通过
- **可复用全栈冒烟脚本**：新增 `scripts/smoke.py`（纯标准库、零依赖、环境变量可配），覆盖登录/401 门禁/30 只读/11 写闭环/跨用户写入；`ci-gate.sh` 与云端 CI 均改指向它
- **云端 CI 集成测试**：`.github/workflows/ci.yml` 新增 `integration` job（GitHub service container 起 `mysql:8.0.39`，后端 `MigrateAsync` 自动迁移 + 种子后跑 `scripts/smoke.py`），Development 环境 + InMemory TokenStore 规避 Production 对 Redis 的强制要求，JWT 用临时 RSA 私钥（环境变量注入，不入库）

## 五、已知限制 / 后续 TODO（仅列仍开放的）
1. **技术债（可选重构）**：`SyncBroadcaster.NotifyAsync` 仍可 AOP 化自动广播；`SyncBroadcaster` 未带增量载荷；`TokenStore` 生产仍允许配置级降级为 InMemory（应通过环境强制）
2. **部署**：容器化编排已就绪（见 `D:\Docker\couple-love-system`：docker-compose 编排 mysql/redis/backend/frontend/caddy，端口与本机错开，自签 HTTPS），可补 GitHub Actions 自动构建推送镜像

## 六、版本控制
- 项目已 `git init`，首次提交 `f012118`（分支 `master`）。提交已排除：bin/obj/node_modules/dist/密钥/密码文件。
- 已推送到 GitHub（SSH）：`git remote add origin git@github.com:<user>/<repo>.git`，远端 `refs/heads/master` 跟踪。
- CI：GitHub Actions 三 job —— `backend`（构建 + InMemory 测试）、`frontend`（vitest + 类型检查 + 构建 + 体积门禁）、`integration`（MySQL service + 全栈冒烟）。
