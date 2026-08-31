import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import router from './router';
import './assets/style/global.css';
import '@fontsource/noto-serif-sc/500.css'; // 情感衬线（按 unicode-range 切片，浏览器按需加载）
import '@fontsource/noto-serif-sc/700.css';
import './interactions/finesse.css'; // 12 项微交互动效层（keyframes + 指令注入元素样式）
import './assets/style/uiverse-motion.css'; // UIverse Motion Kit：取自 uiverse.io 的动效，重调为浪漫柔光设计语言
import HeroIcon from './components/Common/HeroIcon.vue';
import { registerFinesseDirectives } from './interactions';

const app = createApp(App);
app.use(createPinia());
app.use(router);
registerFinesseDirectives(app); // 注册 v-ripple / v-press-bounce / v-click-burst 全局指令
// NaiveUI 改为按需引入（unplugin-vue-components + NaiveUiResolver，见 vite.config.ts）：
// 编译期只注入模板实际用到的组件，避免全量 app.use(NaiveUi) 把整个库打进 bundle。
// 消息/通知由 App.vue 的 <n-message-provider>/<n-notification-provider> 提供并绑定 notifyStore。
app.component('HeroIcon', HeroIcon); // 本地 Heroicons 统一入口（src/assets/icons/heroicons/*.svg）

app.mount('#app');

// PWA：仅在「生产构建」注册手写 Service Worker（public/sw.js，离线 app-shell + 运行时缓存）。
// 开发环境跳过，避免 Service Worker 缓存 HMR 模块导致改了代码不生效。
if (import.meta.env.PROD && 'serviceWorker' in navigator) {
  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/sw.js').catch(() => {});
  });
}
