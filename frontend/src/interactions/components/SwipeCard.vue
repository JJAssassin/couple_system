<template>
  <div
    ref="el"
    class="fx-swipe"
    :class="{ dismissing }"
    :style="{ transform: `translateX(${offset}px)`, opacity: opacity }"
    @pointerdown="onDown"
  >
    <slot />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

const props = withDefaults(defineProps<{ threshold?: number }>(), { threshold: 80 });
const emit = defineEmits<{ (e: 'dismiss'): void }>();

const el = ref<HTMLElement>();
const offset = ref(0);
const opacity = ref(1);
const dismissing = ref(false);
let startX = 0;
let dragging = false;

function onDown(e: PointerEvent) {
  if (dismissing.value) return;
  dragging = true;
  startX = e.clientX;
  const move = (ev: PointerEvent) => {
    if (!dragging) return;
    const dx = ev.clientX - startX;
    // 只允许横向抽走；向上/下让位给页面滚动
    offset.value = Math.abs(dx) > Math.abs(ev.clientY - e.clientY) ? dx : 0;
  };
  const up = (ev: PointerEvent) => {
    dragging = false;
    window.removeEventListener('pointermove', move);
    window.removeEventListener('pointerup', up);
    const dx = ev.clientX - startX;
    if (Math.abs(dx) > props.threshold) {
      dismiss();
    } else {
      offset.value = 0; // 回弹归位
    }
  };
  window.addEventListener('pointermove', move);
  window.addEventListener('pointerup', up);
}

function dismiss() {
  dismissing.value = true;
  const dir = offset.value < 0 ? -1 : 1;
  offset.value = dir * (window.innerWidth || 400);
  opacity.value = 0;
  setTimeout(() => emit('dismiss'), 300);
}
</script>

<style scoped>
.fx-swipe {
  touch-action: pan-y; /* 让页面纵向滚动优先，只有明确横滑才抽走 */
  will-change: transform, opacity;
  transition: transform 0.3s var(--fx-ease-soft, ease), opacity 0.3s ease;
}
.fx-swipe.dismissing { transition: transform 0.3s var(--fx-ease-soft, ease), opacity 0.3s ease; }
html.reduce-motion .fx-swipe { transition: opacity 0.2s ease; }
</style>
