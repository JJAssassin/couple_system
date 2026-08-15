<template>
  <span class="ind-led" :class="{ 'led-green': color === 'green' }" :style="size ? { width: size + 'px', height: size + 'px' } : {}"></span>
</template>
<script setup lang="ts">
withDefaults(defineProps<{ color?: 'accent' | 'green'; size?: number }>(), { color: 'accent' });
</script>
<style scoped>
.led-green { background: #43d17a; }
</style>
