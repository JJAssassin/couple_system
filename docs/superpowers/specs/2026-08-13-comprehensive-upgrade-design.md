# 情侣系统全面升级 — 设计文档（Design Spec）

> 分类：**Architectural（架构级）**
> 创建：2026-08-13　流程：brainstorming → writing-plans → executing-plans
> 范围确认：用户已批准「全栈聚焦升级」——后端健壮性/安全 + 前端工业风落地 + TDD/代码评审/验证门禁

## 1. 现状（已读代码确认的真实状态）

### 后端 ASP.NET Core 8（`backend/CoupleLoveSystem.Api`）
- **TokenStore 为内存版**：`InMemoryTokenStore` 存于静态 `ConcurrentDictionary`；`AuthService.FindUserIdByRefreshAsync` 每次刷新/登出都 `foreach` 全表 `Users` 再逐个 `GetAsync` → **O(N) 全表扫描**。已在注释标明「上线替换为 RedisTokenStore」。
- **富文本 XSS 净化不完备**：`DiaryService.SanitizeContent` 仅用正则兜底（去 `<script>`、`on*` 属性、`javascript:`），源码自带 `TODO: 引入 Ganss.Xss HtmlSanitizer` 做白名单净化。
- **RefreshToken 不轮换**：`AuthService.RefreshAsync` 原样回发旧 refresh token，旧令牌长期有效，被盗可一直用。
- **Redis80 已就绪**：`D:\System_Environment\Redis`，端口 6379，无密码，Windows 服务 `Redis80` 已运行。`StackExchange.Redis` 尚未接入。
- 已有 `CoupleLoveSystem.Tests` 测试工程（xUnit），可走 TDD。

### 前端 Vue3 + TS + Vite6 + Tailwind v4 + NaiveUI + ECharts
- 工业拟物**设计令牌已落地**（`src/assets/style/global.css`：冷灰底盘 `#e0e5ec`、暖珊瑚强调 `#ff6f7d`、拟物阴影工具类 `.ind-*`、字体 Inter + JetBrains Mono）。
- **布局外壳已重做**：`Sidebar.vue`（螺丝/LED/lucide 图标）、`TabBar.vue`（凹陷图标井）。
- **14 个页面尚未套 `.ind-*` 类**，首页 `Home/Index.vue` 仍是原柔和风，不是工业样板。

## 2. 目标
在不改动既有数据模型与 API 契约的前提下，补齐后端生产级短板（分布式令牌、XSS 净化、令牌轮换），并把已设计的工业视觉系统真正铺满前端，全程以 TDD + 代码评审 + 验证门禁保证质量。

## 3. 设计（三阶段）

### 阶段 A — 后端健壮性 & 安全（TDD 驱动）
**A1. RedisTokenStore**
- 新增 `Infrastructure/Redis/RedisTokenStore.cs` 实现 `ITokenStore`：`SetAsync/GetAsync/RemoveAsync` 走 `StackExchange.Redis`（`IDatabase`）。
- Key 规范：`auth:rt:{userId}`，值为 refresh token 字符串，TTL 用 `TimeSpan`。
- 修复 O(N)：`FindUserIdByRefreshAsync` 改为「refresh token 反查」——登录/刷新时把 `userId` 一并写进 token 载荷或维护 `auth:rt:byvalue:{tokenHash} -> userId` 反向索引，使刷新/登出为 O(1)。本方案采用「token 中编码 userId」：refresh token 形如 `Guid|{userId}` 或存反向索引，二者取反向索引（更干净，不泄露 userId）。
- 接线：`Program.cs` 根据配置 `TokenStore:Provider`（`InMemory`/`Redis`）注入；Redis 模式读 `ConnectionMultiplexer`。
- 测试：用 `CoupleLoveSystem.Tests` 对 `ITokenStore` 契约写 fake 测试；Redis 集成测试用 `RedisServer`/TestContainer 或本地 Redis（CI 可选跳过）。

**A2. HTML 净化（Ganss.Xss）**
- 引入 `Ganss.Xss` NuGet；新增 `Application/Services/HtmlSanitizerService.cs` 封装 `HtmlSanitizer` 白名单（允许 `p,b,i,u,em,strong,br,img[src,alt],a[href,target],ul,ol,li,blockquote,code,pre,h1-h3,span`；`img` 仅 `https`/相对，`a` 强制 `rel=noopener`）。
- `DiaryService.SanitizeContent` 改用 `HtmlSanitizerService`，删除正则兜底。
- 测试：XSS payload（`<script>`、`onerror=`、`javascript:`、`<img src=x onerror>`、CSS 表达式）断言被净化。

**A3. RefreshToken 轮换**
- 刷新时：校验旧 RT 有效 → 删除旧 RT → 生成新 RT（`Guid`）→ 写入存储 → 返回新 RT（同 A1 反向索引）。
- 登出：删除 RT。
- 测试：用旧 RT 二次刷新应失败；新 RT 可用。

### 阶段 B — 前端工业风落地（awesome-design-md 指导）
**B1. 系统性套用 `.ind-*` 类**：对 14 个页面视图统一应用 `ind-card / ind-recessed（输入）/ ind-btn / ind-btn-accent / ind-led / ind-label`，保证视觉一致；替换残留 emoji 为 lucide 图标。
**B2. 首页工业样板**：`Home/Index.vue` 改造成旗舰工业仪表盘——拟物统计卡（LED 状态、螺丝）、凹陷屏内嵌 ECharts、通风槽装饰、品牌 LED「SYSTEM ONLINE」。
**B3. 响应式 & 可访问性**：移动端 48px 触控目标、对比度、暗色模式变量生效。

### 阶段 C — 质量门禁（superpowers 流程）
- `writing-plans` 拆任务 → 后端 TDD（红绿重构）→ `requesting-code-review` / `receiving-code-review` 评审 diff → `verification-before-completion` 构建+测试+冒烟后才宣布完成。
- 回归：`dotnet build` / `dotnet test` 绿；`npm run build` 绿；登录冒烟 `partner_a/123456` 通过。

## 4. 文件清单（将被创建/修改）
- 后端新增：`Infrastructure/Redis/RedisTokenStore.cs`、`Application/Services/HtmlSanitizerService.cs`、`Core/Options/TokenStoreOptions.cs`
- 后端修改：`Program.cs`（注入切换）、`AuthService.cs`（轮换+反查）、`DiaryService.cs`（净化替换）、`ITokenStore.cs`（接口保持）、`CoupleLoveSystem.Api.csproj`（加包）、`appsettings.json`（TokenStore 配置）
- 后端测试新增：`Tests/RedisTokenStoreTests.cs`、`Tests/HtmlSanitizerTests.cs`、`Tests/AuthRefreshRotationTests.cs`
- 前端修改：14 个 `views/*/Index.vue`、`Home/Index.vue`、`global.css`（如需补类）

## 5. 风险
- Redis 连接失败应有降级（InMemory 兜底）+ 启动健康检查。
- HTML 净化白名单过严会破坏正常富文本——需样例回归（日记编辑预览）。
- 令牌轮换会改变前端 refresh 逻辑（前端已在 `request.ts` 处理 401 刷新，需确认拿到新 RT 落库）。

## 6. 成功标准
- 后端：`dotnet test` 全绿（含新增安全测试）；Redis 模式启动后 refresh/logout 不再全表扫描。
- 前端：`npm run build` 绿；14 页视觉统一为工业风，首页为样板。
- 全流程经代码评审 + 验证门禁。
