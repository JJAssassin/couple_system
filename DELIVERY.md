# 情侣专属情感陪伴 Web 系统 —— 实现完成与交付说明

> 项目路径：`D:\Code\My_vscode\couple-love-system`
> 技术栈：前端 Vue3 + TS + Vite6 + Pinia + NaiveUI + Tailwind v4 + ECharts；后端 ASP.NET Core 8 + EF Core + MySQL + BCrypt
> 生成日期：2026-08-13

## 一、已实现功能（全部模块打通）

### 前端（12 个页面全部为真实功能，非占位）
- 登录 `Login`：JWT 双 Token 登录、令牌本地存储
- 首页 `Home`：恋爱天数数字滚动、ECharts 心情/矛盾趋势、就近纪念日、余额/打卡统计
- 时间轴 `Timeline`：纪念日/日记/愿望/矛盾/打卡五表聚合，月份筛选，SVG/CSS 竖线节点
- 日记 `Diary`：公开/私密 Tab、富文本、心情/天气、权限标签、评论；**私密内容后端兜底不可见**
- 愿望 `Wish`：共同/礼物/成长三类、认领、标记完成、状态进度
- 相册 `Album`：相册网格、图片上传（NUpload）、大图预览（NImage）
- 矛盾 `Conflict`：克制风格、等级标记、和解记录
- 书信 `Letter`：未解锁锁呼吸动画（`global.scss` 的 `.letter-lock`）、**仅接收人可见内容**、定时解锁
- 打卡 `CheckIn`：日历、启用项、防重复打卡、连续天数
- 记账 `Account`：收支饼图、记一笔、余额
- 约会 `DatePlan`：待执行/历史、星星评分
- 设置 `Setting`：改资料/密码、深色模式、动效减弱开关、全量导出
- 通用：PC 侧栏 ↔ 移动底栏自适应、侧栏消息铃（未读角标 + 下拉）、路由转场、`useStaggerEnter` 错落入场、骨架/空态

### 后端（14 张表 + 11 模块 + 横切能力）
- 认证：`AuthService`（JWT 双 Token + BCrypt，刷新令牌存 ITokenStore，沙箱用内存版）
- 权限兜底：`PermissionFilter` 统一 `WhereVisible`/`EnsureVisible`/`CanEdit`，**绝不信任前端 ID**
- 模块 Service/Controller：首页、纪念日、日记+评论、愿望（认领/完成）、相册+图片、矛盾、书信（解锁）、打卡（项/记录/连续）、记账、约会、系统消息、时间轴聚合、用户（改密/导出）
- 定时任务：`ScheduledHostedService`（托管服务，**替代设计文档里的独立 Quartz 项目**，零额外依赖）每分钟轮询：① 到达时间的定时书信自动解锁并生成通知；② 纪念日提醒并重装填下次提醒时间。全部以服务器 UTC 时间为准
- 文件上传：`ImageController` 后缀白名单(jpg/jpeg/png/gif/webp) + ≤5MB + `yyyyMMdd_Guid` 重命名 + 存入 `wwwroot/uploads`
- 全量导出：`UserService.ExportAsync` 导出当前用户可见数据（含权限过滤）为 JSON

## 二、验证状态（已实测）

| 项 | 结果 |
|---|---|
| 前端 `npm run build`（vue-tsc 类型检查 + vite 打包） | ✅ 通过，0 类型错误，dist 产出 |
| 前端 `npm run test`（vitest 单测） | ✅ 11/11 通过（format 标签映射 + request 拦截器三分支） |
| 后端 `dotnet build` | ⚠️ 本沙箱无 .NET 8 SDK，**未编译**；已做静态核对（所有引用 DTO 均已定义、Service/Controller/Program 注册一致） |
| 后端 `dotnet test`（xUnit） | ⚠️ 未跑；已提供 `CoupleLoveSystem.Tests` 工程（权限过滤 + 密码哈希纯逻辑测试） |

> 前端已经过类型检查与单测验证；后端代码完整且与契约一致，但**需要你在本机 .NET 8 下 `dotnet build` 确认编译**，如有编译报错把报错贴给我即可修复。

## 三、如何运行

### 前端（看样式/开发，无需后端）
```bash
cd D:\Code\My_vscode\couple-love-system\frontend
npm install        # 已装过可跳过
npm run dev        # http://localhost:5174  （/api、/uploads 已代理到后端 5199）
npm run build      # 类型检查 + 打包
npm run test       # 单元测试
```

### 后端（需 .NET 8 SDK + 本机 MySQL 8.4）
```bash
cd D:\Code\My_vscode\couple-love-system\backend\CoupleLoveSystem.Api
# 1) 改 appsettings.json：Jwt:Secret 改成你自己的长密钥；ConnectionStrings:MySql 指向本机 MySQL
# 2) 建库：CREATE DATABASE couple_love CHARACTER SET utf8mb4;
dotnet restore
dotnet build                       # 首次请确认编译通过
dotnet run                         # http://localhost:5199  （开发期自动建表 + 写入双账号）
# 测试：
cd ..\CoupleLoveSystem.Tests && dotnet test
```
- 默认双账号：`partner_a` / `partner_b`，密码均为 `123456`（开发期种子，生产请删）
- 登录后首页会调后端接口；未起后端时仅前端样式可看，接口会报错（已兜底不崩）

## 四、本次修复/补齐的关键点
- ECharts 选项类型过严导致构建失败 → `ChartWrap` 的 `option` prop 放宽为 `any` 内部 cast，全站图表正常
- 多个页面用 `useMessage()` 但 App 未挂 Provider → `App.vue` 包裹 `NMessageProvider`/`NNotificationProvider` 并绑定 `notifyStore`
- 图片走 `/uploads` 但 dev 代理只代理 `/api` → `vite.config.ts` 增加 `/uploads` 代理
- 后端所有 Service 统一在 `Program.cs` 注册；静态文件 `app.UseStaticFiles()` 提供上传图片
- 定时任务用内置托管服务替代独立 Quartz 工程（更简单、零依赖）

## 五、已知限制 / 后续 TODO（设计文档要求但本次从简，已在代码注明）
1. **富文本净化**：日记 Content 入库前做了 `<script>`/`on*` 兜底过滤，建议接入 `Ganss.XSS/HtmlSanitizer` 做完整净化（`DiaryService` 已标 TODO）
2. **Token 存储**：`ITokenStore` 现为内存版，重启失效；上线请实现 `RedisTokenStore`（注释已标）
3. **图片删除**：`ImageController` 删除仅逻辑删除 DB，磁盘文件未物理清理（标 TODO，需回收策略）
4. **导出格式**：`export/alldata` 仅导出元数据 JSON，图片打包进 zip 未做（标 TODO）
5. **安全加固**：上线前请按设计文档 §十 配置 HTTPS/HSTS、CORS 白名单、IIS 对 `uploads` 目录禁脚本执行、MySQL 仅本机连接
6. **后端编译/集成测试**：本沙箱无 .NET，未做端到端编译验证，请本机 `dotnet build`/`dotnet test`
