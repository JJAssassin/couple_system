<template>
  <div class="lf-field">
    <label v-if="label" class="lf-label">{{ label }}</label>
    <button type="button" class="lf-date" :class="{ 'is-empty': !modelValue }" @click="open">
      <Calendar :size="16" :stroke-width="2" class="lf-date-ico" />
      <span class="lf-date-text">{{ display || placeholder }}</span>
      <span v-if="modelValue" class="lf-date-clear" role="button" aria-label="清除" @click.stop="clear">
        <svg viewBox="0 0 24 24" width="13" height="13"><path d="M6 6l12 12M18 6L6 18" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" /></svg>
      </span>
      <input
        ref="picker"
        class="lf-date-native"
        :type="mode === 'datetime' ? 'datetime-local' : 'date'"
        :value="nativeValue"
        @change="onChange"
      />
    </button>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { Calendar } from 'lucide-vue-next';

const props = withDefaults(
  defineProps<{ modelValue?: number | null; label?: string; placeholder?: string; mode?: 'date' | 'datetime' }>(),
  { modelValue: null, placeholder: '选择日期', mode: 'date' }
);
const emit = defineEmits<{ (e: 'update:modelValue', v: number | null): void }>();

const picker = ref<HTMLInputElement>();

function pad(n: number) {
  return String(n).padStart(2, '0');
}
const nativeValue = computed(() => {
  if (!props.modelValue) return '';
  const d = new Date(props.modelValue);
  const date = `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  return props.mode === 'datetime' ? `${date}T${pad(d.getHours())}:${pad(d.getMinutes())}` : date;
});

const display = computed(() => {
  if (!props.modelValue) return '';
  const d = new Date(props.modelValue);
  return props.mode === 'datetime'
    ? d.toLocaleString('zh-CN', { month: 'long', day: 'numeric', weekday: 'short', hour: '2-digit', minute: '2-digit' })
    : d.toLocaleDateString('zh-CN', { year: 'numeric', month: 'long', day: 'numeric', weekday: 'short' });
});

function open() {
  picker.value?.showPicker?.() ?? picker.value?.click();
}
function onChange(e: Event) {
  const v = (e.target as HTMLInputElement).value;
  if (!v) { emit('update:modelValue', null); return; }
  emit('update:modelValue', props.mode === 'datetime' ? new Date(v).getTime() : new Date(v + 'T00:00:00').getTime());
}
function clear() {
  emit('update:modelValue', null);
}
</script>

<style scoped>
.lf-field { display: flex; flex-direction: column; gap: 6px; }
.lf-label { font-size: 13px; font-weight: 500; color: var(--color-ink-2); padding-left: 2px; }
.lf-date {
  position: relative;
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  min-height: 46px;
  padding: 0 12px;
  border: none;
  border-radius: 12px;
  background: var(--color-surface-2);
  color: var(--color-ink);
  font-size: 15px;
  cursor: pointer;
  transition:
    box-shadow var(--dur-micro) var(--ease-love),
    background var(--dur-micro) var(--ease-love);
}
.lf-date.is-empty { color: var(--color-ink-3); }
.lf-date:hover { background: var(--color-rose-soft); }
.lf-date:active { transform: scale(0.99); }
.lf-date-ico { color: var(--color-rose); flex: 0 0 auto; }
.lf-date-text { flex: 1; text-align: left; }
.lf-date-clear {
  flex: 0 0 auto;
  width: 20px;
  height: 20px;
  display: grid;
  place-items: center;
  border-radius: 50%;
  background: var(--color-ink-soft);
  color: var(--color-ink-3);
}
.lf-date-native {
  position: absolute;
  inset: 0;
  opacity: 0;
  width: 100%;
  height: 100%;
  cursor: pointer;
}
</style>
