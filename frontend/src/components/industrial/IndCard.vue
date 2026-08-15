<template>
  <component :is="as" class="ind-card-shell">
    <header v-if="title || $slots.header" class="ind-card-head">
      <IndLed v-if="led" :size="9" />
      <span class="ind-label">{{ title }}</span>
      <slot name="header" />
    </header>
    <div class="ind-card-body"><slot /></div>
  </component>
</template>
<script setup lang="ts">
import IndLed from './IndLed.vue';
withDefaults(defineProps<{ as?: string; title?: string; led?: boolean }>(), {
  as: 'section', led: false,
});
</script>
<style scoped>
.ind-card-shell {
  position: relative; background: var(--color-surface); border: 1px solid var(--color-border);
  border-radius: var(--radius-lg); padding: 18px;
  box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04), 0 10px 28px -10px rgba(122, 100, 98, 0.16);
  transition: transform var(--dur-pop) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love);
}
html:not(.reduce-motion) .ind-card-shell:hover { transform: translateY(-3px); box-shadow: 0 4px 12px rgba(31, 41, 55, 0.06), 0 18px 44px -12px rgba(122, 100, 98, 0.22); }
.ind-card-head { display: flex; align-items: center; gap: 8px; margin-bottom: 12px; }
</style>
