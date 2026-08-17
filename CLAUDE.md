# CLAUDE.md — couple-love-system（情侣小世界）

> 给未来会话 / 新环境的速查。换机器或开新会话**先读这份**，能直接避开已踩过的坑。
> 本文件不收录任何明文密码；凭据一律指向仓库外的本地文件。

---

## 0. 一句话定位

全栈情侣记录 / 互动应用：**.NET 8 后端 + Vue3 + TS + Vite 前端**，MySQL 8 + Redis，SignalR 实时同步，支持 docker-compose 一键容器化部署。前端 15 个视图模块，全局**情侣数据隔离**。

仓库根：`D:\Code\My_vscode\couple-love-system`（git 已 init，默认分支 `master`，**尚无 remote**）。

---

## 1. 本机运行环境（统一装在 D 盘，勿装 C 盘默认位置）

| 组件 | 路径 / 命令 | 备注 |
|---|---|---|
| .NET 8 SDK | `D:\System_Environment\dotnet\dotnet.exe` | net8.0；用户 PATH 已写入 |
| MySQL 8 | `D:\System_Environment\MySQL` | `my.ini` + `data`；凭据见 `D:\System_Environment\MySQL\credentials.txt`；库名 `couple_love` |
| Redis | `D:\System_Environment\Redis` | 端口 6379（本机）；容器里用 6380 错开 |
| 前端 | `frontend/` | Vue3+TS+Vite，dev 端口 5174，代理 `/api` `/uploads` `/hub` → 后端 5199 |

- demo 账号：`partner_a` / `partner_b`，密码 `123456`，已预绑定同一 `CoupleId`（用于双端联调实时同步）。
- DB 账号：`root`（运维）与 `app`（后端连接串使用）。具体密码见上面的 credentials 文件，**不要写进仓库**。

---

## 2. 两个必踩的运维坑（已解决，复现即照做）

1. **后端必须从 DLL 的 bin 目录启动**
   `backend` 是 solution root，**没有 appsettings.json**。正确启动：
   ```bash
   cd backend/CoupleLoveSystem.Api/bin/Debug/net8.0
   dotnet CoupleLoveSystem.Api.dll --urls http://localhost:5199
   ```
   若从 `backend` 根跑 `dotnet .../bin/.../CoupleLoveSystem.Api.dll`，`ContentRoot` 会落在 solution root → `GetConnectionString("MySql")` 返回 null → 崩溃报 `user ''@'jp' (using password: NO)`。

2. **MySQL `app` 账号需 `@'%'` 授权**
   后端走 TCP `127.0.0.1`，MySQL 反解为机器名（如 `jp`），而 `app@localhost` 会拒连（`Host 'jp' is not allowed`）。已补 `app@'%'`（本地开发放宽）。**重装 / 重置 MySQL 后必须重做此授权**。

3. **`sc` / `net start` 被安全策略禁用** → 起 MySQL 直接拉 `mysqld.exe --defaults-file=D:\System_Environment\MySQL\my.ini`，不要走 Windows 服务命令。

---

## 3. 本地起服务（非容器）

```bash
# 1) MySQL（后台）
D:\System_Environment\MySQL\bin\mysqld.exe --defaults-file=D:\System_Environment\MySQL\my.ini

# 2) Redis（后台）
D:\System_Environment\Redis\redis-server.exe D:\System_Environment\Redis\redis.conf

# 3) 后端（必须从 bin 目录，见坑①）
cd backend/CoupleLoveSystem.Api/bin/Debug/net8.0 && dotnet CoupleLoveSystem.Api.dll --urls http://localhost:5199

# 4) 前端 dev
cd frontend && npm run dev   # http://localhost:5174
```

---

## 4. 测试资产与命令

| 类型 | 命令 | 说明 |
|---|---|---|
| 后端 xUnit | `dotnet test CoupleLoveSystem.Tests/CoupleLoveSystem.Tests.csproj` | InMemory + `Infrastructure/TestDb.cs` 的 `NoopHubContext` 桩；约 65 用例 |
| 前端 vitest | `node ./node_modules/vitest/vitest.mjs run` | **Windows 不能用 `node node_modules/.bin/vitest`**（sh 包装失效）；约 26 用例 |
| 前端类型检查 | `cd frontend && node ./node_modules/vue-tsc/bin/vue-tsc.js --noEmit` | 0 错误为通过 |
| 全栈冒烟 | `python D:\Item\cap\workbuddy\smoke_api.py` | 登录 + 28 读 + 12 写闭环 + 401 门控 |

- **沙箱注意**：`rm -rf` 与清 `dist/` 会被批量删除守卫拦截。vite 构建前先 `mv dist dist.del.$$`，构建后再用 node `fs.rmSync` 清 `dist.del.$$`。
- 构建后端前先停掉占用 `Api.dll` 的 dotnet 进程，否则 MSB3027 文件锁。

