# 情侣专属情感陪伴 Web 系统（Version A · 地基切片）

> 根据《前端/后端详细设计方案（深化版）》实现的**第一版可运行代码**。架构与文档一致：后端 ASP.NET Core 8 六边形分层，前端 Vue3 + TS + Vite + Pinia + NaiveUI + Tailwind v4 + ECharts。

---

## 项目总览

这是一款专为情侣设计的**情感陪伴 Web 应用**，覆盖恋爱天数追踪、纪念日管理、心情日记、愿望清单、相册共享、矛盾复盘、书信传递、打卡激励、共同记账、约会规划等 11 个核心模块。当前切片完成了地基架构 + 三条完整垂直链路（认证 → 首页 → 纪念日），其余模块已搭好脚手架，沿用同套结构即可扩展。

### 技术栈总览

| 层级 | 技术选型 |
|------|----------|
| 后端框架 | ASP.NET Core 8（单项目六边形分层） |
| ORM | Entity Framework Core 8 + Pomelo.EntityFrameworkCore.MySql |
| 数据库 | MySQL 8.4（utf8mb4） |
| 认证 | JWT 双 Token（Access + Refresh）+ BCrypt 密码哈希 |
| 日志 | Serilog（文件 + 控制台） |
| 前端框架 | Vue 3.5 + TypeScript 5.7 + Vite 6 |
| 状态管理 | Pinia 2（auth / notify / setting 三个 store） |
| UI 框架 | NaiveUI + Tailwind CSS v4 |
| 图表 | ECharts 5.5（心情趋势、矛盾趋势） |
| 动画 | GSAP 3.12（交错入场动画） |
| 富文本 | @wangeditor/editor v5 |
| 图标 | @iconify/vue |

---

## 项目结构

