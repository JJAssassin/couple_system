<template>
  <span class="fx-check" :class="{ drawing: playing }" :style="{ width: size + 'px', height: size + 'px' }">
    <svg :viewBox="`0 0 ${size} ${size}`" fill="none">
      <circle
        v-if="showCircle"
        class="fx-check__ring"
        :cx="size / 2" :cy="size / 2" :r="size / 2 - 2"
        :stroke="color" stroke-width="1.8" :opacity="active ? 1 : 0.25"
      />
      <path
        class="fx-check__path"
        :d="path"
        :stroke="color" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"
      />
    </svg>
  </span>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';

const props = withDefaults(
  defineProps<{ active?: boolean; size?: number; color?: string; showCircle?: boolean }>(),
  { active: false, size: 28, color: 'var(--color-rose, #ff6f7d)', showCircle: true }
);

// 对勾路径：从左下到右下再到右上，两段折线
const path = computed(() => {
  const s = props.size;
  const u = s / 28;
  return `M ${7 * u} ${15 * u} L ${12 * u} ${20 * u} L ${21 * u} ${9 * u}`;
});

const playing = ref(false);
// 路径长度（用于 dash 动画），与尺寸成正比
const len = computed(() => props.size * 1.05);

watch(
  () => props.active,
  (v) => {
    if (v) {
      playing.value = false;
      requestAnimationFrame(() => (playing.value = true));
    } else {
      playing.value = false;
    }
  },
  { immediate: true }
);
function onEnd() {
  if (props.active) playing.value = false;
}
</script>

<style scoped>
.fx-check { display: inline-block; line-height: 0; }
.fx-check__path {
  stroke-dasharray: v-bind(len);
  stroke-dashoffset: v-bind(len);
}
/* 对勾逐笔绘制：active 时从 0 描到满，体现“完成的过程感” */
.fx-check.drawing .fx-check__path {
  animation: fx-check-draw var(--fx-dur-settle, 420ms) var(--fx-ease-out, ease) forwards;
}
@keyframes fx-check-draw { to { stroke-dashoffset: 0; } }
html.reduce-motion .fx-check__path { stroke-dashoffset: 0; }
html.reduce-motion .fx-check.drawing .fx-check__path { animation: none; }
</style>
