# 情侣专属情感陪伴 Web 系统

> 一款专为情侣设计的**情感陪伴 Web 应用**：恋爱天数、纪念日、心情日记、愿望清单、相册共享、矛盾复盘、定时书信、共同记账、约会规划、共同足迹、站内消息等模块，支持实时同步与定时提醒。架构与文档一致：后端 ASP.NET Core 8 六边形分层，前端 Vue3 + TS + Vite + Pinia + NaiveUI + Tailwind v4 + ECharts。
>
> 最后更新：2026-08-16

---

## 项目总览

覆盖 **14 个业务模块 + 认证/首页/文件/实时同步等横切能力**，前后端均已实现并打通，并通过后端 65 项、前端 26 项自动化测试。

### 技术栈总览

| 层级 | 技术选型 |
|------|----------|
| 后端框架 | ASP.NET Core 8（单项目六边形分层） |
| ORM | Entity Framework Core 8 + Pomelo.EntityFrameworkCore.MySql |
| 数据库 | MySQL 8.4（utf8mb4） |
| 缓存/锁/令牌 | Redis（StackExchange.Redis） |
| 认证 | JWT 双 Token（Access + Refresh）+ BCrypt；**RSA 非对称签名，密钥外置** |
| 实时 | SignalR（`@microsoft/signalr`） |
| 日志 | Serilog（文件滚动 + 控制台） |
| 前端框架 | Vue 3.5 + TypeScript 5.7 + Vite 6 |
| 状态管理 | Pinia 2（auth / notify / setting 三个 store） |
| UI 框架 | NaiveUI + Tailwind CSS v4 |
| 图表 | ECharts 5.5（按需引入，心情趋势、矛盾趋势、记账饼图） |
| 动画 | GSAP 3.12（交错入场、数字滚动） |
| 富文本 | @wangeditor/editor v5 + HtmlSanitizer 净化 |
| 图标 | @iconify/vue |

---

## 项目结构（关键目录）

```
couple-love-system/
├─ README.md / DELIVERY.md
├─ .gitignore                  # 已排除 bin/obj/node_modules/dist/密钥/密码
├─ ci-gate.sh                  # 六阶段门禁（构建/测试/类型/体积/冒烟）
├─ backend/
│  └─ CoupleLoveSystem.Api/
│     ├─ Core/                 # 核心层（零依赖）：Entities / Enums / Dtos / Result / Options
│     ├─ Infrastructure/       # Persistence(CoupleDbContext+Migrations+DbSeeder) / Repositories
│     ├─ Application/          # Services(22) / Filters(PermissionFilter)
│     ├─ Api/                  # Controllers(20) / Middlewares / Hubs(SyncHub)
│     ├─ Program.cs
│     ├─ appsettings.json      # 已 gitignore（含 DB/SMTP 密码）
│     └─ keys/jwt-private.pem  # 已 gitignore（RSA 私钥，生产必需）
└─ frontend/
   ├─ .env                     # VITE_API_BASE=/api
   ├─ src/
   │  ├─ main.ts / App.vue
   │  ├─ store/                # authStore / notifyStore / settingStore
   │  ├─ composables/          # useDevice / useRealtime(SignalR) / useStaggerEnter
   │  ├─ router/               # 路由守卫 + 懒加载
   │  ├─ components/           # layout(AppShell/Sidebar/TabBar) / ChartWrap / Common
   │  └─ views/                # Login/Home/Timeline/Diary/Wish/Album/Message/Conflict/
   │                          #   Letter/Account/DatePlan/Footprint/Anniversary/Setting
   │                          #   （CheckIn、Score 目录已废弃，未路由）
   └─ vite.config.ts           # 代理 /api、/uploads、/hub；组件按需
```

---

## 数据模型（15 张实体表）

| 实体 | 说明 | 关键字段 |
|------|------|----------|
| `CoupleUser` | 用户 | UserName, NickName, PasswordHash(BCrypt), Avatar, LoveStartTime, RoleType(PartnerA/B), Email |
| `CoupleAnniversary` | 纪念日 | Name, AnniversaryType, TargetDate, CoverImage, RemindDays(0/1/3/7/15), NextRemindTime |
| `CoupleDiary` | 日记 | Title, Content(HtmlSanitizer净化), MoodTag, MoodScore(1-10), PermissionType, Weather, DiaryDate |
| `CoupleDiaryComment` | 日记评论 | DiaryId, Content |
| `CoupleWish` | 愿望 | WishType, Title, Description, ExpectTime, Priority, Status, ClaimUserId |
| `CoupleAlbum` | 相册 | AlbumName, Cover, Remark |
| `CoupleImage` | 照片 | AlbumId, ImagePath, ShootTime, Location |
| `CoupleConflict` | 矛盾 | OccurTime, Summary, ConflictLevel(1-3), MyThoughtA/B, ReconcileTime/Way, ReflectA/B, RuleConclusion |
| `CoupleLetter` | 书信 | ReceiverUserId, Content, CoverImage, UnlockTime(服务器时间), IsUnlocked |
| `CoupleAccountRecord` | 记账 | RecordType(Income/Expend), Category, Amount, RecordTime, Remark |
| `CoupleDateRecord` | 约会 | IsCompleted, PlanTime, RealTime, Location, Budget, RealCost, ExperienceScore(1-5) |
| `CoupleSystemMessage` | 系统消息 | ReceiverUserId, Title, Content, MessageType, IsRead |
| `CoupleFootprint` | 共同足迹 | 地点/时间/备注等 |
| `CoupleQuote` | 每日一言 | 文案/作者 |
| `CoupleSetting` | 情侣级设置 | 键值配置 |

