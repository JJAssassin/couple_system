<template>
  <Teleport to="body">
    <Transition :name="isBottom ? 'lsheet-bottom' : 'lsheet-center'" @after-leave="onAfterLeave">
      <div v-if="show" class="lsheet-root" :class="{ 'is-bottom': isBottom }">
        <div class="lsheet-mask" :class="{ 'is-closable': maskClosable }" @click="onMaskClick" />
        <div
          ref="panel"
          class="lsheet-panel"
          :class="[{ 'is-bottom': isBottom, 'is-dragging': dragging }, reduceClass ]"
          :style="panelStyle"
          role="dialog"
          aria-modal="true"
        >
          <div class="lsheet-grab" @pointerdown="onGrabDown">
            <span v-if="isBottom" class="lsheet-handle" />
            <div v-if="title || subtitle || $slots.header" class="lsheet-head">
              <slot name="header">
                <div class="lsheet-titles">
                  <h3 v-if="title" class="lsheet-title">{{ title }}</h3>
                  <p v-if="subtitle" class="lsheet-sub">{{ subtitle }}</p>
                </div>
              </slot>
              <button v-if="dismissible" class="lsheet-x" type="button" aria-label="关闭" @click="close">
                <svg viewBox="0 0 24 24" width="18" height="18"><path d="M6 6l12 12M18 6L6 18" stroke="currentColor" stroke-width="2" stroke-linecap="round" /></svg>
              </button>
            </div>
          </div>
          <div class="lsheet-body" :class="{ 'has-foot': $slots.footer }">
            <slot />
          </div>
          <div v-if="$slots.footer" class="lsheet-foot">
            <slot name="footer" />
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch, onBeforeUnmount } from 'vue';
import { isMobile } from '@/composables/useDevice';

const props = withDefaults(
  defineProps<{
    modelValue: boolean;
    title?: string;
    subtitle?: string;
    variant?: 'auto' | 'center' | 'bottom';
    maskClosable?: boolean;
    dismissible?: boolean;
  }>(),
  { variant: 'auto', maskClosable: true, dismissible: true }
);

const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void;
  (e: 'close'): void;
}>();

const show = ref(false);
const reduce = ref(false);
const reduceClass = computed(() => (reduce.value ? 'reduce-motion' : ''));
const isBottom = computed(() => {
  if (props.variant === 'bottom') return true;
  if (props.variant === 'center') return false;
  return isMobile();
});

function prefersReduced() {
  return (
    document.documentElement.classList.contains('reduce-motion') ||
    window.matchMedia('(prefers-reduced-motion: reduce)').matches
  );
}

function open() {
  reduce.value = prefersReduced();
  show.value = true;
  document.body.style.overflow = 'hidden';
  window.addEventListener('keydown', onKey);
}

function doClose(emitEvents: boolean) {
  if (!show.value) return;
  show.value = false;
  document.body.style.overflow = '';
  window.removeEventListener('keydown', onKey);
  if (emitEvents) {
    emit('update:modelValue', false);
    emit('close');
  }
}

function close() {
  if (!props.dismissible) return;
  doClose(true);
}
function onMaskClick() {
  if (props.maskClosable) close();
}
function onKey(e: KeyboardEvent) {
  if (e.key === 'Escape') close();
}
function onAfterLeave() {
  /* 动画离场后无需额外清理 */
}

watch(
  () => props.modelValue,
  (v) => {
    if (v && !show.value) open();
    else if (!v && show.value) doClose(false);
  }
);

onBeforeUnmount(() => {
  document.body.style.overflow = '';
  window.removeEventListener('keydown', onKey);
  window.removeEventListener('pointermove', onGrabMove);
  window.removeEventListener('pointerup', onGrabUp);
});

// —— 拖拽关闭（仅 bottom 变体）——
const panel = ref<HTMLElement>();
const dragging = ref(false);
const dragY = ref(0);
let startY = 0;
let startT = 0;
const panelStyle = computed(() =>
  dragging.value ? { transform: `translateY(${dragY.value}px)` } : {}
);

function onGrabDown(e: PointerEvent) {
  if (!isBottom.value || !props.dismissible) return;
  if ((e.target as HTMLElement).closest('.lsheet-x')) return;
  dragging.value = true;
  startY = e.clientY;
  startT = performance.now();
  (e.target as HTMLElement).setPointerCapture?.(e.pointerId);
  window.addEventListener('pointermove', onGrabMove);
  window.addEventListener('pointerup', onGrabUp);
}
function onGrabMove(e: PointerEvent) {
  if (!dragging.value) return;
  dragY.value = Math.max(0, e.clientY - startY);
}
function onGrabUp() {
  if (!dragging.value) return;
  dragging.value = false;
  window.removeEventListener('pointermove', onGrabMove);
  window.removeEventListener('pointerup', onGrabUp);
  const dy = dragY.value;
  const dt = Math.max(performance.now() - startT, 1);
  const velocity = dy / dt;
  dragY.value = 0;
  if (dy > 120 || velocity > 0.6) close();
}
</script>

