<template>
  <teleport to="body">
    <div v-if="visible" class="fx-drawer-root">
      <div class="fx-drawer-mask" :class="{ closing }" @click="close" />
      <div class="fx-drawer-panel" :class="{ closing, reduced }" role="dialog" aria-modal="true">
        <div class="fx-drawer-grab" />
        <div v-if="title || $slots.head" class="fx-drawer-head">
          <slot name="head"><span class="fx-drawer-title">{{ title }}</span></slot>
          <button class="fx-drawer-close" aria-label="关闭" @click="close">×</button>
        </div>
        <div class="fx-drawer-body"><slot /></div>
      </div>
    </div>
  </teleport>
</template>

<script setup lang="ts">
import { ref, watch, onBeforeUnmount } from 'vue';

const props = defineProps<{ modelValue: boolean; title?: string }>();
const emit = defineEmits<{ (e: 'update:modelValue', v: boolean): void }>();

const visible = ref(false);
const closing = ref(false);
const reduced = ref(false);

watch(
  () => props.modelValue,
  (v) => {
    if (v) {
      closing.value = false;
      reduced.value =
        document.documentElement.classList.contains('reduce-motion') ||
        window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ||
        false;
      visible.value = true;
      window.addEventListener('keydown', onKey);
    } else if (visible.value) {
      closing.value = true;
      window.removeEventListener('keydown', onKey);
      setTimeout(() => (visible.value = false), 300);
    }
  }
);

function onKey(e: KeyboardEvent) {
  if (e.key === 'Escape') close();
}
function close() {
  emit('update:modelValue', false);
}
onBeforeUnmount(() => window.removeEventListener('keydown', onKey));
</script>

<style scoped>
.fx-drawer-root { position: fixed; inset: 0; z-index: 90; }
.fx-drawer-mask {
  position: absolute; inset: 0; background: rgba(31, 41, 55, 0.42);
  animation: fx-drawer-mask var(--fx-dur-drawer, 380ms) ease both;
}
.fx-drawer-mask.closing { opacity: 0; animation: none; transition: opacity 0.28s ease; }
/* 底部抽屉：上滑到位后轻微过冲停顿，让用户“看见”它停下了 */
.fx-drawer-panel {
  position: absolute; left: 0; right: 0; bottom: 0;
  background: var(--color-surface);
  border-radius: 22px 22px 0 0;
  box-shadow: 0 -10px 40px rgba(31, 41, 55, 0.18);
  padding: 8px 20px calc(20px + env(safe-area-inset-bottom));
  animation: fx-drawer-up var(--fx-dur-drawer, 380ms) var(--fx-ease-out, ease) both;
}
.fx-drawer-panel.closing { animation: fx-drawer-down 0.3s var(--fx-ease-soft, ease) both; }
.fx-drawer-panel.reduced { animation: fx-fade-in 0.24s ease both; }
.fx-drawer-panel.reduced.closing { animation: fx-fade-out 0.24s ease both; }
.fx-drawer-grab {
  width: 38px; height: 4px; border-radius: 999px; background: var(--color-border);
  margin: 4px auto 8px;
}
.fx-drawer-head { display: flex; align-items: center; justify-content: space-between; padding: 4px 0 12px; }
.fx-drawer-title { font-size: 16px; font-weight: 700; color: var(--color-ink); }
.fx-drawer-close {
  border: none; background: var(--color-surface-2); width: 30px; height: 30px;
  border-radius: 50%; font-size: 18px; cursor: pointer; color: var(--color-ink-2);
  transition: all var(--fx-dur-micro, 140ms) var(--fx-ease-soft, ease);
}
.fx-drawer-close:hover { color: var(--color-rose); }
.fx-drawer-body { max-height: 70vh; overflow: auto; }
@keyframes fx-drawer-down { from { transform: translateY(0); } to { transform: translateY(100%); } }
@keyframes fx-fade-in { from { opacity: 0; } to { opacity: 1; } }
@keyframes fx-fade-out { from { opacity: 1; } to { opacity: 0; } }
html.reduce-motion .fx-drawer-panel,
html.reduce-motion .fx-drawer-panel.closing { animation: none !important; }
html.reduce-motion .fx-drawer-mask { animation: none !important; }
</style>
