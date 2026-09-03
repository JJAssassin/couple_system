<template>
  <teleport to="body">
    <transition name="ob-slide">
      <div v-if="!isOnline" class="offline-bar" role="status" aria-live="polite">
        <span class="ob-ico" aria-hidden="true">📡</span>
        <span class="ob-text">当前离线 · 已缓存的内容仍可查看，恢复网络后自动同步</span>
      </div>
    </transition>
  </teleport>
</template>

<script setup lang="ts">
import { useOnlineStatus } from '@/composables/useOnlineStatus';

const { isOnline } = useOnlineStatus();
</script>

<style scoped>
.offline-bar {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 1300;
  display: flex;
  align-items: center;
  gap: 8px;
  /* 顶部安全区用 calc() 包裹，规避 esbuild 压缩删裸 env() 的坑 */
  padding: calc(env(safe-area-inset-top) + 8px) 14px 8px;
  background: linear-gradient(135deg, #ffb74d 0%, #ff8a65 100%);
  color: #2b1416;
  font-size: 13px;
  font-weight: 600;
  line-height: 1.3;
  text-align: center;
  box-shadow: 0 4px 16px -6px rgba(255, 138, 101, 0.5);
}
.ob-ico {
  font-size: 15px;
  flex-shrink: 0;
}
.ob-text {
  flex: 1;
  min-width: 0;
}
.ob-slide-enter-active,
.ob-slide-leave-active {
  transition:
    transform 0.28s var(--ease-love),
    opacity 0.28s var(--ease-love);
}
.ob-slide-enter-from,
.ob-slide-leave-to {
  transform: translateY(-100%);
  opacity: 0;
}
:global(.reduce-motion) .ob-slide-enter-active,
:global(.reduce-motion) .ob-slide-leave-active {
  transition: none;
}
</style>
