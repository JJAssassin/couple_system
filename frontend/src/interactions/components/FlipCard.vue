<template>
  <div class="fx-flip" :class="{ flipped: modelValue }" @click="onClick">
    <div class="fx-flip__inner">
      <div class="fx-flip__face fx-flip__front"><slot name="front" /></div>
      <div class="fx-flip__face fx-flip__back"><slot name="back" /></div>
    </div>
  </div>
</template>

<script setup lang="ts">
const props = withDefaults(
  defineProps<{ modelValue: boolean; interactive?: boolean }>(),
  { interactive: false }
);
const emit = defineEmits<{ (e: 'update:modelValue', v: boolean): void }>();
function onClick() {
  if (props.interactive) emit('update:modelValue', !props.modelValue);
}
</script>

<style scoped>
.fx-flip {
  perspective: 1200px;
  width: 100%;
}
.fx-flip__inner {
  display: grid; /* 正反面同格堆叠：容器高度随内容自适应，兼容变高卡片 */
  transform-style: preserve-3d;
  transition: transform var(--fx-dur-settle, 420ms) var(--fx-ease-out, ease);
}
.fx-flip.flipped .fx-flip__inner { transform: rotateY(180deg); }
.fx-flip__face {
  grid-area: 1 / 1; /* 两张面叠在同一格，高度取两者最大 */
  -webkit-backface-visibility: hidden;
  backface-visibility: hidden;
  border-radius: var(--radius-lg, 16px);
  overflow: hidden;
}
/* 正反面提前藏好，翻转不穿帮 */
.fx-flip__back { transform: rotateY(180deg); }
html.reduce-motion .fx-flip__inner { transition: none; }
</style>
