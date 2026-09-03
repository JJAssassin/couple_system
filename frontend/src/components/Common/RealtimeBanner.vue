<template>
  <teleport to="body">
    <transition name="rt-slide">
      <div
        v-if="visible"
        class="rt-bar"
        :class="connState === 'reconnecting' ? 'is-reconnecting' : 'is-disconnected'"
        role="status"
        :aria-live="connState === 'disconnected' ? 'assertive' : 'polite'"
      >
        <span class="rt-ico" aria-hidden="true">{{ connState === 'reconnecting' ? '🔄' : '💔' }}</span>
        <span class="rt-text">
          <template v-if="connState === 'reconnecting'">实时连接重连中…</template>
          <template v-else>实时连接已断开 · 点此重新连接</template>
        </span>
        <button v-if="connState === 'disconnected'" class="rt-btn" type="button" @click="onReconnect">
          重连
        </button>
      </div>
    </transition>
  </teleport>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRealtime } from '@/composables/useRealtime';
import { useOnlineStatus } from '@/composables/useOnlineStatus';

const { connState, reconnect } = useRealtime();
const { isOnline } = useOnlineStatus();

// 仅当"网络在线但 SignalR 失联"时提示，避免与 OfflineBanner（浏览器离线）重叠/重复。
// connecting / connected / idle 不显示；浏览器离线时交由 OfflineBanner 提示。
const visible = computed(
  () => isOnline.value && (connState.value === 'reconnecting' || connState.value === 'disconnected')
);

async function onReconnect() {
  await reconnect();
}
</script>

<style scoped>
.rt-bar {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 1320;
  display: flex;
  align-items: center;
  gap: 8px;
  /* 顶部安全区用 calc() 包裹，规避 esbuild 压缩删裸 env() 的坑 */
  padding: calc(env(safe-area-inset-top) + 8px) 14px 8px;
  font-size: 13px;
  font-weight: 600;
  line-height: 1.3;
  text-align: center;
  box-shadow: 0 4px 16px -6px rgba(0, 0, 0, 0.35);
}
/* 重连中：琥珀色（临时态，无需用户介入） */
.rt-bar.is-reconnecting {
  background: linear-gradient(135deg, #ffb74d 0%, #ff8a65 100%);
  color: #2b1416;
}
/* 断开：玫瑰红（需用户点重连按钮） */
.rt-bar.is-disconnected {
  background: linear-gradient(135deg, #ff8a96 0%, #ff5e72 100%);
  color: #fff;
}
.rt-ico {
  font-size: 15px;
  flex-shrink: 0;
}
.rt-text {
  flex: 1;
  min-width: 0;
}
.rt-btn {
  flex-shrink: 0;
  border: none;
  border-radius: 999px;
  padding: 4px 12px;
  font-size: 12px;
  font-weight: 700;
  cursor: pointer;
  background: rgba(255, 255, 255, 0.92);
  color: #c23b50;
  transition: transform 0.12s var(--ease-love);
}
.rt-btn:active {
  transform: scale(0.96);
}
.rt-slide-enter-active,
.rt-slide-leave-active {
  transition:
    transform 0.28s var(--ease-love),
    opacity 0.28s var(--ease-love);
}
.rt-slide-enter-from,
.rt-slide-leave-to {
  transform: translateY(-100%);
  opacity: 0;
}
:global(.reduce-motion) .rt-slide-enter-active,
:global(.reduce-motion) .rt-slide-leave-active {
  transition: none;
}
</style>