```
couple-love-system/
├─ README.md                          # 本文件
├─ backend/
│  └─ CoupleLoveSystem.Api/           # ASP.NET Core 8 Web API（单项目六边形分层）
│     ├─ Core/                        # 核心层（零依赖）
│     │  ├─ Entities/                 # 14 张业务实体（含逻辑删除基类）
│     │  │  ├─ Entities.cs            #   CoupleUser, Anniversary, Diary, Wish, Album...
│     │  │  └─ IProtectable.cs        #   业务权限接口（供 PermissionFilter 兜底）
│     │  ├─ Enums/                    # 枚举：RoleType, PermissionType, AnniversaryType...
│     │  │  └─ Enums.cs
│     │  ├─ Dtos/                     # 请求/响应 DTO
│     │  │  └─ Dtos.cs                #   LoginReq/Resp, LoveInfo, DashboardData, AnniversaryDto...
│     │  ├─ Result/                   # 统一返回 + 异常类型
│     │  │  └─ ApiResult.cs           #   ApiResult<T>, ErrorCode, ForbiddenException...
│     │  └─ Options/                  # 配置选项
│     │     └─ JwtOptions.cs
│     ├─ Infrastructure/              # 外部适配层
│     │  ├─ Persistence/
│     │  │  ├─ CoupleDbContext.cs     #   EF DbContext（14 张 DbSet + 全局软删除过滤 + 索引）
│     │  │  └─ DbSeeder.cs            #   开发期种子（双账号 partner_a / partner_b）
│     │  └─ Repositories/
│     │     ├─ IRepository.cs         #   通用仓储接口 + EfRepository 实现
│     │     └─ IAnniversaryRepository.cs  # 纪念日专属仓储
│     ├─ Application/                 # 用例层
│     │  ├─ Services/
│     │  │  ├─ AuthService.cs         #   登录/刷新/登出（JWT + BCrypt + ITokenStore）
│     │  │  ├─ HomeService.cs         #   恋爱天数/仪表盘/就近纪念日
│     │  │  └─ AnniversaryService.cs  #   纪念日 CRUD + 分页 + 软删除
│     │  └─ Filters/
│     │     └─ PermissionFilter.cs    #   业务数据权限兜底（Public/PrivateSelf/ViewOnlyOther）
│     ├─ Api/                         # 接口适配层
│     │  ├─ Controllers/
│     │  │  ├─ BaseController.cs      #   基类（从 JWT 取 CurrentUserId，绝不信任前端参数）
│     │  │  ├─ AuthController.cs      #   POST login / refresh / logout
│     │  │  ├─ HomeController.cs      #   GET loveinfo / dashboard / nearestanniversary
│     │  │  └─ AnniversaryController.cs  #  GET list/{id} / POST create / PUT update / DELETE
│     │  └─ Middlewares/
│     │     └─ GlobalExceptionMiddleware.cs  # 业务异常 → 统一 ApiResult + HTTP 状态码
│     ├─ Program.cs                   # 应用入口（DI 配置、Middleware 管道）
│     ├─ CoupleLoveSystem.Api.csproj  # 项目文件（NuGet 依赖）
│     └─ appsettings.json             # 配置（MySQL 连接串 + JWT + Serilog）
│
└─ frontend/
   ├─ .env                            # VITE_API_BASE=/api
   ├─ package.json                    # 依赖清单
   ├─ index.html                      # HTML 入口
   ├─ tsconfig.node.json              # TS 配置
   ├─ src/
   │  ├─ main.ts                      # 应用入口（启动即应用主题偏好）
   │  ├─ App.vue                      # 根组件
   │  ├─ assets/
   │  │  └─ style/
   │  │     └─ global.scss            # 全局样式（CSS 变量 + Tailwind v4）
   │  ├─ types/
   │  │  └─ index.ts                  # TypeScript 类型定义（与后端 DTO 对齐）
   │  ├─ utils/
   │  │  └─ request.ts                # Axios 封装（无感刷新、错误提示、并发锁）
   │  ├─ store/
   │  │  ├─ authStore.ts              # 认证状态（双 Token、登录/登出/自动刷新）
   │  │  ├─ notifyStore.ts            # 消息通知（NaiveUI Message/Notification）
   │  │  └─ settingStore.ts           # 设置（深色模式、动效减弱偏好）
   │  ├─ composables/
   │  │  └─ useDevice.ts              # 设备检测（移动端判断）
   │  ├─ router/
   │  │  └─ index.ts                  # Vue Router（路由守卫、懒加载）
   │  ├─ components/
   │  │  ├─ layout/
   │  │  │  ├─ AppShell.vue           # 布局外壳（PC 侧栏 + 移动端 TabBar + 页面过渡）
   │  │  │  ├─ Sidebar.vue            # 侧边栏导航（PC）
   │  │  │  └─ TabBar.vue             # 底部标签栏（移动端）
   │  │  ├─ ChartWrap.vue             # ECharts 封装（自适应 + 动画）
   │  │  └─ Common/
   │  │     └─ LoveCount.vue          # 恋爱天数数字滚动组件
   │  └─ views/
   │     ├─ Login.vue                 # 登录页
   │     ├─ Home/
   │     │  └─ Index.vue              # 首页（恋爱天数 + 就近纪念日 + 心情趋势 + 矛盾趋势 + 统计卡片）
   │     ├─ Anniversary/               # 纪念日模块（完整 CRUD + 分页）
   │     │  └─ Index.vue
   │     ├─ Timeline/Index.vue         # 时间轴（占位）
   │     ├─ Diary/Index.vue            # 双人日记（占位）
   │     ├─ Wish/Index.vue             # 愿望清单（占位）
   │     ├─ Album/Index.vue            # 双人相册（占位）
   │     ├─ Conflict/Index.vue         # 矛盾复盘（占位）
   │     ├─ Letter/Index.vue           # 悄悄话 & 定时书信（占位）
   │     ├─ CheckIn/Index.vue          # 情侣打卡（占位）
   │     ├─ Account/Index.vue          # 共同记账（占位）
   │     ├─ DatePlan/Index.vue         # 约会计划（占位）
   │     └─ Setting/Index.vue          # 设置与备份（占位）
```