**全局特性：**
- 所有实体继承 `BaseEntity`：统一 `Id/CreateUserId/CreateTime/UpdateUserId/UpdateTime/IsDeleted`
- 全局逻辑删除过滤：EF Core QueryFilter 自动排除 `IsDeleted = true`（后台作业用 `IgnoreQueryFilters`）
- 全局情侣隔离：`CoupleId` 查询过滤器 + `SaveChanges` 拦截器自动盖章
- 中文友好：全表 `utf8mb4`

---

## 已实现功能

### 后端（全部完整，非占位）

| 模块 | 状态 | 说明 |
|------|------|------|
| 认证 | ✅ | JWT 双 Token（Access 2h / Refresh 7d），BCrypt 密码，RSA 非对称签名，无感刷新；令牌存 Redis |
| 首页/仪表盘 | ✅ | 恋爱天数、就近纪念日、心情趋势(30天)、矛盾趋势(6月)、愿望完成率、余额 |
| 纪念日 | ✅ | 增删改查 + 分页 + 软删除 + 提前提醒（0/1/3/7/15天） |
| 日记+评论 | ✅ | 公开/私密、HtmlSanitizer 净化、后端权限兜底 |
| 愿望 | ✅ | 共同/礼物/成长、认领、完成 |
| 相册+图片 | ✅ | 上传白名单 + 大小校验 + 静态文件服务 |
| 矛盾 | ✅ | 等级标记、和解/复盘 |
| 书信 | ✅ | 仅接收人可见、**定时解锁** |
| 记账 | ✅ | 收支、饼图、余额 |
| 约会 | ✅ | 计划/历史、评分 |
| 足迹 | ✅ | 共同足迹记录 |
| 站内消息 | ✅ | 列表/未读/已读；**到期提醒、书信解锁、纪念日由后端实时推送 + SMTP 邮件**（默认关闭） |
| 时间轴 | ✅ | 多表聚合混合排序 |
| 数据权限 | ✅ | `PermissionFilter` 后端兜底：Public / PrivateSelf / ViewOnlyOther |
| 实时同步 | ✅ | SignalR `SyncHub`，写后向 `couple-{cid}` 组广播；令牌走 Header |
| 定时任务 | ✅ | `ScheduledHostedService` 每分钟；Redis 分布式锁 |
| 统一返回/日志/种子 | ✅ | `ApiResult<T>` + 全局异常中间件；Serilog；双账号种子 |

### 前端（13 个功能页 + 登录，全部完整）

| 模块 | 状态 | 说明 |
|------|------|------|
| 登录 | ✅ | 双 Token 存储、路由守卫、错误提示 |
| 布局 | ✅ | PC 侧栏 + 移动端底部 TabBar + 页面过渡 + 安全区适配 |
| 首页 | ✅ | 数字滚动(GSAP)、ECharts 趋势、统计卡片 |
| 时间轴 | ✅ | 多表聚合、月份筛选 |
| 日记 | ✅ | 富文本、私密兜底、评论 |
| 愿望/相册/矛盾/书信/记账/约会/足迹/站内消息/纪念日 | ✅ | 各自完整交互 |
| 设置 | ✅ | 深色模式、动效减弱、导出 |
| 实时刷新 | ✅ | `useRealtime().onSync(module, load)` 订阅后端推送 |
| 请求封装 | ✅ | Axios 拦截器（Token 注入、401 并发刷新锁、统一错误提示） |

---

## 快速开始

### 环境要求
- **后端**：.NET 8 SDK、MySQL 8.4、Redis
- **前端**：Node.js 20+、npm

### 启动步骤

**1. 准备后端密钥**
```bash
cd D:\Code\My_vscode\couple-love-system\backend\CoupleLoveSystem.Api
# RSA 私钥（二选一）：
#   a) 放置私钥到 keys/jwt-private.pem
#   b) 设置环境变量 JWT_RSA_PRIVATE_KEY（PEM 文本）
# 生产环境必须提供 RSA 私钥，否则启动失败（已弃用对称 Jwt:Secret）
```

