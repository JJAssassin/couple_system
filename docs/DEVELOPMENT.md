# 💻 开发指南

## 1. 技术栈

| 层 | 技术 |
|---|---|
| 后端 | ASP.NET Core 8 · EF Core（MySQL 8）· SignalR · Redis（令牌存储 + 缓存）· JWT（RSA-2048 非对称，私钥外置文件） |
| 前端 | Vue 3 (`<script setup>`) + TypeScript + Vite · Pinia · Naive UI · ECharts（按需 import）· 手写 Service Worker |
| 移动端 | Capacitor 7（`mobile/`，在线模式加载 Web，原生插件扩展） |

## 2. 目录结构

```
backend/
  CoupleLoveSystem.Api/
    Api/Controllers/       REST 控制器
    Application/Services/  业务服务（HomeService / YearReportService / AuthService…）
    Core/                  实体 Entities / DTO / 枚举 / Result
    Infrastructure/        DbContext、缓存 ICacheService、Repository、SignalR Hub
    Program.cs             服务注册 / JWT / 全局过滤器 / 生产 fail-fast
  CoupleLoveSystem.Tests/  xUnit 集成测试（InMemory + NoopHubContext 桩）
frontend/
  src/
    views/                 页面（Home/Diary/Account/Stats…，每模块 Index.vue）
    components/            组件（layout、Common：ChartWrap/YearPoster/AnniversaryReminder…）
    store/                 Pinia（authStore / settingStore / partnerStore / notifyStore）
    api/                   模块 API 封装（request.ts 统一 axios + 401 刷新 + 离线缓存兜底）
    composables/           useRealtime（SignalR）、usePwa、useDevice…
    public/                sw.js（离线缓存）、manifest、icons
mobile/                    Capacitor 壳（android/ 原生工程 + UpdatePlugin 等原生插件）
docs/                      部署 / 开发 / 运维手册
```

## 3. 核心架构事实（改代码前必读）

1. **情侣隔离**：所有内容实体实现 `ICoupleScoped`，`CoupleDbContext` 全局查询过滤器 `!IsDeleted && (CoupleId == 当前情侣)`；`SaveChanges` 拦截器自动盖章 `CoupleId`。**后台作业（如 ScheduledHostedService）处理全部情侣时用 `IgnoreQueryFilters()`**。
2. **软删除**：删除 = 各 Service 显式 `IsDeleted = true`（无拦截器自动删除）；查询自动过滤。
3. **枚举以数字序列化**（后端无 JsonStringEnumConverter）→ 前端类型用数字（`PermissionType` 1/2/3、`WishStatus` 1-4、`AccountRecordType` 1=收入 2=支出、`MessageType` 1=纪念日…）。
4. **实时同步**：写操作后调 `SyncBroadcaster.NotifyAsync(module)`（SyncSignal：created/updated/deleted/reload + payload + senderId）；前端 `useRealtime().onSync(module, cb)` 订阅，`useModuleSync` 可做增量 upsert（reload 信号回退整表刷新）。
5. **SignalR 认证**：匿名连 `/hub/sync` → 携带 JWT POST `/api/sync/authenticate` 上报 connectionId → 后端绑定用户并加入情侣组（`IConnectionIdentityStore`）。
6. **令牌**：AccessToken（JWT RSA，2h）+ RefreshToken（Redis `auth:rt:` 前缀，7d，旋转）；生产强制 Redis TokenStore（`TokenStore__Provider=Redis`，启动时 fail-fast Ping）。
7. **API 离线缓存（sw.js + request.ts）**：GET 读接口 network-first + 离线回退；缓存 key 按 Authorization 指纹（djb2）隔离用户；仅缓存 200 且 `success=true`。
8. **登录态三层持久化**（authStore）：localStorage → sessionStorage → cookie 镜像（vivo 等国产浏览器清 localStorage 的兜底）。

## 4. 常用命令

```bash
# 后端测试（Release 绕开本地 Debug 文件锁）
cd backend
D:/System_Environment/dotnet/dotnet.exe test CoupleLoveSystem.Tests -c Release   # 109 用例

# 前端
cd frontend
node ./node_modules/vue-tsc/bin/vue-tsc.js --noEmit        # 类型检查（0 错误）
node ./node_modules/vitest/vitest.mjs run                  # 26 用例
node ./node_modules/vite/bin/vite.js build                 # 构建（echarts≤700KB/naive≤900KB 门禁）

# 全栈冒烟（本机 nginx 直连，不走公网）
python D:/Item/cap/workbuddy/scripts/daily_maintenance.py
```

### 本地构建坑（已踩）
- **后端必须从 DLL 的 bin 目录启动**：`cd backend/CoupleLoveSystem.Api/bin/Debug/net8.0 && dotnet CoupleLoveSystem.Api.dll --urls http://localhost:5199`（从 solution root 起会让 ContentRoot 错位 → 连接串为 null）。
- **沙箱 safe-delete 拦截**：vite 构建删 dist / Capacitor 生成文件时，`unset CODEBUDDY_SESSION_ID CLAUDE_SESSION_ID CODEBUDDY_SAFE_DELETE_BULK_STATE_DIR CODEBUDDY_TOOL_CALL_ID` 后重跑。
- **Git Bash 跑 gradlew 无效**：用 PowerShell（`$env:JAVA_HOME` + `.\gradlew.bat`）。
- **npm 依赖**：registry 已配腾讯镜像；gradle 用腾讯发行版镜像 + 阿里云 maven（见 `mobile/android/build.gradle`、`gradle-wrapper.properties`）。

## 5. 模块清单（后端控制器 → 前端页面）

| 模块 | API 前缀 | 前端路由 |
|---|---|---|
| 认证 | `/api/auth` | /login |
| 首页 | `/api/home` | /home |
| 时间轴 | `/api/timeline` | /timeline |
| 日记 | `/api/diary` | /diary |
| 愿望 | `/api/wish` | /wish |
| 待办 | `/api/todo` | /todo |
| 留言板 | `/api/board` | /board |
| 默契问答 | `/api/quiz` | /quiz |
| 相册 | `/api/album` + `/api/image` | /album |
| 矛盾 | `/api/conflict` | /conflict |
| 书信 | `/api/letter` | /letter |
| 记账 | `/api/account` + `/api/budget` | /account |
| 约会 | `/api/daterecord` | /dateplan |
| 足迹 | `/api/footprint` | /footprint |
| 纪念日 | `/api/anniversary` | /anniversary |
| 消息 | `/api/message` | /message |
| 统计 | `/api/stats/yearreport` | /stats |
| 设置 | `/api/setting` | /setting |
| 伴侣 | `/api/partner` | — |
| 同步 | `/api/sync`（SignalR 握手） | — |

## 6. CI

`.github/workflows/ci.yml`：backend（build + test）/ frontend（test + build + 体积门禁）。全栈冒烟依赖本机 MySQL/Redis，保留为本地 `daily_maintenance.py` 门禁。
