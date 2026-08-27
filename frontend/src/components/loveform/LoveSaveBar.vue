<template>
  <div class="lsb">
    <button type="button" class="lsb-cancel" :class="{ hidden: hideCancel }" @click="emit('cancel')">
      {{ cancelText }}
    </button>
    <button
      type="button"
      class="lsb-save fx-ripple-host"
      :class="{ done: success, busy: loading }"
      :disabled="loading || success"
      v-press-bounce
      @click="emit('save')"
    >
      <span class="lsb-spinner" v-if="loading" />
      <Transition name="lsb-check">
        <span v-if="success" class="lsb-check"><svg viewBox="0 0 24 24" width="20" height="20"><path d="M5 13l4 4L19 7" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" fill="none" /></svg></span>
      </Transition>
      <span class="lsb-label" :class="{ hide: loading || success }">{{ saveText }}</span>
    </button>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  loading?: boolean;
  success?: boolean;
  cancelText?: string;
  saveText?: string;
  hideCancel?: boolean;
}>();

const emit = defineEmits<{ (e: 'cancel'): void; (e: 'save'): void }>();
</script>

<style scoped>
.lsb { display: flex; gap: 10px; width: 100%; }
.lsb-cancel {
  flex: 0 0 auto;
  min-width: 84px;
  min-height: 46px;
  border: none;
  border-radius: 12px;
  background: var(--color-surface-2);
  color: var(--color-ink-2);
  font-size: 15px;
  font-weight: 500;
  cursor: pointer;
  transition: all var(--dur-micro) var(--ease-love);
}
.lsb-cancel:hover { background: var(--color-ink-soft); }
.lsb-cancel:active { transform: scale(0.97); }
.lsb-cancel.hidden { display: none; }

.lsb-save {
  position: relative;
  flex: 1;
  min-height: 46px;
  border: none;
  border-radius: 12px;
  background: linear-gradient(135deg, var(--color-rose), var(--color-rose-deep));
  color: #fff;
  font-size: 15px;
  font-weight: 600;
  cursor: pointer;
  overflow: hidden;
  display: grid;
  place-items: center;
  box-shadow: 0 6px 16px rgba(255, 111, 125, 0.3);
  transition:
    transform var(--dur-micro) var(--ease-love),
    box-shadow var(--dur-micro) var(--ease-love),
    background var(--dur-pop) var(--ease-love);
}
.lsb-save:hover { box-shadow: 0 8px 20px rgba(255, 111, 125, 0.4); transform: translateY(-1px); }
.lsb-save:active { transform: translateY(0) scale(0.99); }
.lsb-save.busy { cursor: progress; }
.lsb-save.done { background: linear-gradient(135deg, #43c98a, #2fae74); box-shadow: 0 6px 16px rgba(47, 174, 116, 0.3); }

.lsb-label { transition: opacity var(--dur-micro) var(--ease-love); }
.lsb-label.hide { opacity: 0; }

.lsb-spinner {
  position: absolute;
  width: 19px;
  height: 19px;
  border-radius: 50%;
  border: 2.4px solid rgba(255, 255, 255, 0.45);
  border-top-color: #fff;
  animation: lsb-spin 0.7s linear infinite;
}
@keyframes lsb-spin { to { transform: rotate(360deg); } }

.lsb-check {
  position: absolute;
  display: grid;
  place-items: center;
  color: #fff;
}
.lsb-check-enter-active { transition: transform var(--dur-pop) var(--fx-ease-back), opacity var(--dur-pop) var(--ease-love); }
.lsb-check-enter-from { transform: scale(0.4); opacity: 0; }

@media (prefers-reduced-motion: reduce) {
  .lsb-spinner { animation-duration: 1.4s; }
}
</style>
