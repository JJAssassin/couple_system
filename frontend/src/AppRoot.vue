<template>
  <!-- 路由过渡：keyed 容器在每次导航重建并触发入场淡入。
       仅用 opacity（绝不用 transform/filter），以免创建 containing block
       破坏页面内 position:fixed 元素（如 Album 移动端底部固定上传栏）。
       内部页面可是多根组件（loading 骨架 v-if/v-else），不受影响。 -->
  <router-view v-slot="{ Component, route }">
    <div class="route-fade" :key="route.path">
      <component :is="Component" />
    </div>
  </router-view>
  <GlobalLoadingBar />
  <Onboarding />
  <HeartBurstLayer />
  <PartnerActivityToast />
  <FestivalEgg />
  <PwaInstallPrompt />
  <AppUpdatePrompt />
  <AnniversaryReminder />

  <!-- 原生首次启动引导：APK 内 WebView 无法预知后端地址，首次打开引导填写服务器地址 -->
  <teleport to="body">
    <transition name="svr-fade">
      <div v-if="showServerSetup" class="svr-mask">
        <div class="svr-card">
          <div class="svr-ico">💞</div>
          <div class="svr-title">设置服务器地址</div>
          <p class="svr-tip">
            在手机上使用需要填写情侣后端地址：<br />
            域名（如 https://love.example.com）或电脑局域网地址（如 http://192.168.1.50:5199）。
          </p>
          <input
            v-model="serverSetupInput"
            class="svr-input"
            placeholder="https:// 或 http://192.168.1.50:5199"
            @keyup.enter="confirmServerSetup"
          />
          <NButton type="primary" block :loading="serverSetupSaving" @click="confirmServerSetup">保存并继续</NButton>
        </div>
      </div>
    </transition>
  </teleport>

  <!-- 屏幕阅读器状态播报区（WCAG 4.1.3 Status Messages）：与视觉 message 同步，但不抢占焦点 -->
  <div class="sr-only" aria-live="polite" aria-atomic="true">{{ notify.polite }}</div>
  <div class="sr-only" role="alert" aria-live="assertive" aria-atomic="true">{{ notify.assertive }}</div>
</template>
<script setup lang="ts">
import { useMessage, useNotification } from 'naive-ui';
import { ref } from 'vue';
import { getServerBase, setServerBase, isNative } from '@/config/server';
import { NButton } from 'naive-ui';
import { useSettingStore } from '@/store/settingStore';
import { useAuthStore } from '@/store/authStore';
import { bindNotify, useNotifyStore } from '@/store/notifyStore';
import { usePwa } from '@/composables/usePwa';
import { useSwipeBack } from '@/composables/useSwipeBack';
import Onboarding from '@/components/Onboarding.vue';
import HeartBurstLayer from '@/components/Common/HeartBurstLayer.vue';
import PartnerActivityToast from '@/components/Common/PartnerActivityToast.vue';
import FestivalEgg from '@/components/Common/FestivalEgg.vue';
import PwaInstallPrompt from '@/components/Common/PwaInstallPrompt.vue';
import AppUpdatePrompt from '@/components/Common/AppUpdatePrompt.vue';
import AnniversaryReminder from '@/components/Common/AnniversaryReminder.vue';
import GlobalLoadingBar from '@/components/Common/GlobalLoadingBar.vue';

// 必须在 <n-message-provider> / <n-notification-provider> 之下调用，
// 因此本组件作为 provider 的子节点（见 App.vue）渲染，setup 中才能拿到实例。
useSettingStore().hydrate();
bindNotify(useMessage(), useNotification());
// 状态播报：与视觉 message 镜像到隐藏 aria-live 区域（见模板底部）
const notify = useNotifyStore();

// PWA：VitePWA 自动注册 SW；本处只初始化安装引导 + 后台系统通知
const pwa = usePwa();
pwa.init();
pwa.setupNotifications();

// 移动端：左边缘右滑返回上一页
useSwipeBack();

// 原生壳首次启动：若尚未配置服务器地址，弹出引导（避免在不知道后端在哪的情况下白屏）。
const showServerSetup = ref(false);
const serverSetupInput = ref('');
const serverSetupSaving = ref(false);
if (isNative() && !getServerBase()) {
  showServerSetup.value = true;
}
async function confirmServerSetup() {
  const v = serverSetupInput.value.trim();
  if (!v) {
    notify.assertive = '请填写服务器地址';
    return;
  }
  serverSetupSaving.value = true;
  try {
    setServerBase(v);
    showServerSetup.value = false;
    notify.polite = '已连接到服务器';
  } finally {
    serverSetupSaving.value = false;
  }
}

// 原生 WebView / 普通刷新兜底：重载后内存 accessToken 丢失，但 HttpOnly Cookie cl_rt 仍在，
// 启动即静默调 /auth/refresh 续期，避免路由守卫把已登录用户误踢回登录框（评审 #2）。
const auth = useAuthStore();
if (!auth.accessToken) {
  auth.restoreSession().catch(() => {});
}
</script>

<style scoped>
.svr-mask {
  position: fixed; inset: 0; z-index: 1600;
  background: rgba(60, 30, 35, 0.55);
  display: flex; align-items: center; justify-content: center;
  padding: calc(24px + env(safe-area-inset-top)) 24px calc(24px + env(safe-area-inset-bottom));
}
.svr-card {
  width: min(92vw, 380px);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 20px; padding: 26px 22px; text-align: center;
  box-shadow: 0 24px 60px -16px rgba(0, 0, 0, 0.4);
}
.svr-ico { font-size: 40px; }
.svr-title { margin-top: 10px; font-size: 18px; font-weight: 800; color: var(--color-ink); }
.svr-tip { margin: 12px 0 16px; font-size: 13px; color: var(--color-ink-2); line-height: 1.7; }
.svr-input {
  width: 100%; padding: 11px 12px; margin-bottom: 14px;
  border-radius: var(--radius-md, 10px); border: 1px solid var(--color-border);
  background: var(--color-surface); color: var(--color-ink); font-size: 14px;
  box-sizing: border-box;
}
.svr-input:focus { outline: none; border-color: var(--color-rose); box-shadow: 0 0 0 2px var(--color-rose-soft); }
.svr-fade-enter-active, .svr-fade-leave-active { transition: opacity 0.25s var(--ease-love); }
.svr-fade-enter-from, .svr-fade-leave-to { opacity: 0; }
:global(.reduce-motion) .svr-fade-enter-active,
:global(.reduce-motion) .svr-fade-leave-active { transition: none; }
</style>
