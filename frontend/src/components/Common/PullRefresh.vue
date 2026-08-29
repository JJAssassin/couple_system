<template>
  <div
    ref="root"
    class="pull-refresh"
    @touchstart.passive="onStart"
    @touchmove="onMove"
    @touchend="onEnd"
    @touchcancel="onCancel"
  >
    <div class="pull-refresh__hint" :class="{ show: pulling || refreshing }">
      <span v-if="refreshing" class="pull-refresh__spinner" />
      {{ refreshing ? '刷新中…' : pullText }}
    </div>
    <div class="pull-refresh__body" :style="bodyStyle">
      <slot />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onBeforeUnmount } from 'vue';

/**
 * refresh 事件会把 done 回调作为参数传出，调用方在数据加载完成后调用它收起指示器。
 * 兼容旧用法：调用方若忘记调用，看门狗会在 WATCHDOG_MS 后自动收起，不会永久卡住。
 */
const emit = defineEmits<{ (e: 'refresh', done: () => void): void }>();

const THRESHOLD = 64; // 触发刷新的下拉距离
const DIR_LOCK_PX = 8; // 超过该位移才判定手势方向
const WATCHDOG_MS = 8000; // 调用方未 done() 时的兜底收起时间

const root = ref<HTMLElement>();
const startY = ref(0);
const startX = ref(0);
const delta = ref(0);
const pulling = ref(false);
const refreshing = ref(false);
/** 手势方向锁：none=未判定，vertical=下拉刷新，horizontal=交给横向滚动容器 */
const axis = ref<'none' | 'vertical' | 'horizontal'>('none');
let watchdog: number | null = null;

const bodyStyle = computed(() => ({
  transform: `translateY(${Math.min(delta.value, THRESHOLD + 24)}px)`,
  transition: pulling.value ? 'none' : 'transform 0.28s var(--ease-love)',
}));
const pullText = computed(() => (delta.value >= THRESHOLD ? '松手刷新' : '下拉刷新'));

/**
 * 判断触点所在位置是否已滚动到顶。
 * 组件根节点自身是 overflow:hidden，scrollTop 恒为 0，不能作为依据；
 * 真实滚动可能发生在 document 级，也可能在页面内某个 overflow-y 容器里。
 */
function isAtTop(target: EventTarget | null): boolean {
  const doc = document.scrollingElement || document.documentElement;
  if ((window.scrollY || doc.scrollTop || 0) > 0) return false;

  let el = target instanceof Element ? target : null;
  const stop = root.value;
  while (el && el !== stop) {
    if (el.scrollHeight - el.clientHeight > 1) {
      const oy = getComputedStyle(el).overflowY;
      if ((oy === 'auto' || oy === 'scroll' || oy === 'overlay') && el.scrollTop > 0) return false;
    }
    el = el.parentElement;
  }
  return true;
}

function onStart(e: TouchEvent) {
  if (refreshing.value) return;
  if (e.touches.length !== 1) return;
  if (!isAtTop(e.target)) return;
  startY.value = e.touches[0].clientY;
  startX.value = e.touches[0].clientX;
  pulling.value = true;
  axis.value = 'none';
  delta.value = 0;
}

function onMove(e: TouchEvent) {
  if (!pulling.value) return;
  const dy = e.touches[0].clientY - startY.value;
  const dx = e.touches[0].clientX - startX.value;

  // 方向未定：先判定主轴，横向手势（如胶片相册横滑）直接放弃下拉
  if (axis.value === 'none') {
    if (Math.abs(dx) < DIR_LOCK_PX && Math.abs(dy) < DIR_LOCK_PX) return;
    if (Math.abs(dx) > Math.abs(dy)) {
      axis.value = 'horizontal';
      pulling.value = false;
      delta.value = 0;
      return;
    }
    axis.value = 'vertical';
  }
  if (axis.value !== 'vertical') return;

  if (dy <= 0) {
    delta.value = 0;
    return;
  }
  // 阻尼：越往下拉越"沉"
  delta.value = Math.round(dy * 0.5);
  // 阻止页面整体回弹
  if (e.cancelable) e.preventDefault();
}

function onEnd() {
  if (!pulling.value) return;
  pulling.value = false;
  axis.value = 'none';
  if (delta.value >= THRESHOLD) {
    refreshing.value = true;
    delta.value = THRESHOLD;
    armWatchdog();
    emit('refresh', done);
  } else {
    delta.value = 0;
  }
}

function onCancel() {
  if (!pulling.value) return;
  pulling.value = false;
  axis.value = 'none';
  delta.value = 0;
}

function armWatchdog() {
  clearWatchdog();
  watchdog = window.setTimeout(() => {
    watchdog = null;
    done();
  }, WATCHDOG_MS);
}
function clearWatchdog() {
  if (watchdog !== null) {
    clearTimeout(watchdog);
    watchdog = null;
  }
}

/** 由调用方在刷新数据完成后调用，收起指示器 */
function done() {
  clearWatchdog();
  refreshing.value = false;
  delta.value = 0;
}
defineExpose({ done });

onBeforeUnmount(() => {
  clearWatchdog();
  pulling.value = false;
  refreshing.value = false;
});
</script>

<style scoped>
.pull-refresh { position: relative; overflow: hidden; }
.pull-refresh__hint {
  position: absolute; left: 0; right: 0; top: 0; text-align: center;
  color: var(--color-ink-3); font-size: 13px; padding: 10px 0;
  transform: translateY(-100%); transition: transform var(--dur-pop) var(--ease-love);
  pointer-events: none; z-index: 2;
}
.pull-refresh__hint.show { transform: translateY(0); }
.pull-refresh__spinner {
  display: inline-block; width: 16px; height: 16px; margin-right: 6px; vertical-align: -3px;
  border: 2px solid var(--color-ink-soft); border-top-color: var(--color-accent);
  border-radius: 50%; animation: pr-spin 0.7s linear infinite;
}
.pull-refresh__body { will-change: transform; }
@keyframes pr-spin { to { transform: rotate(360deg); } }
.reduce-motion .pull-refresh__spinner { animation: none; }
</style>
