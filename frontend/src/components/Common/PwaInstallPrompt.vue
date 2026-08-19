<template>
  <teleport to="body">
    <transition name="pwa-slide">
      <div v-if="installAvailable" class="pwa-install">
        <div class="pwa-ico">💞</div>
        <div class="pwa-text">
          <div class="pwa-title">安装到手机主屏</div>
          <div class="pwa-sub">像 App 一样随时打开我们的小世界</div>
        </div>
        <button class="pwa-btn" @click="promptInstall">安装</button>
        <button class="pwa-later" @click="dismissInstall">稍后</button>
      </div>
    </transition>
  </teleport>
</template>

<script setup lang="ts">
import { usePwa } from '@/composables/usePwa';

const { installAvailable, promptInstall, dismissInstall } = usePwa();
</script>

<style scoped>
.pwa-install {
  position: fixed;
  left: 50%;
  bottom: 84px;
  transform: translateX(-50%);
  z-index: 1200;
  display: flex;
  align-items: center;
  gap: 10px;
  max-width: calc(100vw - 24px);
  padding: 10px 12px;
  border-radius: 16px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  box-shadow: 0 12px 40px -12px rgba(122, 100, 98, 0.4);
}
.pwa-ico {
  font-size: 22px;
}
.pwa-text {
  flex: 1;
  min-width: 0;
}
.pwa-title {
  font-size: 14px;
  font-weight: 700;
  color: var(--color-ink);
}
.pwa-sub {
  font-size: 11px;
  color: var(--color-ink-3);
}
.pwa-btn {
  padding: 8px 16px;
  border-radius: 999px;
  border: none;
  cursor: pointer;
  background: var(--color-rose);
  color: #fff;
  font-size: 13px;
  font-weight: 600;
}
.pwa-later {
  padding: 8px 10px;
  border: none;
  background: transparent;
  cursor: pointer;
  color: var(--color-ink-3);
  font-size: 13px;
}
.pwa-slide-enter-active,
.pwa-slide-leave-active {
  transition: all 0.28s var(--ease-love);
}
.pwa-slide-enter-from,
.pwa-slide-leave-to {
  opacity: 0;
  transform: translate(-50%, 16px);
}
:global(.reduce-motion) .pwa-slide-enter-active,
:global(.reduce-motion) .pwa-slide-leave-active {
  transition: none;
}
</style>
