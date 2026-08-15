# 前端（couple-love-system/frontend）

技术栈：Vue3 + TypeScript 5.7 + Vite 6 + Pinia + Vue-Router 4 + NaiveUI + TailwindCSS 4 + ECharts 5.5 + GSAP。

## 已实现
- 脚手架：Vite + TS + Tailwind v4 主题（柔玫瑰/米白/豆沙棕调色板）、viewport-fit=cover 安全区。
- `request.ts`：统一拦截器 + **无感刷新 RefreshToken**（401 重试一次 + 并发刷新锁）+ 403 提示。
- 路由 + 守卫（登录拦截）、Pinia 三 store（auth/notify/setting）。
- 核心组件：`PageTransition`（移动端 x 轴转场）、`LoveCount`（rAF 数字滚动）、`ChartWrap`（ECharts 从 0 生长）、`AppShell`（PC 侧栏 ↔ 移动底栏）。
- 页面：`Login`（登录）、`Home`（恋爱天数滚动 + ECharts 看板 + 就近纪念日）、其余 10 个为**占位页**（标注待实现，沿用同构扩展）。
- 移动端：响应式 + TabBar + 安全区 + 动效减弱开关（详见设计文档 §12）。

## 运行
```bash
npm install
npm run dev      # http://localhost:5174 ，/api 已代理到后端 5199
# 生产
npm run build
```
> 需后端先起（默认 5199）。初始账号 partner_a / partner_b，密码 123456。

## 目录
```
src/
├─ api/ (可放按模块封装的调用，当前在 view 内直接调 request)
├─ assets/style/global.scss   # 主题变量 + 全局样式
├─ components/{Common,ChartWrap,layout}
├─ composables/  useDevice / useAnimation
├─ router/
├─ store/        authStore / notifyStore / settingStore
├─ types/       与后端 DTO 对齐
├─ utils/request.ts
└─ views/       Login + Home(实) + 其余占位
```
