<template>
  <div class="lf-field">
    <label v-if="label" class="lf-label" :for="uid">{{ label }}</label>
    <div class="lf-input-wrap" :class="{ 'is-focused': focused, 'is-invalid': invalid }">
      <span v-if="$slots.leading" class="lf-leading"><slot name="leading" /></span>
      <input
        :id="uid"
        class="lf-input"
        :type="type"
        :value="modelValue"
        :placeholder="placeholder"
        :maxlength="maxlength"
        :inputmode="inputmode"
        :enterkeyhint="enterkeyhint"
        :aria-label="label ? undefined : (placeholder || '请输入')"
        :aria-invalid="invalid || !!error || undefined"
        :aria-required="required || undefined"
        :aria-describedby="error ? errId : undefined"
        @input="onInput"
        @focus="focused = true"
        @blur="focused = false"
      />
      <button v-if="clearable && modelValue !== '' && modelValue !== null" class="lf-clear" type="button" aria-label="清除" @click="clear">
        <svg viewBox="0 0 24 24" width="14" height="14"><path d="M6 6l12 12M18 6L6 18" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" /></svg>
      </button>
      <span v-if="counter && maxlength" class="lf-count">{{ String(modelValue ?? '').length }}/{{ maxlength }}</span>
    </div>
    <p v-if="error" :id="errId" class="lf-error">{{ error }}</p>
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
    required?: boolean;
    error?: string;
  }>(),
  { modelValue: '', type: 'text', clearable: false, counter: false, invalid: false, required: false, error: '' }
);

const emit = defineEmits<{ (e: 'update:modelValue', v: string): void }>();
const focused = ref(false);
const uid = 'lfi-' + Math.random().toString(36).slice(2, 9);
const errId = uid + '-err';

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
  background: var(--color-surface-glass);
  /* 聚焦光晕环：玫瑰柔光外扩 + 微投影，呼应全局玻璃质感 */
  box-shadow: 0 0 0 4px var(--color-rose-soft), 0 6px 18px -6px rgba(255, 111, 125, 0.35);
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
.lf-error {
  font-size: 12px;
  color: var(--color-danger, #e55a68);
  padding-left: 2px;
}
</style>
