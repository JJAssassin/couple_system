<template>
  <div class="lf-field">
    <label v-if="label" class="lf-label">{{ label }}</label>
    <div class="lf-input-wrap" :class="{ 'is-focused': focused, 'is-invalid': invalid }">
      <span v-if="$slots.leading" class="lf-leading"><slot name="leading" /></span>
      <input
        class="lf-input"
        :type="type"
        :value="modelValue"
        :placeholder="placeholder"
        :maxlength="maxlength"
        :inputmode="inputmode"
        :enterkeyhint="enterkeyhint"
        @input="onInput"
        @focus="focused = true"
        @blur="focused = false"
      />
      <button v-if="clearable && modelValue !== '' && modelValue !== null" class="lf-clear" type="button" aria-label="清除" @click="clear">
        <svg viewBox="0 0 24 24" width="14" height="14"><path d="M6 6l12 12M18 6L6 18" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" /></svg>
      </button>
      <span v-if="counter && maxlength" class="lf-count">{{ String(modelValue ?? '').length }}/{{ maxlength }}</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

const props = withDefaults(
  defineProps<{
    modelValue?: string | number | null;
    label?: string;
    placeholder?: string;
    type?: 'text' | 'password' | 'email' | 'tel' | 'search' | 'url' | 'number';
    maxlength?: number;
    clearable?: boolean;
    counter?: boolean;
    inputmode?: 'text' | 'numeric' | 'decimal' | 'tel' | 'email' | 'url' | 'search' | 'none';
    enterkeyhint?: 'enter' | 'done' | 'go' | 'next' | 'previous' | 'search' | 'send';
    invalid?: boolean;
  }>(),
  { modelValue: '', type: 'text', clearable: false, counter: false, invalid: false }
);

const emit = defineEmits<{ (e: 'update:modelValue', v: string): void }>();
const focused = ref(false);

function onInput(e: Event) {
  emit('update:modelValue', (e.target as HTMLInputElement).value);
}
function clear() {
  emit('update:modelValue', '');
}
</script>

<style scoped>
.lf-field { display: flex; flex-direction: column; gap: 6px; }
.lf-label {
  font-size: 13px;
  font-weight: 500;
  color: var(--color-ink-2);
  padding-left: 2px;
}
.lf-input-wrap {
  display: flex;
  align-items: center;
  gap: 8px;
  height: 46px;
  padding: 0 12px;
  background: var(--color-surface-2);
  border-radius: 12px;
  transition:
    box-shadow var(--dur-micro) var(--ease-love),
    background var(--dur-micro) var(--ease-love);
}
.lf-input-wrap.is-focused {
  background: var(--color-surface);
  box-shadow: 0 0 0 3px var(--color-rose-soft);
}
.lf-input-wrap.is-invalid {
  box-shadow: 0 0 0 3px rgba(229, 90, 104, 0.18);
}
.lf-input {
  flex: 1;
  min-width: 0;
  height: 100%;
  border: none;
  outline: none;
  background: transparent;
  font-size: 16px;
  color: var(--color-ink);
}
.lf-input::placeholder { color: var(--color-ink-3); }
.lf-leading { display: inline-flex; color: var(--color-ink-3); flex: 0 0 auto; }
.lf-clear {
  flex: 0 0 auto;
  width: 22px;
  height: 22px;
  display: grid;
  place-items: center;
  border: none;
  border-radius: 50%;
  background: var(--color-ink-soft);
  color: var(--color-ink-3);
  cursor: pointer;
  transition: all var(--dur-micro) var(--ease-love);
}
.lf-clear:hover { color: var(--color-ink-2); }
.lf-clear:active { transform: scale(0.9); }
.lf-count {
  flex: 0 0 auto;
  font-size: 12px;
  color: var(--color-ink-3);
  white-space: nowrap;
}
</style>
