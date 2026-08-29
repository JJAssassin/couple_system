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
  background: linear-gradient(90deg, var(--color-border), transparent);
}
</style>
