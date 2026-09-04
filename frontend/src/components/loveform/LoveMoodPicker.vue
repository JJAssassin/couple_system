<template>
  <div class="lf-field">
    <label v-if="label" class="lf-label">
      <span>{{ label }}</span>
      <span v-if="current" class="lf-mood-label">{{ current.label }} {{ modelValue }} 分</span>
    </label>
    <div class="lf-moods" role="radiogroup">
      <button
        v-for="m in moods"
        :key="m.value"
        type="button"
        class="lf-mood"
        :class="{ active: sameRange(modelValue, m.value) }"
        :aria-label="m.label"
        :aria-pressed="sameRange(modelValue, m.value)"
        @click="pick(m.value)"
      >
        <IpIcon :name="m.icon" :size="26" :alt="m.label" class="lf-mood-face" />
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import IpIcon from '@/components/Common/IpIcon.vue';
import { MOOD_LEVELS, moodIconName } from '@/utils/mood';

const props = withDefaults(
  defineProps<{ modelValue?: number; label?: string }>(),
  { modelValue: 6, label: '心情' }
);
const emit = defineEmits<{ (e: 'update:modelValue', v: number): void }>();

// 5 档卡通心情（1→10 由低到高）：点击取各档代表分；历史任意 1-10 分按档位高亮
const moods = MOOD_LEVELS;
const current = computed(() => moods.find((m) => m.icon === moodIconName(props.modelValue)));

function sameRange(a: number | undefined, b: number): boolean {
  return moodIconName(a ?? 6) === moodIconName(b);
}

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
.lf-mood-label { font-size: 13px; color: var(--color-rose-text); font-weight: 600; }
.lf-moods {
  display: grid;
  grid-template-columns: repeat(5, minmax(44px, 1fr));
  gap: 6px;
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
/* IpIcon 是 img：由组件层控制缩放，按钮内做弹性动画 */
.lf-mood-face { transition: transform var(--dur-micro) var(--fx-ease-back); }
.lf-mood:hover { background: var(--color-rose-soft); }
.lf-mood:active { transform: scale(0.9); }
.lf-mood.active {
  background: var(--color-rose-soft);
  box-shadow: 0 0 0 2px var(--color-rose);
}
.lf-mood.active .lf-mood-face { transform: scale(1.25); }
@media (max-width: 380px) {
  .lf-mood-face { width: 22px; height: 22px; }
}
</style>
