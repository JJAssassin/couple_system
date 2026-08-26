<template>
  <button
    type="button"
    class="fx-switch"
    role="switch"
    :aria-checked="modelValue"
    :disabled="disabled"
    :class="{ on: modelValue, anim: stretch }"
    @click="toggle"
  >
      <span class="fx-switch__track">
        <span class="fx-switch__knob" @animationend="onAnimEnd" />
      </span>
  </button>
</template>

<script setup lang="ts">
import { ref } from 'vue';

const props = withDefaults(defineProps<{ modelValue: boolean; disabled?: boolean }>(), {
  disabled: false,
});
const emit = defineEmits<{ (e: 'update:modelValue', v: boolean): void }>();

const stretch = ref(false);
function toggle() {
  if (props.disabled) return;
  emit('update:modelValue', !props.modelValue);
  // 起步先横向拉长，落位再恢复宽度 —— 制造有质量感的“弹性开关”
  stretch.value = false;
  requestAnimationFrame(() => (stretch.value = true));
}
function onAnimEnd() {
  stretch.value = false;
}
</script>

<style scoped>
.fx-switch {
  border: none; background: none; padding: 0; cursor: pointer; line-height: 0;
  --knob-x: 0px;
}
.fx-switch:disabled { opacity: 0.5; cursor: not-allowed; }
.fx-switch__track {
  display: block; width: 46px; height: 26px; border-radius: 999px;
  background: var(--color-mist);
  transition: background var(--fx-dur-pop, 320ms) var(--fx-ease-soft, ease);
  position: relative;
}
.fx-switch.on { --knob-x: 20px; }
.fx-switch.on .fx-switch__track { background: var(--color-rose, #ff6f7d); }
.fx-switch__knob {
  position: absolute; top: 3px; left: 3px; width: 20px; height: 20px;
  border-radius: 50%; background: #fff;
  box-shadow: 0 1px 3px rgba(31, 41, 55, 0.25);
  transform: translateX(var(--knob-x));
  transition: transform var(--fx-dur-pop, 320ms) var(--fx-ease-back, ease);
}
.fx-switch.anim .fx-switch__knob {
  animation: fx-knob-stretch var(--fx-dur-pop, 320ms) var(--fx-ease-back, ease);
  animation-fill-mode: none;
}
@keyframes fx-knob-stretch {
  0%   { transform: translateX(var(--knob-x)) scaleX(1); }
  38%  { transform: translateX(var(--knob-x)) scaleX(1.35); } /* 横向拉长 */
  100% { transform: translateX(var(--knob-x)) scaleX(1); }
}
html.reduce-motion .fx-switch__knob { transition: none; }
html.reduce-motion .fx-switch.anim .fx-switch__knob { animation: none; }
</style>
