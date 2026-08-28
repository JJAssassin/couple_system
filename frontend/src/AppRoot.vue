<template>
  <router-view />
  <GlobalLoadingBar />
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

// PWA：VitePWA 自动注册 SW；本处只初始化安装引导 + 后台系统通知
const pwa = usePwa();
pwa.init();
pwa.setupNotifications();

// 移动端：左边缘右滑返回上一页
useSwipeBack();

// 原生 WebView / 普通刷新兜底：重载后内存 accessToken 丢失，但 HttpOnly Cookie cl_rt 仍在，
// 启动即静默调 /auth/refresh 续期，避免路由守卫把已登录用户误踢回登录框（评审 #2）。
const auth = useAuthStore();
if (!auth.accessToken) {
  auth.restoreSession().catch(() => {});
}
</script>