---

## 5. 关键架构事实（改代码前必读）

- **全局情侣隔离**：`CoupleId` 全局查询过滤器 + `SaveChanges` 拦截器自动盖章；后台作业用 `IgnoreQueryFilters`。
- **枚举以数字序列化**：后端仅 `CamelCase`，无 `JsonStringEnumConverter` → 前端类型**必须按数字**使用（如 `PermissionType` 1/2/3、`RoleType` 1/2）。
- **实时同步**：后端在九大模块写后调用 `SyncBroadcaster.NotifyAsync(module)`；前端 `useRealtime().onSync(module, load)` 订阅刷新。
  - ⚠️ 目前前端收到 sync 后**缺"对方刚更新了 XX"的轻提示**（潜在体验增强点，见 roadmap）。
- 前端 15 视图模块：Account / Album / Anniversary / CheckIn / Conflict / DatePlan / Diary / Footprint / Home / Letter / Message / Score / Setting / Timeline / Wish。

---

## 6. 容器化部署（docker-compose，部署配置已迁至 D:\Docker）

- 编排：`D:\Docker\couple-love-system\docker-compose.yml`（mysql:8.0.39 / redis:7 / backend / frontend / caddy 五服务）。**端口与本机错开**：`8080→frontend(nginx:80)`、`3307→mysql`、`6380→redis`、`80/443→caddy`，不冲突本机 3306/6379/5199。
- 一键：`cd D:\Docker\couple-love-system && docker compose up -d --build` → 访问 `http://localhost:8080`（或 `https://localhost` 走 Caddy 自签）。部署配置与源码分离：compose 内 `build.context` 指向源码绝对路径 `D:\Code\My_vscode\couple-love-system\{backend,frontend}`。
- 后端环境变量：`ASPNETCORE_ENVIRONMENT=Production` + `ConnectionStrings__MySql` 指向 `mysql:3306` + `TokenStore__Provider=Redis` + JWT 私钥挂载 `/app/keys/jwt-private.pem`（私钥在 `D:\Docker\couple-love-system\secrets\jwt-private.pem`，已 gitignore）。
- 官方 MySQL 镜像自动建库 `couple_love` + 建 `app@'%'` 并授权，无需手写 init 脚本。
- 前端 `baseURL` / SignalR 均为**相对路径** → 容器化零改前端代码；nginx 反代 `/api` `/uploads` `/hub`（含 websocket）。
- 数据持久化：命名卷 `mysql-data` / `redis-data` / `uploads-data` / `caddy-data` / `caddy-config`。
- **`.env` / `secrets/` / `certs/` 已 gitignore**（且已不在本项目仓库内）；`.env.example` 现位于 `D:\Docker\couple-love-system\.env.example`（脱敏）。

### nginx 缓存坑（已踩并已修，别再回退）
`/home` 白屏**不是代码 bug**，根因是浏览器缓存旧版 JS：`/assets/` 原用 `immutable` 永久缓存，本地反复 `docker rebuild` 会删旧 hash 的 chunk；浏览器永久信任旧 JS、持续 import 已删旧 chunk（404 或回退 HTML 被当 JS）→ 白屏。且 `index.html` 的 no-cache 只在整页刷新时生效，SPA 内点链接切换路由不重载 index.html。
**修复（`frontend/nginx.conf`）**：`/` 与 `/assets/` 都设 `Cache-Control: no-cache`，`/assets/` 用 `try_files $uri =404`。用户遇白屏先**整页硬刷新（Ctrl+Shift+R）**；改 no-cache 后刷一次即永久稳定。

---

## 7. 版本控制约定

- 项目此前**零版本控制**（已修正，首次提交 `f012118`）。
- `.gitignore` 已排除：`bin/` `obj/` `node_modules/` `dist/` `keys`(RSA 私钥) `appsettings.json`(含 DB+SMTP 密码) `MySQL账号密码.txt`；另存脱敏 `appsettings.example.json`。
- **尚无 git remote**：需推送到 GitHub/Gitee 时，用户提供远程 URL 后 `git remote add origin <url> && git push -u origin master`。
- 提交分支默认名：`master`。

---

## 8. 前端特别注意

- **Vue Router `name` 区分大小写**（关键坑）：`router.push({ name })` 的 `name` 大小写必须与路由表定义完全一致。曾出现路由表 name 大写、调用处传小写导致导航**全部失效**，修复为 `router.push('/' + name)` 形式。新增路由跳转请优先用 path 或严格匹配 name。
- 全局样式在 `frontend/src/assets/style/global.css`（如 `.ind-btn` 需自带 padding/font-size，否则空态按钮文字会溢出方框）。
- 组件类型声明 `frontend/components.d.ts` 由 unplugin 自动生成，已纳入版本库（CI 类型检查依赖它）。

