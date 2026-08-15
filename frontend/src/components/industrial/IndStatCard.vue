<template>
  <div class="ind-stat">
    <div class="ind-label">{{ label }}</div>
    <div class="ind-stat-v"><slot>{{ value }}</slot></div>
    <div v-if="sub" class="ind-stat-sub sub-text">{{ sub }}</div>
  </div>
</template>
<script setup lang="ts">
defineProps<{ label: string; value?: string | number; sub?: string }>();
</script>
<style scoped>
.ind-stat {
  background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg); padding: 16px 18px;
  box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04), 0 10px 28px -10px rgba(122, 100, 98, 0.16);
}
.ind-stat-v {
  font-family: var(--font-mono); font-size: 26px; font-weight: 700; color: var(--color-rose);
  margin-top: 6px; letter-spacing: -0.01em;
}
</style>