---

## 数据模型（14 张表）

| 实体 | 说明 | 关键字段 |
|------|------|----------|
| `CoupleUser` | 用户 | UserName, NickName, PasswordHash(BCrypt), Avatar, LoveStartTime, RoleType(PartnerA/B) |
| `CoupleAnniversary` | 纪念日 | Name, AnniversaryType, TargetDate, CoverImage, RemindDays(0/1/3/7/15), NextRemindTime |
| `CoupleDiary` | 日记 | Title, Content(HtmlSanitizer净化), MoodTag, MoodScore(1-10), PermissionType, Weather, DiaryDate |
| `CoupleDiaryComment` | 日记评论 | DiaryId, Content |
| `CoupleWish` | 愿望 | WishType, Title, Description, ExpectTime, Priority, Status(NotStart/Doing/Completed/Archive), ClaimUserId |
| `CoupleAlbum` | 相册 | AlbumName, Cover, Remark |
| `CoupleImage` | 照片 | AlbumId, ImagePath, ShootTime, Location |
| `CoupleConflict` | 矛盾 | OccurTime, Summary, ConflictLevel(1-3), MyThoughtA/B, ReconcileTime/Way, ReflectA/B, RuleConclusion |
| `CoupleLetter` | 书信 | ReceiverUserId, Content, CoverImage, UnlockTime(服务器时间), IsUnlocked |
| `CoupleCheckInItem` | 打卡项 | ItemName, Description, IsEnable |
| `CoupleCheckInRecord` | 打卡记录 | CheckInItemId, UserId, CheckInDateTime, Remark, Image |
| `CoupleAccountRecord` | 记账 | RecordType(Income/Expend), Category, Amount, RecordTime, Remark |
| `CoupleDateRecord` | 约会 | IsCompleted, PlanTime, RealTime, Location, Budget, RealCost, ExperienceScore(1-5) |
| `CoupleSystemMessage` | 系统消息 | ReceiverUserId, Title, Content, MessageType, IsRead |

**全局特性：**
- 所有实体继承 `BaseEntity`：统一 `Id/CreateUserId/CreateTime/UpdateUserId/UpdateTime/IsDeleted`
- 全局逻辑删除过滤：EF Core QueryFilter 自动排除 `IsDeleted = true` 的行
- 中文友好：全表 `utf8mb4` 字符集

---

## 已实现功能

### 后端

| 模块 | 状态 | 说明 |
|------|------|------|
| 认证 | ✅ 完整 | JWT 双 Token（Access 2h / Refresh 7天），BCrypt 密码，无感刷新 |
| 首页/仪表盘 | ✅ 完整 | 恋爱天数、就近纪念日、心情趋势(30天)、矛盾趋势(6月)、愿望完成率、余额、连续打卡 |
| 纪念日 | ✅ 完整 | 增删改查 + 分页 + 软删除 + 提前提醒（0/1/3/7/15天） |
| 数据权限 | ✅ 完整 | `PermissionFilter` 后端兜底：Public / PrivateSelf / ViewOnlyOther |
| 统一返回 | ✅ 完整 | `ApiResult<T>` + 错误码枚举 + 全局异常中间件（不暴露堆栈） |
| 日志 | ✅ 完整 | Serilog 文件滚动 + 控制台输出 |
| 种子数据 | ✅ 完整 | 开发期自动建表 + 双账号（partner_a / partner_b，密码 123456） |

### 前端

