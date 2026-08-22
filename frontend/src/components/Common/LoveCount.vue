<template><span ref="el" class="love-count">0</span></template>
<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { useSettingStore } from '@/store/settingStore';

const props = withDefaults(defineProps<{ value: number; duration?: number }>(), { duration: 1200 });
const el = ref<HTMLElement>();
const setting = useSettingStore();
let rafId: number | null = null;

onMounted(() => {
  if (setting.reduceMotion) {
    el.value!.textContent = String(props.value);
    return;
  }
  const dur = props.duration;
  const start = performance.now();
  const tick = (now: number) => {
    if (!el.value) return;
    const p = Math.min((now - start) / dur, 1);
    const eased = 1 - Math.pow(1 - p, 3); // easeOutCubic
    el.value.textContent = String(Math.round(props.value * eased));
    if (p < 1) rafId = requestAnimationFrame(tick);
  };
  rafId = requestAnimationFrame(tick);
});

onUnmounted(() => {
  if (rafId !== null) cancelAnimationFrame(rafId);
});
</script>
