<template>
  <router-view />
  <Onboarding />
  <HeartBurstLayer />
</template>
<script setup lang="ts">
import { useMessage, useNotification } from 'naive-ui';
import { useSettingStore } from '@/store/settingStore';
import { bindNotify } from '@/store/notifyStore';
import Onboarding from '@/components/Onboarding.vue';
import HeartBurstLayer from '@/components/Common/HeartBurstLayer.vue';

// 必须在 <n-message-provider> / <n-notification-provider> 之下调用，
// 因此本组件作为 provider 的子节点（见 App.vue）渲染，setup 中才能拿到实例。
useSettingStore().hydrate();
bindNotify(useMessage(), useNotification());
</script>
