<template>
  <div
    ref="root"
    class="pull-refresh"
    @touchstart.passive="onStart"
    @touchmove="onMove"
    @touchend="onEnd"
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

const emit = defineEmits<{ (e: 'refresh'): void }>();

const THRESHOLD = 64; // 触发刷新的下拉距离
const root = ref<HTMLElement>();
const startY = ref(0);
const delta = ref(0);
const pulling = ref(false);
const refreshing = ref(false);

const bodyStyle = computed(() => ({
  transform: `translateY(${Math.min(delta.value, THRESHOLD + 24)}px)`,
  transition: pulling.value ? 'none' : 'transform 0.28s var(--ease-mech)',
}));
const pullText = computed(() => (delta.value >= THRESHOLD ? '松手刷新' : '下拉刷新'));

function onStart(e: TouchEvent) {
  if (refreshing.value) return;
  const el = root.value;
  // 仅在滚动到顶时才允许下拉刷新，避免与列表滚动冲突
  if (el && el.scrollTop > 0) return;
  startY.value = e.touches[0].clientY;
  pulling.value = true;
  delta.value = 0;
}
function onMove(e: TouchEvent) {
  if (!pulling.value) return;
  const dy = e.touches[0].clientY - startY.value;
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
  if (delta.value >= THRESHOLD) {
    refreshing.value = true;
    delta.value = THRESHOLD;
    emit('refresh');
    // 调用方完成刷新后需调用 done()
  } else {
    delta.value = 0;
  }
}

/** 由调用方在刷新数据完成后调用，收起指示器 */
function done() {
  refreshing.value = false;
  delta.value = 0;
}
defineExpose({ done });

onBeforeUnmount(() => {
  pulling.value = false;
  refreshing.value = false;
});
</script>

<style scoped>
.pull-refresh { position: relative; overflow: hidden; }
.pull-refresh__hint {
  position: absolute; left: 0; right: 0; top: 0; text-align: center;
  color: var(--color-ink-3); font-size: 13px; padding: 10px 0;
  transform: translateY(-100%); transition: transform var(--dur-pop) var(--ease-mech);
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
