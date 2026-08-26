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
  perspective: 1000px; /* 比 1200 更紧，旋转时透视更明显、更有立体感 */
  width: 100%;
  transition: transform var(--fx-dur-pop, 320ms) var(--fx-ease-out, ease);
  will-change: transform;
}
/* 未翻转时悬停：卡片轻轻抬起 + 投影加深，强化「实体卡片」触感（避免与翻面抢戏） */
.fx-flip:not(.flipped):hover { transform: translateY(-4px); }
.fx-flip__inner {
  display: grid; /* 正反面同格堆叠：容器高度随内容自适应，兼容变高卡片 */
  transform-style: preserve-3d;
  transition: transform var(--fx-dur-flip, 340ms) var(--fx-ease-out, ease);
}
.fx-flip.flipped .fx-flip__inner { transform: rotateY(180deg); }
.fx-flip__face {
  grid-area: 1 / 1; /* 两张面叠在同一格，高度取两者最大 */
  -webkit-backface-visibility: hidden;
  backface-visibility: hidden;
  border-radius: var(--radius-lg, 16px);
  overflow: hidden;
  box-shadow: 0 6px 18px rgba(0, 0, 0, 0.06);
  transition: box-shadow var(--fx-dur-pop, 320ms) var(--fx-ease-soft, ease);
}
/* 正反面沿 Z 轴错开 ±1px → 旋转中呈现约 2px 的虚拟厚度，不再是单层纸片 */
.fx-flip__front { transform: rotateY(0deg) translateZ(1px); }
.fx-flip__back  { transform: rotateY(180deg) translateZ(1px); }
/* 悬停时投影加深（仅未翻转面可见，背面 backface-hidden 不显示） */
.fx-flip:not(.flipped):hover .fx-flip__face {
  box-shadow: 0 14px 30px rgba(0, 0, 0, 0.12);
}
html.reduce-motion .fx-flip,
html.reduce-motion .fx-flip__inner,
html.reduce-motion .fx-flip__face { transition: none; }
html.reduce-motion .fx-flip:not(.flipped):hover { transform: none; }
</style>
