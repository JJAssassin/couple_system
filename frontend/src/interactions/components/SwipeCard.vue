<template>
  <div class="fx-swipe" :class="{ dismissing, armed }">
    <!-- 底层：拖动时随进度淡入的操作意图（归档 / 完成 等） -->
    <div
      v-if="hint"
      class="fx-swipe__behind"
      :class="side"
      :style="{ background: hintColor, opacity: behindOpacity }"
    >
      <span class="fx-swipe__hint" :style="{ opacity: hintOpacity, color: hintColor }">
        {{ side === 'left' ? '←' : '→' }} {{ hint }}
      </span>
    </div>

    <!-- 表层：被抽走的卡片本体（承载 slot） -->
    <div
      ref="el"
      class="fx-swipe__surface"
      :class="{ dragging }"
      :style="surfaceStyle"
      @pointerdown="onDown"
    >
      <slot />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';

const props = withDefaults(
  defineProps<{ threshold?: number; hint?: string; hintColor?: string }>(),
  { threshold: 80, hintColor: '#FF6F7D' }
);
const emit = defineEmits<{ (e: 'dismiss'): void }>();

const el = ref<HTMLElement>();
const offset = ref(0);
const opacity = ref(1);
const dismissing = ref(false);
const dragging = ref(false);
let startX = 0;

// 拖动进度：0 → 1 在阈值处「武装」完成
const progress = computed(() => Math.min(Math.abs(offset.value) / props.threshold, 1));
const behindOpacity = computed(() => progress.value * 0.92);
const hintOpacity = computed(() => Math.min(progress.value * 1.35, 1));
const armed = computed(() => progress.value >= 1);
const side = computed<'left' | 'right'>(() => (offset.value < 0 ? 'left' : 'right'));

// 抽走过程中随位移轻微倾斜 + 缩小，制造「被拎起」的实体感
const rot = computed(() => Math.max(-6, Math.min(6, offset.value / 40)));
const scl = computed(() => 1 - Math.min(Math.abs(offset.value) / 4000, 0.04));

const surfaceStyle = computed(() => ({
  transform: `translateX(${offset.value}px) rotate(${rot.value}deg) scale(${scl.value})`,
  opacity: opacity.value,
}));

function onDown(e: PointerEvent) {
  if (dismissing.value) return;
  dragging.value = true;
  startX = e.clientX;
  const move = (ev: PointerEvent) => {
    if (!dragging.value) return;
    const dx = ev.clientX - startX;
    // 只允许横向抽走；向上/下让位给页面滚动
    let raw = Math.abs(dx) > Math.abs(ev.clientY - e.clientY) ? dx : 0;
    // 越过阈值后施加橡皮筋阻尼，像真实卡片有「拽不动」的回弹上限
    const limit = props.threshold * 1.6;
    if (Math.abs(raw) > limit) {
      const sign = Math.sign(raw);
      raw = limit + (Math.abs(raw) - limit) * 0.25;
      raw *= sign;
    }
    offset.value = raw;
  };
  const up = (ev: PointerEvent) => {
    dragging.value = false;
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
  position: relative;
  overflow: visible;
}
/* 底层操作意图，固定在卡片之下，仅在拖动时随进度露出 */
.fx-swipe__behind {
  position: absolute;
  inset: 0;
  z-index: 0;
  border-radius: inherit;
  display: flex;
  align-items: center;
  padding: 0 22px;
}
.fx-swipe__behind.left { justify-content: flex-start; }
.fx-swipe__behind.right { justify-content: flex-end; }
.fx-swipe__hint {
  font-weight: 700;
  font-size: 14px;
  letter-spacing: 0.04em;
  background: #fff;
  padding: 6px 14px;
  border-radius: 999px;
  box-shadow: 0 4px 14px -4px rgba(31, 41, 55, 0.25);
  white-space: nowrap;
}
/* 表层卡片本体，位移只作用在这一层 */
.fx-swipe__surface {
  position: relative;
  z-index: 1;
  touch-action: pan-y; /* 让页面纵向滚动优先，只有明确横滑才抽走 */
  transform-origin: center bottom;
  will-change: transform, opacity;
  transition: transform var(--fx-dur-pop, 320ms) var(--fx-ease-soft, ease),
    opacity 0.3s ease;
}
.fx-swipe__surface.dragging { transition: none; }
/* 抽走时与逐帧 transform 解耦：用独立过渡让飞出更顺 */
.fx-swipe.dismissing .fx-swipe__surface {
  transition: transform var(--fx-dur-pop, 320ms) var(--fx-ease-soft, ease),
    opacity 0.3s ease;
}
/* 过阈值后底层只留纯色底，强调「即将触发」 */
.fx-swipe.armed .fx-swipe__behind { opacity: 1 !important; }
html.reduce-motion .fx-swipe__surface {
  transition: opacity 0.2s ease;
  transform: none !important;
}
</style>
