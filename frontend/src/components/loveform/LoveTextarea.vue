<template>
  <div class="lf-field">
    <label v-if="label" class="lf-label">
      <span>{{ label }}</span>
      <span v-if="counter && maxlength" class="lf-count">{{ String(modelValue ?? '').length }}/{{ maxlength }}</span>
    </label>
    <div class="lf-textarea-wrap" :class="{ 'is-focused': focused, 'is-invalid': invalid }">
      <textarea
        ref="ta"
        class="lf-textarea"
        :value="modelValue"
        :placeholder="placeholder"
        :maxlength="maxlength"
        :rows="rows"
        @input="onInput"
        @focus="focused = true"
        @blur="focused = false"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, nextTick } from 'vue';

const props = withDefaults(
  defineProps<{
    modelValue?: string | null;
    label?: string;
    placeholder?: string;
    maxlength?: number;
    rows?: number;
    maxHeight?: number;
    counter?: boolean;
    invalid?: boolean;
  }>(),
  { modelValue: '', rows: 4, maxHeight: 240, counter: false, invalid: false }
);

const emit = defineEmits<{ (e: 'update:modelValue', v: string): void }>();
const ta = ref<HTMLTextAreaElement>();
const focused = ref(false);

function onInput(e: Event) {
  emit('update:modelValue', (e.target as HTMLTextAreaElement).value);
  autoGrow();
}
function autoGrow() {
  const el = ta.value;
  if (!el) return;
  el.style.height = 'auto';
  el.style.height = Math.min(el.scrollHeight, props.maxHeight) + 'px';
}
watch(() => props.modelValue, () => nextTick(autoGrow));
onMounted(() => nextTick(autoGrow));
</script>

<style scoped>
.lf-field { display: flex; flex-direction: column; gap: 6px; }
.lf-label {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 8px;
  font-size: 13px;
  font-weight: 500;
  color: var(--color-ink-2);
  padding-left: 2px;
}
.lf-count { font-size: 12px; color: var(--color-ink-3); }
.lf-textarea-wrap {
  background: var(--color-surface-2);
  border-radius: 12px;
  padding: 12px 14px;
  transition:
    box-shadow var(--dur-micro) var(--ease-love),
    background var(--dur-micro) var(--ease-love);
}
.lf-textarea-wrap.is-focused {
  background: var(--color-surface);
  box-shadow: 0 0 0 3px var(--color-rose-soft);
}
.lf-textarea-wrap.is-invalid {
  box-shadow: 0 0 0 3px rgba(229, 90, 104, 0.18);
}
.lf-textarea {
  width: 100%;
  border: none;
  outline: none;
  background: transparent;
  resize: none;
  font-size: 16px;
  line-height: 1.7;
  color: var(--color-ink);
  font-family: inherit;
}
.lf-textarea::placeholder { color: var(--color-ink-3); }
</style>
