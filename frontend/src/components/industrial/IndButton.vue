<template>
  <button class="ind-btn" :class="{ 'ind-btn-accent': accent, 'w-full': block }" :type="type">
    <slot />
  </button>
</template>
<script setup lang="ts">
withDefaults(defineProps<{ accent?: boolean; block?: boolean; type?: 'button' | 'submit' | 'reset' }>(), {
  accent: false, block: false, type: 'button',
});
</script>
