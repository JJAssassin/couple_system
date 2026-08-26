<template>
  <div
    ref="root"
    class="fx-slider"
    :class="{ disabled }"
    @pointerdown="onDown"
  >
    <div class="fx-slider__track">
      <div class="fx-slider__fill" :style="{ width: pct + '%' }" />
      <div class="fx-slider__knob" :style="{ left: pct + '%' }" />
    </div>
    <div class="fx-slider__value" :class="{ pop }">{{ display }}</div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, toRef } from 'vue';
import { useNumberRoll } from '../composables/useNumberRoll';

const props = withDefaults(
  defineProps<{
    modelValue: number;
    min?: number;
    max?: number;
    step?: number;
    disabled?: boolean;
    decimals?: number;
    suffix?: string;
  }>(),
  { min: 0, max: 100, step: 1, disabled: false, decimals: 0, suffix: '' }
);
const emit = defineEmits<{ (e: 'update:modelValue', v: number): void; (e: 'change', v: number): void }>();

const root = ref<HTMLElement>();
const dragging = ref(false);
const pop = ref(false);

const pct = computed(() => {
  const p = (props.modelValue - props.min) / (props.max - props.min);
  return Math.max(0, Math.min(100, p * 100));
});
const display = useNumberRoll(toRef(props, 'modelValue'), { duration: 240 });

function snap(v: number): number {
  const steps = Math.round((v - props.min) / props.step);
  const val = props.min + steps * props.step;
  return Math.max(props.min, Math.min(props.max, +val.toFixed(6)));
}
function valueFromX(clientX: number): number {
  const rect = root.value!.getBoundingClientRect();
  const r = (clientX - rect.left) / rect.width;
  return snap(props.min + r * (props.max - props.min));
}
function set(v: number, commit: boolean) {
  emit('update:modelValue', v);
  if (commit) {
    emit('change', v);
    pop.value = true;
    setTimeout(() => (pop.value = false), 200);
  }
}
function onDown(e: PointerEvent) {
  if (props.disabled) return;
  dragging.value = true;
  set(valueFromX(e.clientX), false);
  const move = (ev: PointerEvent) => dragging.value && set(valueFromX(ev.clientX), false);
  const up = (ev: PointerEvent) => {
    if (!dragging.value) return;
    dragging.value = false;
    set(valueFromX(ev.clientX), true);
    window.removeEventListener('pointermove', move);
    window.removeEventListener('pointerup', up);
  };
  window.addEventListener('pointermove', move);
  window.addEventListener('pointerup', up);
}
</script>

<style scoped>
.fx-slider {
  display: flex; align-items: center; gap: 12px; padding: 8px 2px;
  touch-action: none; user-select: none;
}
.fx-slider.disabled { opacity: 0.5; pointer-events: none; }
.fx-slider__track {
  position: relative; flex: 1; height: 6px; border-radius: 999px;
  background: var(--color-mist); overflow: visible;
}
/* 液态：填充与旋钮使用同一时长/缓动，三组动画同频同步 */
.fx-slider__fill {
  position: absolute; left: 0; top: 0; bottom: 0; border-radius: 999px;
  background: linear-gradient(90deg, var(--color-rose-deep, #d88593), var(--color-rose, #ff6f7d));
  transition: width var(--fx-dur-pop, 320ms) var(--fx-ease-out, ease);
}
.fx-slider__knob {
  position: absolute; top: 50%; width: 18px; height: 18px; border-radius: 50%;
  background: #fff; border: 2px solid var(--color-rose, #ff6f7d);
  box-shadow: 0 2px 8px rgba(255, 111, 125, 0.35);
  transform: translate(-50%, -50%);
  transition: left var(--fx-dur-pop, 320ms) var(--fx-ease-out, ease);
}
.fx-slider__value {
  min-width: 42px; text-align: right; font-variant-numeric: tabular-nums;
  font-weight: 600; color: var(--color-ink); font-size: 14px;
  transition: transform var(--fx-dur-micro, 140ms) var(--fx-ease-back, ease);
}
.fx-slider__value.pop { transform: scale(1.18); color: var(--color-rose); }
html.reduce-motion .fx-slider__fill,
html.reduce-motion .fx-slider__knob { transition: none; }
html.reduce-motion .fx-slider__value { transition: none; }
</style>
