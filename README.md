# 💞 我们的小世界（couple-love-system）

> 情侣专属的数字小世界：两个人的日记、相册、纪念日、账单、默契问答……都在这里。

.NET 8 + Vue 3 全栈情侣应用，支持 **PWA 手机安装**、**原生安卓 App（自动更新）**、**公网 HTTPS（Cloudflare Tunnel）** 与**离线可用**。

---

## ✨ 功能总览

| 模块 | 说明 |
|---|---|
| 🏠 首页 | 恋爱天数精确计时、里程碑、每日一句情话、纪念日倒计时、节日彩蛋 |
| 📜 时间轴 | 纪念日/日记/愿望/矛盾聚合回顾，类型筛选 + 年份分组 + 相对时间 |
| 📖 日记 | 富文本 + 心情分（1-10），双方共享，实时同步 |
| ⭐ 愿望 | 共享愿望清单，完成率统计 |
| ✅ 待办 | 情侣共同任务 |
| 📌 留言板 | 悄悄话墙 |
| 💞 默契问答 | 双人作答 → 揭晓 → 默契率统计 |
| 🖼️ 相册 | 批量上传、封面视差、回忆胶片 |
| ☁️ 矛盾 | 矛盾记录 + 和解复盘（情侣吵架也要好好收场） |
| 💌 书信 | 定时解锁的情书（最长 1 年） |
| 💰 记账 | 共享账本、月度预算、消费分类环形图、近 6 月收支趋势、超额提醒、CSV 导出 |
| ☕ 约会 | 约会计划/成行/评分 |
| 👣 足迹 | 小确幸计数（抱抱/亲亲/电影） |
| 🗓️ 纪念日 | 一次/每年重复、提前 N 天提醒（系统通知 + 消息中心） |
| 📊 我们的一年 | 年度恋爱报告（10+ 项数据 + 图表）→ 一键生成浪漫分享海报 |
| 📱 PWA / 原生 App | 可安装到桌面/主屏；安卓原生壳（爱心图标、正式签名、**App 内自动更新**）；iOS 壳（GitHub Actions 云构建未签名 ipa + 全能签重签） |

**跨端实时同步**：任一端的写入（日记/愿望/记账/留言…）通过 SignalR 增量推送，另一端即时刷新 + 伴侣活动提示 + 系统通知。

## 🧱 技术栈

- **后端**：ASP.NET Core 8 · EF Core（MySQL 8）· SignalR · Redis（令牌存储/缓存）· JWT（RSA 非对称）
- **前端**：Vue 3 + TypeScript + Vite · Pinia · Naive UI · ECharts（按需）· PWA（手写 Service Worker 离线缓存）
- **移动端**：Capacitor 7 原生壳（远程模式加载 Web；Android APK 自托管分发 + 应用内更新；iOS 未签名 ipa 由 GitHub Actions 自动构建、全能签安装）
- **部署**：Docker Compose（mysql / redis / backend / frontend / cloudflared）· Cloudflare Tunnel 公网 HTTPS（免公网 IP、免端口转发）

## 🚀 快速开始

```bash
# 1. 部署配置目录（compose / secrets / .env）
cd D:\Docker\couple-love-system

# 2. 一键起全部容器（mysql/redis/backend/frontend/cloudflared）
docker compose up -d --build

# 3. 访问
#    本机：    http://localhost:8080
#    公网：    https://7182629.xyz   （Cloudflare Tunnel）
```

详细文档见 [`docs/`](docs/)：

- 📦 [部署手册](docs/DEPLOYMENT.md) —— 容器化部署、隧道、APK/ipa 分发
- 🔑 [访问指南](docs/ACCESS.md) —— 三种访问方式、登录账号、App 安装
- 📢 [发版手册](docs/RELEASE.md) —— 版本号管理、iOS 云构建、发布 checklist

## 🧪 质量门禁

```bash
# 后端测试（InMemory，无需外部依赖）
dotnet test backend/CoupleLoveSystem.Tests -c Release        # 119 用例

# 前端
cd frontend && node ./node_modules/vue-tsc/bin/vue-tsc.js --noEmit   # 0 错误
node ./node_modules/vitest/vitest.mjs run                            # 26 用例

# 一键健康检查（容器 / 本机 / 外网 / 登录探测）
bash scripts/healthcheck.sh
```

## 📁 目录结构

```
backend/    ASP.NET Core 8（API + 服务 + 基础设施）
frontend/   Vue 3 + TS（views / components / store / api）
mobile/     Capacitor 7 安卓壳（原生插件 UpdatePlugin 等）
docs/       部署 / 开发 / 运维手册
.github/    GitHub Actions CI（后端 + 前端）
```

## 📄 许可证

私有项目。仅限作者与伴侣使用。