**2. 配置数据库 / Redis**
```bash
# MySQL 建库
mysql -u root -p
CREATE DATABASE couple_love CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
# 改 appsettings.json：ConnectionStrings.MySql 密码、TokenStore/Redis 地址
```

**3. 启动后端**
```bash
dotnet run --urls http://localhost:5199
# 启动期自动迁移建表 + 种子数据
# Swagger：http://localhost:5199/swagger
```

**4. 启动前端（新终端）**
```bash
cd frontend
npm install
npm run dev        # http://localhost:5174  （/api、/uploads、/hub 代理到 5199）
```

**5. 登录**
- 账号：`partner_a` 或 `partner_b`；密码：`123456`

---

## 架构设计

### 后端：六边形分层（单项目实现）
```
Core（纯业务，零框架依赖）：Entities / Enums / Dtos / Result / Options
Infrastructure（外部适配）：Persistence(EF DbContext + Migrations + 种子) / Repositories
Application（用例编排）：Services(22) / Filters(PermissionFilter / 全局 CoupleId 拦截)
Api（接口适配）：Controllers(20) / Hubs(SyncHub) / Middlewares(全局异常)
```

**关键设计原则：**
- 后端兜底权限：`IProtectable` + `PermissionFilter`，前端只做 UI 隐藏
- 全局情侣隔离：`CoupleId` 过滤器 + `SaveChanges` 拦截器盖章
- 逻辑删除：全局 QueryFilter，不物理删除情感数据
- JWT 双 Token：**RSA 非对称签名**（Access 2h + Refresh 7d），刷新令牌存 Redis，可主动吊销
- 实时同步：SignalR 广播 + 前端 `onSync` 订阅刷新
- 定时任务：内置托管服务 + Redis 分布式锁，每分钟轮询提醒/解锁

### 前端：组件化 + 响应式
- 路由：懒加载 + 路由守卫（未登录跳登录页）
- 状态：Pinia 三个 store（auth / notify / setting）
- 布局：PC 侧栏 + 移动端底部 TabBar 自适应
- 实时：`useRealtime()` 封装 SignalR 连接与 `onSync` 订阅
- 图表：ECharts 按需引入（构建体积门禁）
- 主题：CSS 变量 + 深色模式 + 动效减弱

---

## API 端点（代表性，完整见 Swagger）

| 方法 | 路径 | 说明 | 认证 |
|------|------|------|------|
| POST | `/api/auth/login` | 登录（返回双 Token） | 否 |
| POST | `/api/auth/refresh` | 刷新 Access Token | 否 |
| POST | `/api/auth/logout` | 登出（吊销 Refresh Token） | 是 |
| GET | `/api/home/loveinfo` | 恋爱信息（天数/小时/分钟） | 是 |
| GET | `/api/home/dashboard` | 仪表盘数据 | 是 |
| GET | `/api/anniversary/list` | 纪念日列表（分页） | 是 |
| GET/POST/PUT/DELETE | `/api/{module}/...` | 各模块 CRUD（anniversary/diary/wish/album/conflict/letter/account/dateplan/footprint/message/timeline） | 是 |
| GET | `/api/message/unread/count` | 未读消息数 | 是 |
| GET | `/api/sync/negotiate` | SignalR 协商（令牌走 Header） | 是 |

---

## 前端页面路由（活跃模块）

| 路径 | 页面 | 状态 |
|------|------|------|
| `/login` | 登录页 | ✅ |
| `/home` | 首页 | ✅ |
| `/timeline` | 恋爱时间轴 | ✅ |
| `/diary` | 双人日记 | ✅ |
| `/wish` | 愿望清单 | ✅ |
| `/album` | 双人相册 | ✅ |
| `/message` | 站内消息 | ✅ |
| `/conflict` | 矛盾复盘 | ✅ |
| `/letter` | 悄悄话 & 定时书信 | ✅ |
| `/account` | 共同记账 | ✅ |
| `/dateplan` | 约会计划 | ✅ |
| `/footprint` | 共同足迹 | ✅ |
| `/anniversary` | 纪念日管理 | ✅ |
| `/setting` | 设置与备份 | ✅ |

> `CheckIn`（打卡）、`Score`（互评）目录仍存在于 `src/views/`，但已退出路由与导航（后端实体已移除），属历史遗留，请勿使用。

---

## 核心特性详解

### 1. JWT 双 Token + RSA 非对称签名
- **Access Token**：2 小时，携带在 `Authorization: Bearer` Header
- **Refresh Token**：7 天，存 Redis（`auth:rt:` 前缀），可主动吊销
- **签名**：RSA 非对称（RS256）。`JwtKeyResolver` 从环境变量 `JWT_RSA_PRIVATE_KEY` 或 `keys/jwt-private.pem` 读取私钥；生产环境缺失则启动失败（杜绝对称密钥泄露）
- **无感刷新**：前端 Axios 拦截器检测 401 时自动刷新，并发请求加锁避免重复刷新

