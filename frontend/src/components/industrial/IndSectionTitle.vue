<template>
  <div class="ind-sec-title">
    <IndLed v-if="led" :size="8" />
    <span class="ind-label" role="heading" :aria-level="level">{{ label }}</span>
    <span class="ind-sec-line"></span>
  </div>
</template>
<script setup lang="ts">
import IndLed from './IndLed.vue';
withDefaults(defineProps<{ label: string; led?: boolean; level?: number }>(), { led: false, level: 2 });
</script>
<style scoped>
.ind-sec-title { display: flex; align-items: center; gap: 10px; margin: 0 0 12px; }
.ind-sec-line {
  flex: 1; height: 2px; border-radius: 2px; opacity: 0.6;
  /* 起始端带模块点缀色（diary 蓝 / wish 青 / conflict 橙…），其余走普通分隔线色 */
  background: linear-gradient(
    90deg,
    color-mix(in srgb, var(--module-accent, var(--color-rose)) 35%, transparent),
    var(--color-border) 35%,
    transparent
  );
}
</style>