| 模块 | 状态 | 说明 |
|------|------|------|
| 登录页 | ✅ | 双 Token 存储、路由守卫、错误提示 |
| 布局 | ✅ | PC 侧栏 + 移动端底部 TabBar + 页面过渡动画 + 安全区适配 |
| 首页 | ✅ | 恋爱天数数字滚动(GSAP)、ECharts 心情趋势(折线)、矛盾趋势(柱状)、统计卡片 |
| 纪念日 | ✅ | 列表(分页)、详情、创建、编辑、删除、剩余天数计算 |
| 设置 | ✅ | 深色模式切换、动效减弱偏好（localStorage 持久化） |
| 响应式 | ✅ | PC 侧栏 ↔ 移动端 TabBar 自适应切换 |
| 请求封装 | ✅ | Axios 拦截器（Token 注入、401 并发刷新锁、统一错误提示） |

### 待实现模块（8 个，均为占位页）

| 模块 | 后端状态 | 前端状态 |
|------|----------|----------|
| 时间轴 | 实体已定义 | 占位页 |
| 双人日记 | 实体已定义 | 占位页 |
| 愿望清单 | 实体已定义 | 占位页 |
| 双人相册 | 实体已定义 | 占位页 |
| 矛盾复盘 | 实体已定义 | 占位页 |
| 悄悄话 & 定时书信 | 实体已定义 | 占位页 |
| 情侣打卡 | 实体已定义 | 占位页 |
| 共同记账 | 实体已定义 | 占位页 |
| 约会计划 | 实体已定义 | 占位页 |
| 设置与备份 | — | 占位页 |

---

## 快速开始

### 环境要求

- **后端**：.NET 8 SDK、MySQL 8.4
- **前端**：Node.js 20+、npm

### 启动步骤

**1. 克隆项目**
```bash
cd d:\Code\My_vscode\couple-love-system
```

**2. 配置数据库**
```bash
# 在 MySQL 中创建数据库
mysql -u root -p
CREATE DATABASE couple_love CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

**3. 修改后端配置**
```bash
# 编辑 backend/CoupleLoveSystem.Api/appsettings.json
# 修改 ConnectionStrings.MySql 中的密码
# 修改 Jwt.Secret 为 >= 32 字节的随机字符串
```

**4. 启动后端**
```bash
cd backend\CoupleLoveSystem.Api
dotnet run
# 自动建表 + 种子数据
# API 地址：http://localhost:5199
# Swagger：http://localhost:5199/swagger
```

**5. 启动前端（新终端）**
```bash
cd frontend
npm install
npm run dev
# 前端地址：http://localhost:5173
# /api 请求自动代理到 5199
```

**6. 登录**
- 账号：`partner_a` 或 `partner_b`
- 密码：`123456`

---

## 架构设计

### 后端：六边形分层（单项目实现）

```
Core（纯业务，零框架依赖）
  ├── Entities       → 领域实体（14 张表）
  ├── Enums          → 值对象（枚举）
  ├── Dtos           → 数据传输对象
  ├── Result         → 统一返回 + 异常类型
  └── Options        → 配置选项

Infrastructure（外部适配）
  ├── Persistence    → EF Core DbContext + 种子
  └── Repositories   → 仓储实现（IRepository<T> + EfRepository）

Application（用例编排）
  ├── Services        → 业务服务（Auth / Home / Anniversary）
  └── Filters         → 权限兜底（PermissionFilter）

Api（接口适配）
  ├── Controllers     → REST API（统一继承 BaseController）
  └── Middlewares      → 全局异常中间件
