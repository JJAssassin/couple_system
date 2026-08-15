import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import router from './router';
import './assets/style/global.css';
import HeroIcon from './components/Common/HeroIcon.vue';

const app = createApp(App);
app.use(createPinia());
app.use(router);
// NaiveUI 改为按需引入（unplugin-vue-components + NaiveUiResolver，见 vite.config.ts）：
// 编译期只注入模板实际用到的组件，避免全量 app.use(NaiveUi) 把整个库打进 bundle。
// 消息/通知由 App.vue 的 <n-message-provider>/<n-notification-provider> 提供并绑定 notifyStore。
app.component('HeroIcon', HeroIcon); // 本地 Heroicons 统一入口（src/assets/icons/heroicons/*.svg）

app.mount('#app');