<style scoped>
.lsheet-root {
  position: fixed;
  inset: 0;
  z-index: 1000;
  display: flex;
}
.lsheet-root:not(.is-bottom) {
  align-items: center;
  justify-content: center;
  padding: 24px;
}
.lsheet-root.is-bottom {
  align-items: flex-end;
}
.lsheet-mask {
  position: absolute;
  inset: 0;
  background: rgba(28, 22, 24, 0.42);
  backdrop-filter: blur(6px) saturate(115%);
  -webkit-backdrop-filter: blur(6px) saturate(115%);
}
.lsheet-mask.is-closable { cursor: pointer; }

.lsheet-panel {
  position: relative;
  display: flex;
  flex-direction: column;
  max-height: 92vh;
  background: var(--color-surface);
  box-shadow: 0 24px 60px -18px rgba(60, 40, 45, 0.45);
  z-index: 1;
}
.lsheet-root.is-bottom .lsheet-panel {
  width: 100%;
  max-height: 94dvh;
  border-radius: 22px 22px 0 0;
}
.lsheet-root:not(.is-bottom) .lsheet-panel {
  width: min(520px, 100%);
  border-radius: 20px;
}

.lsheet-grab { padding-top: 8px; touch-action: none; }
.lsheet-handle {
  display: block;
  width: 38px;
  height: 5px;
  border-radius: 3px;
  background: var(--color-border);
  margin: 4px auto 6px;
}
.lsheet-head {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 4px 16px 12px;
  position: relative;
}
.lsheet-titles { min-width: 0; }
.lsheet-title {
  margin: 0;
  font-size: 17px;
  font-weight: 600;
  color: var(--color-ink);
}
.lsheet-sub {
  margin: 3px 0 0;
  font-size: 13px;
  color: var(--color-ink-3);
}
.lsheet-x {
  position: absolute;
  right: 12px;
  top: 6px;
  width: 32px;
  height: 32px;
  display: grid;
  place-items: center;
  border: none;
  border-radius: 50%;
  background: var(--color-surface-2);
  color: var(--color-ink-3);
  cursor: pointer;
  transition: all var(--dur-micro) var(--ease-love);
}
.lsheet-x:hover { background: var(--color-rose-soft); color: var(--color-rose); }
.lsheet-x:active { transform: scale(0.92); }

.lsheet-body {
  padding: 4px 18px 18px;
  overflow-y: auto;
  flex: 1;
  -webkit-overflow-scrolling: touch;
  overscroll-behavior: contain;
}
/* 表单字段依次轻升起，营造 iOS 般「逐项落位」的入场节奏 */
.lsheet-body > * {
  animation: lsheet-rise var(--dur-pop) var(--ease-love) both;
}
.lsheet-body > *:nth-child(1) { animation-delay: 0.03s; }
.lsheet-body > *:nth-child(2) { animation-delay: 0.06s; }
.lsheet-body > *:nth-child(3) { animation-delay: 0.09s; }
.lsheet-body > *:nth-child(4) { animation-delay: 0.12s; }
.lsheet-body > *:nth-child(5) { animation-delay: 0.15s; }
.lsheet-body > *:nth-child(6) { animation-delay: 0.18s; }
.lsheet-body > *:nth-child(7) { animation-delay: 0.21s; }
.lsheet-body > *:nth-child(8) { animation-delay: 0.24s; }
@keyframes lsheet-rise {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: none; }
}
.lsheet-body.has-foot { padding-bottom: 4px; }
.lsheet-foot {
  display: flex;
  gap: 10px;
  padding: 12px 18px calc(12px + env(safe-area-inset-bottom, 0px));
  border-top: 1px solid var(--color-border);
  background: var(--color-surface);
}

/* —— 底部抽屉：上滑落位，离场下滑 —— */
.lsheet-bottom-enter-active,
.lsheet-bottom-leave-active { transition: opacity var(--dur-pop) var(--ease-love); }
.lsheet-bottom-enter-active .lsheet-mask,
.lsheet-bottom-leave-active .lsheet-mask { transition: opacity var(--dur-pop) var(--ease-love); }
.lsheet-bottom-enter-from .lsheet-mask,
.lsheet-bottom-leave-to .lsheet-mask { opacity: 0; }
.lsheet-bottom-enter-active .lsheet-panel,
.lsheet-bottom-leave-active .lsheet-panel { transition: transform var(--dur-pop) var(--ease-love); }
.lsheet-bottom-enter-from .lsheet-panel,
.lsheet-bottom-leave-to .lsheet-panel { transform: translateY(100%); }

/* —— 居中卡片：缩放 + 轻微上移，带弹簧回弹 —— */
.lsheet-center-enter-active,
.lsheet-center-leave-active { transition: opacity var(--dur-pop) var(--ease-love); }
.lsheet-center-enter-active .lsheet-panel,
.lsheet-center-leave-active .lsheet-panel {
  transition:
    transform var(--dur-pop) var(--fx-ease-back),
    opacity var(--dur-pop) var(--ease-love);
}
.lsheet-center-enter-from .lsheet-panel,
.lsheet-center-leave-to .lsheet-panel { transform: scale(0.94) translateY(8px); opacity: 0; }

/* 拖拽中关闭过渡，1:1 跟手 */
.lsheet-panel.is-dragging { transition: none !important; }

/* 降级：关闭所有位移动画 */
.lsheet-panel.reduce-motion,
.lsheet-panel.reduce-motion .lsheet-mask { transition: none !important; }
.lsheet-panel.reduce-motion { transform: none !important; }
.lsheet-panel.reduce-motion .lsheet-body > * { animation: none !important; }
</style>