```

**关键设计原则：**
- 后端兜底权限：`IProtectable` + `PermissionFilter`，前端只做 UI 隐藏
- 逻辑删除：全局 QueryFilter，不物理删除情感数据
- 统一返回：`ApiResult<T>` 包裹所有响应，错误码枚举
- JWT 双 Token：Access Token 2h + Refresh Token 7天，请求拦截器无感刷新

### 前端：组件化 + 响应式

- **路由**：懒加载 + 路由守卫（未登录跳登录页）
- **状态**：Pinia 三个 store（auth / notify / setting）
- **布局**：PC 侧栏 + 移动端底部 TabBar 自适应
- **动画**：GSAP 交错入场 + 数字滚动 + 页面过渡
- **图表**：ECharts 封装（ChartWrap），自动响应式
- **请求**：Axios 拦截器（Token 注入 + 401 并发刷新 + 统一错误提示）
- **主题**：CSS 变量 + 深色模式 + 动效减弱

---

## API 端点

| 方法 | 路径 | 说明 | 认证 |
|------|------|------|------|
| POST | `/api/auth/login` | 登录（返回双 Token） | 否 |
| POST | `/api/auth/refresh` | 刷新 Access Token | 否 |
| POST | `/api/auth/logout` | 登出（吊销 Refresh Token） | 是 |
| GET | `/api/home/loveinfo` | 恋爱信息（天数/小时/分钟） | 是 |
| GET | `/api/home/dashboard` | 仪表盘数据（心情/矛盾/愿望/记账/打卡） | 是 |
| GET | `/api/home/nearestanniversary` | 就近纪念日（默认取 3 个） | 是 |
| GET | `/api/anniversary/list` | 纪念日列表（分页） | 是 |
| GET | `/api/anniversary/{id}` | 纪念日详情 | 是 |
| POST | `/api/anniversary/create` | 创建纪念日 | 是 |
| PUT | `/api/anniversary/update` | 更新纪念日 | 是 |
| DELETE | `/api/anniversary/delete` | 删除纪念日（软删除） | 是 |

---

## 前端页面路由

| 路径 | 页面 | 状态 |
|------|------|------|
| `/login` | 登录页 | ✅ |
| `/home` | 首页（恋爱天数 + 仪表盘） | ✅ |
| `/anniversary` | 纪念日管理 | ✅ |
| `/timeline` | 恋爱时间轴 | ⏳ 占位 |
| `/diary` | 双人日记 | ⏳ 占位 |
| `/wish` | 愿望清单 | ⏳ 占位 |
| `/album` | 双人相册 | ⏳ 占位 |
| `/conflict` | 矛盾复盘 | ⏳ 占位 |
| `/letter` | 悄悄话 & 定时书信 | ⏳ 占位 |
| `/checkin` | 情侣打卡 | ⏳ 占位 |
| `/account` | 共同记账 | ⏳ 占位 |
| `/dateplan` | 约会计划 | ⏳ 占位 |
| `/setting` | 设置与备份 | ⏳ 占位 |

---

## 核心特性详解

### 1. JWT 双 Token 认证

- **Access Token**：有效期 2 小时，携带在 Authorization Header 中
- **Refresh Token**：有效期 7 天，存储在 `ITokenStore`（当前内存实现，生产换 Redis）
- **无感刷新**：前端 Axios 拦截器检测 401 时，自动用 Refresh Token 获取新 Access Token，并发请求加锁避免重复刷新

### 2. 后端兜底权限

```
PermissionType 枚举：
  Public       → 双方可读写
  PrivateSelf  → 仅本人可见，对方 API 直接返回 403
  ViewOnlyOther→ 双方可读，仅 owner 可写
