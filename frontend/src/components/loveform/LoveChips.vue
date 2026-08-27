<template>
  <div class="lf-field">
    <label v-if="label" class="lf-label">{{ label }}</label>
    <div class="lf-chips">
      <button
        v-for="opt in options"
        :key="opt"
        type="button"
        class="lf-chip"
        :class="{ active: modelValue === opt }"
        :aria-pressed="modelValue === opt"
        @click="toggle(opt)"
      >
        {{ opt }}
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
const props = withDefaults(
  defineProps<{ modelValue?: string; label?: string; options: string[] }>(),
  { modelValue: '', label: '' }
);
const emit = defineEmits<{ (e: 'update:modelValue', v: string): void }>();

function toggle(opt: string) {
  // 再次点击已选项 → 取消选择
  emit('update:modelValue', props.modelValue === opt ? '' : opt);
}
</script>

<style scoped>
.lf-field { display: flex; flex-direction: column; gap: 8px; }
.lf-label { font-size: 13px; font-weight: 500; color: var(--color-ink-2); padding-left: 2px; }
.lf-chips { display: flex; flex-wrap: wrap; gap: 8px; }
.lf-chip {
  min-height: 38px;
  padding: 0 16px;
  border: 1px solid var(--color-border);
  border-radius: 999px;
  background: var(--color-surface);
  color: var(--color-ink-2);
  font-size: 14px;
  cursor: pointer;
  transition:
    transform var(--dur-micro) var(--fx-ease-back),
    background var(--dur-micro) var(--ease-love),
    color var(--dur-micro) var(--ease-love),
    border-color var(--dur-micro) var(--ease-love),
    box-shadow var(--dur-micro) var(--ease-love);
}
.lf-chip:hover { border-color: var(--color-rose-soft); color: var(--color-rose); }
.lf-chip:active { transform: scale(0.94); }
.lf-chip.active {
  background: var(--color-rose);
  border-color: var(--color-rose);
  color: #fff;
  box-shadow: 0 4px 12px rgba(255, 111, 125, 0.28);
}
</style>