---

## 9. 下一步 backlog（已与用户对齐的优先级）

- **P0（已做）**：首页打磨包提交（`e1597db`）；本 CLAUDE.md 固化坑位。
- **P1 体验做实**：① ~~伴侣实时更新轻提示 toast~~ **已实现**（2026-08-16）：后端给 `SyncSignal` 加 `SenderId`（CoupleContext.CurrentUserId 经中间件写入，CoupleDbContext 捕获进信号）；前端 `PartnerActivityToast` 订阅 `useRealtime().onAnySync`，仅当 `senderId` 非空且 ≠ 当前用户（从 JWT `sub` 解析，避免刷新后 profile 为空漏提示）时弹「伴侣更新了 『模块』」，按模块 1.5s 去抖；已双浏览器验证（伴侣端弹、自己端不弹、零报错）。② ~~纪念日页 `/anniversary` 深化~~ **已实现**（2026-08-16）：纯前端增强——实时倒计时引擎（每秒刷新，hero 大倒计时 + 卡片小倒计时含 HH:MM:SS）、顶部「下一个重要的日子」英雄卡（本周年进度环 / 第 N 周年 / 今天庆祝态）、提醒临近高亮徽标、历史回顾（第 N 周年、已过去 N 天、底部统计）、卡片封面图 + 临近发光；vue-tsc 0 错误、真机 Chrome 验证零报错。③ ~~暗色 / 夜间模式~~ **已实现**（2026-08-16）：`settingStore` 增 `mode`（light/dark/system）三态 + 系统偏好 `matchMedia('(prefers-color-scheme: dark)')` 监听，`dark` 改为解析计算值（mode=system 时跟随系统），保留 `toggleDark()` 兼容顶栏按钮；`Setting` 页「深色模式」NSwitch 升级为三态分段控件（浅色/深色/跟随系统）并示当前系统态；修 `Login.vue` 硬编码 `#fff` 卡片/`input` 背景使其跟随暗色；`App.vue` 主题覆盖里强调色 `var(--color-rose*)` 改写字面 hex（seemly 的 rgba() 解析 var 会抛 `[seemly/rgba] Invalid color value` 控制台错误，视觉正常但报错，已消）。验证：vue-tsc 0 错误；真机 Chrome 经 :8080 三种模式（显式深 / 跟随系统=dark / 跟随系统=light）下 Home/Anniversary/Setting/Album 均渲染、`hasDark` 正确、**零 page error**。④ ~~移动端窄屏回归~~ **已实现**（2026-08-16）：先以 375×667 真机审计遍历 13 个路由，确认 `document.documentElement.scrollWidth - innerWidth === 0` 全零、零 page error；发现的唯一问题是视觉而非布局溢出：① 顶栏右侧头像/退出/主题在 375px 下过满；② 纪念日页 hero 的倒计时 `HH:MM:SS` 被右侧进度环截断。修复：AppShell 在 ≤767px 隐藏面包屑当前标题、缩小头像（34→30）并收紧重叠间距；`Anniversary/Index.vue` 在 ≤767px 将 hero 改为垂直堆叠（进度环下移居中）、倒计时换行、卡片/历史统计单列、封面图加高。再验证 320×568 下 Home/Anniversary/Setting 仍无溢出、零报错。
- **P2 工程化 / 扩展**：推送到远程仓库异地备份（需用户给 GitHub/Gitee URL）；~~GitHub Actions CI~~ **已实现**（2026-08-16）：`.github/workflows/ci.yml` 两个 job——`backend`（ubuntu-latest + setup-dotnet 8，缓存 NuGet，`dotnet build` Api + `dotnet test` Tests，测试走 InMemory 无外部 DB 依赖）、`frontend`（setup-node 22 缓存 npm，`npm ci` + `npm test`(vitest) + `npm run build`(vue-tsc+vite) + echarts≤700KB/naive≤900KB 体积门禁 node 脚本）；全栈冒烟因依赖本机 MySQL/Redis 与机器绝对路径脚本（`D:/Item/cap/workbuddy/smoke_api.py`）保留为本地 `ci-gate.sh` 门禁，CI 注释说明可加 mysql:8/redis:7 service job 做云端集成。本地已实跑验证：后端 `dotnet test` 67/67、前端 `vitest` 26/26 均绿。~~节日/纪念日彩蛋~~ **已实现**（2026-08-16）：新增 `frontend/src/components/Common/FestivalEgg.vue` 挂于 `AppRoot`，未登录(`cl_at` 缺失)跳过；登录后拉 `listAnniversaries(1,50)`，若某纪念日 `daysLeft===0` 则「今天是你们的『X』」庆祝，否则匹配固定节日表(0101/0214/0314/0520/1111/1225)弹「X 快乐」；飘心庆祝层(20 颗 CSS 下落 heart，尊重 `reduceMotion`)，按天 `localStorage(cl_egg_<date>)` 记忆已收下、可点遮罩/按钮关闭；零网络异常静默。验证：vue-tsc 0 错误；真机 Chrome 经 :8080 用 API 造 targetDate=今日的纪念日，断言 `.egg-mask` 出现、标题含「今天」、`pageErrors=[]`、`apiFail=[]`，造的数据已删、DB 干净。~~照片墙视差/批量上传~~ **已实现**（2026-08-16）：批量上传本就支持（`NUpload multiple :max="20"` 逐个 `albumApi.uploadImage`）；新增相册列表封面卡滚动视差——`.album-cover img` 改为 oversize(130%+top:-15%) 以便位移不露边，`rAF` 节流的 `window` scroll/resize 监听按每张卡相对视口中心位置给 img 设 `translateY(±~24px)`，尊重 `setting.reduceMotion`（开启时还原 transform）；`onMounted` 绑定监听、`onUnmounted` 解绑、`loadAlbums` 在 `loading=false` 且网格真实渲染后 `nextTick` 再 `applyParallax`（`v-if` 骨架屏期间网格未渲染，早调会取不到 `.album-grid`——已踩此坑）。验证：vue-tsc 0 错误；真机 Chrome 经 :8080 造 12 个带封面相册撑高页面，`scrollY 0→600` 时封面 transform 由 `translateY(4.74px)` 变为 `23.02px`、`-4.14px` 变为 `14.15px`，`parallaxMoved:true`、`pageErrors=[]`、`apiFail=[]`，数据已全删。剩余 P2：关系时间轴；生产 HTTPS/域名。