```

`PermissionFilter.WhereVisible()` 在列表查询时动态拼接 WHERE 条件，`EnsureVisible()` 在详情时直接抛异常。前端只做 UI 隐藏，绝不信任前端传入的权限参数。

### 3. 逻辑删除

所有 `BaseEntity` 子类自动带 `IsDeleted` 字段，EF Core `OnModelCreating` 中通过 `HasQueryFilter` 全局过滤，查询时自动排除已删除数据，不物理删除情感数据。

### 4. 首页仪表盘

- **恋爱天数**：GSAP 数字滚动动画，精确到分钟
- **就近纪念日**：显示未来最近的 3 个纪念日及剩余天数
- **心情趋势**：ECharts 折线图，近 30 天每日平均心情评分（1-10）
- **矛盾趋势**：ECharts 柱状图，近 6 个月每月矛盾次数
- **统计卡片**：愿望完成率、共同余额、连续打卡天数

### 5. 纪念日模块

- 支持类型：恋爱纪念日、生日、相识日、自定义
- 提前提醒：可设置 0/1/3/7/15 天前提醒
- 分页列表 + 详情 + 创建/编辑 + 软删除
- 封面图支持

### 6. 响应式设计

- **PC（≥768px）**：左侧固定侧栏 + 右侧内容区
- **移动端（<768px）**：隐藏侧栏，底部 TabBar 导航，内容区自动适配安全区

### 7. 深色模式 & 动效减弱

- 深色模式：CSS 变量切换，偏好持久化到 localStorage
- 动效减弱：添加 `reduce-motion` class，GSAP 和 CSS transition 自动降级

---

## 配置说明

### 后端 `appsettings.json`

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| `ConnectionStrings.MySql` | MySQL 连接串 | `Server=127.0.0.1;Port=3306;Database=couple_love;...` |
| `Jwt.Secret` | JWT 签名密钥（≥32 字节） | 需手动替换 |
| `Jwt.AccessExpireMinutes` | Access Token 有效期 | 120（2小时） |
| `Jwt.RefreshExpireDays` | Refresh Token 有效期 | 7 |
| `Jwt.Issuer` / `Audience` | JWT 发行方/受众 | CoupleLove / CoupleLoveClient |

### 前端 `.env`

| 变量 | 说明 | 默认值 |
|------|------|--------|
| `VITE_API_BASE` | 后端 API 地址 | `/api`（Vite 代理到 5199） |

---

## NuGet 依赖

| 包 | 版本 | 用途 |
|---|------|------|
| Pomelo.EntityFrameworkCore.MySql | 8.0.2 | MySQL EF Core 提供程序 |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.0 | JWT 认证 |
| Swashbuckle.AspNetCore | 6.5.0 | Swagger API 文档 |
| Serilog.AspNetCore | 8.0.0 | 结构化日志 |
| FluentValidation.AspNetCore | 11.3.0 | 请求校验（已引入，待使用） |
| BCrypt.Net-Next | 4.0.3 | 密码哈希 |
| HtmlSanitizer | 8.1.870 | 富文本 XSS 防护（已引入，待使用） |

---

## 开发约定

1. **后端**：新增模块遵循 `Entity → Repository → Service → Controller` 四层路径，统一返回 `ApiResult<T>`
2. **前端**：新增页面在 `src/views/` 下创建目录，在 `router/index.ts` 注册路由，复用 `AppShell` 布局
3. **权限**：涉及用户数据的实体实现 `IProtectable` 接口，Service 层调用 `PermissionFilter` 做后端兜底
4. **密码**：永远使用 BCrypt 哈希，禁止明文存储
5. **删除**：全部使用软删除（`IsDeleted = true`），不物理删除
6. **时间**：后端统一使用 UTC 时间，前端按需转换本地时间

---

## 生产化待办

- [ ] 数据库迁移：用 `dotnet ef migrations` 替代 `EnsureCreated`
- [ ] `ITokenStore` 换 Redis 实现（支持分布式 + 主动吊销）
- [ ] 定时任务：Quartz.NET（纪念日提醒、书信解锁、凌晨统计）
- [ ] 文件上传与存储（相册图片、头像、封面图）+ HtmlSanitizer 净化富文本
- [ ] 其余 8 个业务模块后端实现（Service + Controller + Repository）
- [ ] 其余 8 个业务模块前端实现（View + 交互逻辑）
- [ ] 时间轴聚合查询（日记 + 纪念日 + 相册 + 打卡 混合排序）
- [ ] 数据导出（日记 PDF、记账 Excel）
- [ ] FluentValidation 请求校验 + CORS 白名单
- [ ] HTTPS / HSTS + 安全加固（RateLimit、Request Size Limit）
- [ ] Docker 化部署 + CI/CD

---

## License

MIT