### 2. 后端兜底权限
```
PermissionType：Public（双方读写）/ PrivateSelf（仅本人可见）/ ViewOnlyOther（双方可读，仅 owner 可写）
```
`PermissionFilter.WhereVisible()` 列表时动态拼接 WHERE，`EnsureVisible()` 详情时抛异常。前端只做 UI 隐藏，绝不信任前端传入的权限参数。

### 3. 逻辑删除 + 全局情侣隔离
- 所有 `BaseEntity` 子类带 `IsDeleted`，全局 QueryFilter 自动排除
- `CoupleId` 全局过滤 + `SaveChanges` 拦截器自动盖章，后台作业用 `IgnoreQueryFilters`

### 4. 实时同步（SignalR）
- 九大模块写后 `SyncBroadcaster.NotifyAsync(module)` 向 `couple-{cid}` 组广播
- 前端 `useRealtime().onSync(module, load)` 订阅刷新；令牌通过 Header 传递（非 URL）

### 5. 定时任务 + 实时提醒
- `ScheduledHostedService` 每分钟轮询：定时书信到期解锁并生成系统消息；纪念日提前提醒
- 到期项向对应情侣组推送 `message` 信号，前端铃铛角标实时更新
- 已加 Redis 分布式锁，支持多实例部署不重复执行

### 6. 站内消息邮件通知
- `SystemMessageEmailNotifier` 基于 `System.Net.Mail`（零新包）
- `Email:Enabled=false` 默认关闭；启用且有接收人邮箱才发 HTML（HtmlEncode 防注入）

---

## 配置说明

### 后端 `appsettings.json`（已 gitignore，仓库仅有 `appsettings.example.json`）
| 配置项 | 说明 |
|--------|------|
| `ConnectionStrings.MySql` | MySQL 连接串（含 app 账号密码） |
| `Jwt.AccessExpireMinutes` / `Jwt.RefreshExpireDays` | 120 / 7 |
| `Jwt.Issuer` / `Jwt.Audience` | CoupleLove / CoupleLoveClient |
| `TokenStore.Provider` | Redis（默认），`auth:rt:` 前缀 |
| `Email.Enabled` / `SmtpHost` / ... | SMTP 邮件，默认关闭 |

> ⚠️ 不再使用 `Jwt.Secret` 对称密钥；签名改为 RSA，私钥来自环境变量或 `keys/jwt-private.pem`。

### 前端 `.env`
| 变量 | 说明 |
|------|------|
| `VITE_API_BASE` | 后端 API 地址（`/api`，Vite 代理到 5199） |

---

## NuGet 依赖（核心）
| 包 | 用途 |
|---|------|
| Pomelo.EntityFrameworkCore.MySql | MySQL EF Core 提供程序 |
| Microsoft.AspNetCore.Authentication.JwtBearer | JWT 认证（RS256） |
| Microsoft.AspNetCore.SignalR | 实时同步 |
| StackExchange.Redis | 令牌存储 / 分布式锁 / 缓存 |
| Swashbuckle.AspNetCore | Swagger 文档 |
| Serilog.AspNetCore | 结构化日志 |
| BCrypt.Net-Next | 密码哈希 |
| HtmlSanitizer | 富文本 XSS 防护 |

---

## 开发约定
1. 后端：新增模块遵循 `Entity → Repository → Service → Controller`，统一返回 `ApiResult<T>`
2. 前端：页面放 `src/views/`，在 `router/index.ts` 注册，复用 `AppShell`
3. 权限：用户数据实体实现 `IProtectable`，Service 调 `PermissionFilter` 后端兜底
4. 密码：永远 BCrypt 哈希，禁止明文
5. 删除：全部软删除（`IsDeleted = true`）
6. 时间：后端统一 UTC，前端按需转本地；定时解锁/提醒以服务器时间为准
7. 密钥/密码：绝不入库（`keys/` 与 `appsettings.json` 已被 `.gitignore` 排除）

---

## 生产化待办（仍开放）
- [ ] 图片物理删除回收策略（`ImageController` 当前仅逻辑删除 DB）
- [ ] `export/alldata` 图片打包 zip
- [ ] HTTPS/HSTS + CORS 白名单 + `uploads` 目录禁脚本执行 + MySQL 仅本机连接
- [ ] 技术债（可选）：`NotifyAsync` 手写 7 处改 AOP；`SyncBroadcaster` 增量载荷；`TokenStore` 生产禁 InMemory 降级
- [ ] docker-compose（MySQL + Redis + 后端 + 前端）+ CI/CD
- [ ] 推送远程仓库备份（当前无 git remote）

---

## License
MIT
