<template>
  <div class="lf-field">
    <label v-if="label" class="lf-label">{{ label }}</label>
    <div class="lf-seg" role="radiogroup" :style="{ '--n': options.length }">
      <span class="lf-seg-pill" :style="pillStyle" />
      <button
        v-for="opt in options"
        :key="String(opt.value)"
        type="button"
        class="lf-seg-item"
        :class="{ active: modelValue === opt.value }"
        :aria-pressed="modelValue === opt.value"
        @click="pick(opt.value)"
      >
        <span v-if="opt.icon" class="lf-seg-ico"><component :is="opt.icon" :size="16" :stroke-width="2" /></span>
        <span>{{ opt.label }}</span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, type Component } from 'vue';

export interface SegOption<T = string | number> {
  label: string;
  value: T;
  icon?: Component;
}

const props = withDefaults(
  defineProps<{ modelValue?: string | number; label?: string; options: SegOption[] }>(),
  { modelValue: '', label: '' }
);
const emit = defineEmits<{ (e: 'update:modelValue', v: string | number): void }>();

const index = computed(() => Math.max(0, props.options.findIndex((o) => o.value === props.modelValue)));
const pillStyle = computed(() => ({
  width: `calc(100% / ${props.options.length})`,
  transform: `translateX(${index.value * 100}%)`,
}));

function pick(v: string | number) {
  emit('update:modelValue', v);
}
</script>

<style scoped>
.lf-field { display: flex; flex-direction: column; gap: 8px; }
.lf-label { font-size: 13px; font-weight: 500; color: var(--color-ink-2); padding-left: 2px; }
.lf-seg {
  position: relative;
  display: grid;
  grid-template-columns: repeat(var(--n, 3), 1fr);
  gap: 0;
  padding: 4px;
  background: var(--color-surface-2);
  border-radius: 12px;
}
.lf-seg-pill {
  position: absolute;
  top: 4px;
  left: 4px;
  bottom: 4px;
  border-radius: 9px;
  background: var(--color-surface);
  box-shadow: 0 2px 6px rgba(60, 40, 45, 0.14);
  transition: transform var(--dur-pop) var(--fx-ease-back);
  pointer-events: none;
  z-index: 0;
}
.lf-seg-item {
  position: relative;
  z-index: 1;
  min-height: 38px;
  border: none;
  background: transparent;
  border-radius: 9px;
  color: var(--color-ink-2);
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  padding: 0 8px;
  transition: color var(--dur-micro) var(--ease-love);
}
.lf-seg-item:active { transform: scale(0.97); }
.lf-seg-item.active { color: var(--color-rose); font-weight: 600; }
.lf-seg-ico { display: inline-flex; }
</style>
