<template>
  <button
    :type="type"
    :disabled="disabled"
    class="uvi-glow-border uvi-shine uvi-glow-btn"
    @click="$emit('click', $event)"
  >
    <span class="uvi-glow-label"><slot /></span>
  </button>
</template>

<script setup lang="ts">
withDefaults(
  defineProps<{
    type?: 'button' | 'submit';
    disabled?: boolean;
  }>(),
  { type: 'button', disabled: false }
);
defineEmits<{ (e: 'click', ev: MouseEvent): void }>();
</script>

<style scoped>
.uvi-glow-btn {
  width: 100%;
  padding: 12px 18px;
  border: none;
  border-radius: var(--radius-lg, 16px);
  background: linear-gradient(135deg, var(--color-rose) 0%, var(--color-rose-vivid) 100%);
  color: var(--color-on-primary);
  font-size: 15px;
  font-weight: 600;
  letter-spacing: 0.01em;
  cursor: pointer;
  box-shadow: 0 6px 18px -6px rgba(255, 111, 125, 0.5);
  transition:
    background var(--dur-micro) var(--ease-love),
    transform var(--dur-micro) var(--ease-love),
    box-shadow var(--dur-micro) var(--ease-love);
}
.uvi-glow-btn:hover:not(:disabled) {
  background: linear-gradient(135deg, var(--color-rose-hover) 0%, var(--color-rose) 100%);
  transform: translateY(-1px);
  box-shadow: 0 10px 26px -8px rgba(255, 111, 125, 0.6);
}
.uvi-glow-btn:active:not(:disabled) { transform: scale(0.98); }
.uvi-glow-btn:disabled { opacity: 0.6; cursor: default; }
.uvi-glow-label { position: relative; z-index: 2; display: inline-block; }
</style>
