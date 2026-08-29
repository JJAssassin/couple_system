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
  box-shadow: var(--shadow-card);
}
.ind-stat-v {
  font-family: var(--font-mono); font-size: 26px; font-weight: 700; color: var(--color-rose-text);
  margin-top: 6px; letter-spacing: -0.01em;
}
</style>
