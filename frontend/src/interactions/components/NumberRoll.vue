<template>
  <span class="fx-number" :style="{ fontVariantNumeric: 'tabular-nums' }">{{ text }}</span>
</template>

<script setup lang="ts">
import { computed, toRef } from 'vue';
import { useNumberRoll } from '../composables/useNumberRoll';

const props = withDefaults(
  defineProps<{
    value: number;
    duration?: number;
    decimals?: number;
    prefix?: string;
    suffix?: string;
  }>(),
  { duration: 720, decimals: 0, prefix: '', suffix: '' }
);

const display = useNumberRoll(toRef(props, 'value'), { duration: props.duration });
const text = computed(() => {
  const n = display.value;
  const fixed = n.toFixed(props.decimals);
  // 千分位
  const [int, dec] = fixed.split('.');
  const withSep = int.replace(/\B(?=(\d{3})+(?!\d))/g, ',');
  return props.prefix + (dec ? `${withSep}.${dec}` : withSep) + props.suffix;
});
</script>

<style scoped>
.fx-number { font-variant-numeric: tabular-nums; }
</style>
