<template>
  <div ref="rootEl" class="ind-stat">
    <div class="ind-label">{{ label }}</div>
    <div class="ind-stat-v"><slot>{{ display }}</slot></div>
    <div v-if="sub" class="ind-stat-sub sub-text">{{ sub }}</div>
  </div>
</template>
<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref, watch } from 'vue';

const props = defineProps<{ label: string; value?: string | number; sub?: string }>();

/**
 * count-up 数字滚动（Apple Keynote / Stripe 年报手法）：
 * - 解析 value 中首个数字（如 "¥123.00" / "72%" / "3 天"），前缀后缀原样保留、小数位保持；
 * - 进入视口（IntersectionObserver）才从 0 滚到位，卡片在折叠区下方不浪费动画帧；
 * - value 后续变化（实时同步）从当前值平滑滚到新值，不闪跳；
 * - reduce-motion（应用开关或系统偏好）直接输出终值；
 * - slot 使用方不受影响（绕过动画，向后兼容）。
 */
const NUM_RE = /-?\d+(\.\d+)?/;
interface Parts { prefix: string; num: number; decimals: number; suffix: string; }

const rootEl = ref<HTMLElement>();
const display = ref<string>('');
const animated = ref(false);
let raf = 0;
let io: IntersectionObserver | null = null;

const parts = computed<Parts | null>(() => {
  const raw = props.value == null ? '' : String(props.value);
  const m = raw.match(NUM_RE);
  if (!m || m.index === undefined) return null;
  return {
    prefix: raw.slice(0, m.index),
    num: parseFloat(m[0]),
    decimals: (m[0].split('.')[1] ?? '').length,
    suffix: raw.slice(m.index + m[0].length),
  };
});

function reducedMotion(): boolean {
  return (
    document.documentElement.classList.contains('reduce-motion') ||
    (typeof window.matchMedia === 'function' && window.matchMedia('(prefers-reduced-motion: reduce)').matches)
  );
}

/** 从当前显示值（或 0）滚到目标值 */
function animateTo(p: Parts) {
  const m = display.value.match(NUM_RE);
  const from = m ? parseFloat(m[0]) : 0;
  animated.value = true;
  if (reducedMotion()) {
    display.value = p.prefix + p.num.toFixed(p.decimals) + p.suffix;
    return;
  }
  const dur = 800;
  const t0 = performance.now();
  const easeOut = (t: number) => 1 - Math.pow(1 - t, 3);
  cancelAnimationFrame(raf);
  const tick = (now: number) => {
    const k = Math.min(1, (now - t0) / dur);
    display.value = p.prefix + (from + (p.num - from) * easeOut(k)).toFixed(p.decimals) + p.suffix;
    if (k < 1) raf = requestAnimationFrame(tick);
  };
  raf = requestAnimationFrame(tick);
}

onMounted(() => {
  const p = parts.value;
  if (!p) {
    display.value = props.value == null ? '' : String(props.value);
    return;
  }
  if (reducedMotion()) {
    display.value = p.prefix + p.num.toFixed(p.decimals) + p.suffix;
    animated.value = true;
    return;
  }
  // 预置 0 值（带前后缀占位，避免布局抖动），入视口后再滚动
  display.value = p.prefix + (0).toFixed(p.decimals) + p.suffix;
  if ('IntersectionObserver' in window) {
    io = new IntersectionObserver(
      (es) => {
        if (es.some((e) => e.isIntersecting)) {
          io?.disconnect();
          io = null;
          const cur = parts.value;
          if (cur) animateTo(cur);
        }
      },
      { threshold: 0.4 },
    );
    if (rootEl.value) io.observe(rootEl.value);
  } else {
    animateTo(p);
  }
});

// 实时同步下的值更新：已动画过 → 从当前值平滑滚到新值；未动画 → 静默更新目标（IO 触发时取最新）
watch(
  () => props.value,
  () => {
    const p = parts.value;
    if (!p) {
      display.value = props.value == null ? '' : String(props.value);
      return;
    }
    if (animated.value) animateTo(p);
  },
);

onUnmounted(() => {
  cancelAnimationFrame(raf);
  io?.disconnect();
  io = null;
});
</script>
<style scoped>
.ind-stat {
  background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg); padding: 16px 16px;
  box-shadow: var(--shadow-card);
}
.ind-stat-v {
  font-family: var(--font-mono); font-size: 26px; font-weight: 700; color: var(--color-rose-text);
  margin-top: 6px; letter-spacing: -0.01em;
  font-variant-numeric: tabular-nums;
}
</style>
