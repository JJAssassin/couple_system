<template>
  <div class="lf-field">
    <label v-if="label" class="lf-label">
      <span>{{ label }}</span>
      <span v-if="current" class="lf-mood-label">{{ current.label }}</span>
    </label>
    <div class="lf-moods" role="radiogroup">
      <button
        v-for="m in moods"
        :key="m.value"
        type="button"
        class="lf-mood"
        :class="{ active: modelValue === m.value }"
        :aria-label="m.label"
        :aria-pressed="modelValue === m.value"
        @click="pick(m.value)"
      >
        <span class="lf-mood-face">{{ m.face }}</span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = withDefaults(
  defineProps<{ modelValue?: number; label?: string }>(),
  { modelValue: 5, label: '心情' }
);
const emit = defineEmits<{ (e: 'update:modelValue', v: number): void }>();

// 1→10 由低到高，对应「糟糕」到「幸福」
const moods = [
  { value: 1, face: '😣', label: '糟糕' },
  { value: 2, face: '😞', label: '难过' },
  { value: 3, face: '🙁', label: '低落' },
  { value: 4, face: '😕', label: '一般' },
  { value: 5, face: '😐', label: '平静' },
  { value: 6, face: '🙂', label: '还行' },
  { value: 7, face: '😊', label: '不错' },
  { value: 8, face: '😄', label: '开心' },
  { value: 9, face: '😍', label: '甜蜜' },
  { value: 10, face: '🥰', label: '幸福' },
];
const current = computed(() => moods.find((m) => m.value === props.modelValue));

function pick(v: number) {
  emit('update:modelValue', v);
}
</script>

<style scoped>
.lf-field { display: flex; flex-direction: column; gap: 8px; }
.lf-label {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  font-size: 13px;
  font-weight: 500;
  color: var(--color-ink-2);
  padding-left: 2px;
}
.lf-mood-label { font-size: 13px; color: var(--color-rose); font-weight: 600; }
.lf-moods {
  display: grid;
  grid-template-columns: repeat(10, 1fr);
  gap: 4px;
}
.lf-mood {
  aspect-ratio: 1;
  border: none;
  border-radius: 12px;
  background: var(--color-surface-2);
  cursor: pointer;
  display: grid;
  place-items: center;
  transition:
    transform var(--dur-micro) var(--fx-ease-back),
    background var(--dur-micro) var(--ease-love),
    box-shadow var(--dur-micro) var(--ease-love);
}
.lf-mood-face { font-size: 19px; line-height: 1; transition: transform var(--dur-micro) var(--fx-ease-back); }
.lf-mood:hover { background: var(--color-rose-soft); }
.lf-mood:active { transform: scale(0.9); }
.lf-mood.active {
  background: var(--color-rose-soft);
  box-shadow: 0 0 0 2px var(--color-rose);
}
.lf-mood.active .lf-mood-face { transform: scale(1.25); }
@media (max-width: 380px) {
  .lf-mood-face { font-size: 16px; }
}
</style>
