<template>
  <span class="fx-number" :class="{ 'fx-num-bump': bump }" :style="{ fontVariantNumeric: 'tabular-nums' }">{{ text }}</span>
</template>

<script setup lang="ts">
import { computed, toRef, ref, watch, onBeforeUnmount } from 'vue';
import { useNumberRoll } from '../composables/useNumberRoll';

const props = withDefaults(
  defineProps<{
    value: number;
    duration?: number;
    decimals?: number;
    prefix?: string;
    suffix?: string;
  }>(),
  { duration: 720, decimals: 0, prefix: '', suffix: '' }
);

const display = useNumberRoll(toRef(props, 'value'), { duration: props.duration });
const text = computed(() => {
  const n = display.value;
  const fixed = n.toFixed(props.decimals);
  // 千分位
  const [int, dec] = fixed.split('.');
  const withSep = int.replace(/\B(?=(\d{3})+(?!\d))/g, ',');
  return props.prefix + (dec ? `${withSep}.${dec}` : withSep) + props.suffix;
});

// 数值变化时轻微「弹一下」，让 KPI 更有生命力（reduce-motion 下跳过）
function reduceMotion(): boolean {
  if (typeof document === 'undefined') return false;
  if (document.documentElement.classList.contains('reduce-motion')) return true;
  return window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
}
const bump = ref(false);
let timer: ReturnType<typeof setTimeout> | undefined;
watch(
  () => props.value,
  () => {
    if (reduceMotion()) return;
    bump.value = false;
    requestAnimationFrame(() => (bump.value = true));
    if (timer) clearTimeout(timer);
    timer = setTimeout(() => (bump.value = false), 320);
  }
);
onBeforeUnmount(() => {
  if (timer) clearTimeout(timer);
});
</script>

<style scoped>
.fx-number {
  font-variant-numeric: tabular-nums;
  display: inline-block; /* 行内块才能承接 transform 缩放 */
}
.fx-num-bump {
  animation: fx-num-bump var(--fx-dur-pop, 320ms) var(--fx-ease-soft, ease) both;
}
</style>
