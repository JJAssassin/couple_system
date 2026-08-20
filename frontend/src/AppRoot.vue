<template>
  <router-view />
  <Onboarding />
  <HeartBurstLayer />
  <PartnerActivityToast />
  <FestivalEgg />
  <PwaInstallPrompt />
  <AppUpdatePrompt />
  <AnniversaryReminder />
</template>
<script setup lang="ts">
import { useMessage, useNotification } from 'naive-ui';
import { useSettingStore } from '@/store/settingStore';
import { useAuthStore } from '@/store/authStore';
import { bindNotify } from '@/store/notifyStore';
import { usePwa } from '@/composables/usePwa';
import Onboarding from '@/components/Onboarding.vue';
import HeartBurstLayer from '@/components/Common/HeartBurstLayer.vue';
import PartnerActivityToast from '@/components/Common/PartnerActivityToast.vue';
import FestivalEgg from '@/components/Common/FestivalEgg.vue';
import PwaInstallPrompt from '@/components/Common/PwaInstallPrompt.vue';
import AppUpdatePrompt from '@/components/Common/AppUpdatePrompt.vue';
import AnniversaryReminder from '@/components/Common/AnniversaryReminder.vue';

// 必须在 <n-message-provider> / <n-notification-provider> 之下调用，
// 因此本组件作为 provider 的子节点（见 App.vue）渲染，setup 中才能拿到实例。
useSettingStore().hydrate();
bindNotify(useMessage(), useNotification());

// PWA：注册 Service Worker（离线缓存）+ 监听安装引导 + 后台系统通知
const pwa = usePwa();
pwa.register();
pwa.setupNotifications();

// 原生 WebView 兜底：重载后内存令牌丢失但 refreshToken 仍在（cookie/localStorage）时，
// 启动即静默用 refreshToken 续期，避免路由守卫把已登录用户误踢回登录框（iOS App「点一下退回登录」）。
const auth = useAuthStore();
if (auth.refreshToken && !auth.accessToken) {
  auth.restoreSession().catch(() => {});
}
</script>
