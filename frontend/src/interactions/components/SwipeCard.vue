<template>
  <div
    ref="el"
    class="fx-swipe"
    :class="{ dismissing }"
    :style="{
      transform: `translateX(${offset}px) rotate(${rot}deg) scale(${scl})`,
      opacity: opacity
    }"
    @pointerdown="onDown"
  >
    <slot />
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';

const props = withDefaults(defineProps<{ threshold?: number }>(), { threshold: 80 });
const emit = defineEmits<{ (e: 'dismiss'): void }>();

const el = ref<HTMLElement>();
const offset = ref(0);
const opacity = ref(1);
const dismissing = ref(false);
let startX = 0;
let dragging = false;

// 抽走过程中随位移轻微倾斜 + 缩小，制造"被拎起"的实体感
const rot = computed(() => Math.max(-6, Math.min(6, offset.value / 40)));
const scl = computed(() => 1 - Math.min(Math.abs(offset.value) / 4000, 0.04));

function onDown(e: PointerEvent) {
  if (dismissing.value) return;
  dragging = true;
  startX = e.clientX;
  const move = (ev: PointerEvent) => {
    if (!dragging) return;
    const dx = ev.clientX - startX;
    // 只允许横向抽走；向上/下让位给页面滚动
    let raw = Math.abs(dx) > Math.abs(ev.clientY - e.clientY) ? dx : 0;
    // 越过阈值后施加橡皮筋阻尼，像真实卡片有"拽不动"的回弹上限
    const limit = props.threshold * 1.6;
    if (Math.abs(raw) > limit) {
      const sign = Math.sign(raw);
      raw = limit + (Math.abs(raw) - limit) * 0.25;
      raw *= sign;
    }
    offset.value = raw;
  };
  const up = (ev: PointerEvent) => {
    dragging = false;
    window.removeEventListener('pointermove', move);
    window.removeEventListener('pointerup', up);
    const dx = ev.clientX - startX;
    if (Math.abs(dx) > props.threshold) {
      dismiss();
    } else {
      offset.value = 0; // 未过阈值，回弹归位
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
  transform-origin: center bottom;
  will-change: transform, opacity;
  transition: transform var(--fx-dur-pop, 320ms) var(--fx-ease-soft, ease), opacity 0.3s ease;
}
/* 抽走时与逐帧 transform 解耦：用独立过渡让飞出更顺 */
.fx-swipe.dismissing { transition: transform var(--fx-dur-pop, 320ms) var(--fx-ease-soft, ease), opacity 0.3s ease; }
html.reduce-motion .fx-swipe { transition: opacity 0.2s ease; transform: none !important; }
</style>