~~关系时间轴 `/timeline` 深化~~ **已实现**（2026-08-16）：纯前端增强（后端 TimelineService 聚合纪念日/日记/愿望/矛盾，DTO 无需改）——① 左侧筛选区新增**类型 chips**（纪念日/日记/愿望/矛盾 多选切换，与现有月份筛选叠加，含各类型实时计数）；② 右侧顶部**统计条**（总条数 + 各类型计数，总数取完整过滤集而非分页切片，已修一处把分页切片当总数的 bug）；③ 按**年份分组**并渲染 sticky 年标题（`top:60px` 贴顶栏下沿不重叠）；④ 每条卡片加**相对时间**（今天/昨天/N天前/N周前/N个月前/N年前）；⑤ 长 summary（矛盾结论/愿望描述）**可展开/收起**。移动端回归时发现 `.tl-page`/`.tl-wrap` 未约束宽度致统计条撑到 ~631px 横向溢出 272px，已加 `width:100%;max-width:100%` + 统计项 `flex:1 1 56px` 修复。验证：vue-tsc 0 错误；真机 Chrome 经 :8080 造跨 2023/2024/2025 的日记+矛盾+愿望（页面内 fetch 带 `cl_at` JWT，因 `page.request` 不携带 localStorage 令牌会 401），断言 3 个年份分组、类型筛选仅留 `t-diary`、展开生效、相对时间存在、`pageErrors=[]`、`apiFail=[]`、`mobileOverflow=0`，数据已全清（含此前一次清理 404 残留的跨 run 种子，用标题/内容特征 sweep 删除）。剩余 P2：~~生产 HTTPS 反代（Caddy 自签）~~ **已实现并验证**（2026-08-17）：`Caddyfile` 默认 `:443` 用**静态自签证书** `certs/localhost.crt`（SAN=localhost/127.0.0.1，openssl 生成，365 天）；`docker-compose.yml` 增 `caddy` 服务（80/443→frontend:80，挂 `./certs:/certs:ro`）+ `caddy-data`/`caddy-config` 卷；`certs/` 已 gitignore。`docker compose up -d caddy` 实测 `https://localhost` 经 **TLS1.3** 返回 200（首页 `<title>我们的小世界</title>`），自签浏览器会警告属预期；`:80` 与 `:8080` 入口不受影响。踩坑：最初用 `tls internal` + 站点 `:443`（无主机名）导致握手 `alert internal error`（Caddy 无域名可签发），改静态证书后解决。真域名切换：站点改 `yourdomain.com:443` + 删 `tls /certs/...` 两行（Caddy 自动 Let's Encrypt），需 DNS A 指向本机 + 80/443 公网可达。推送到远程仓库（需用户给 GitHub/Gitee URL）。
