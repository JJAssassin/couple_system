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
  box-shadow: var(--shadow-card), inset 0 1px 0 rgba(255, 255, 255, 0.5);
  transition: transform var(--dur-pop) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love), border-color var(--dur-pop) var(--ease-love);
}
/* 顶部内高光：与 .love-card 统一的玻璃质感语言 */
.ind-card-shell::before {
  content: ''; position: absolute; top: 0; left: 0; right: 0; height: 1px;
  background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.85), transparent);
  pointer-events: none;
}
html:not(.reduce-motion) .ind-card-shell:hover {
  transform: translateY(-3px);
  box-shadow: 0 4px 12px rgba(31, 41, 55, 0.06), 0 18px 44px -12px rgba(122, 100, 98, 0.22), inset 0 1px 0 rgba(255, 255, 255, 0.55);
  border-color: var(--color-rose-soft);
}
.ind-card-head { display: flex; align-items: center; gap: 8px; margin-bottom: 12px; }
</style>
