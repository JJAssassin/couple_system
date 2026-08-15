<template><span ref="el" class="love-count">0</span></template>
<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useSettingStore } from '@/store/settingStore';

const props = withDefaults(defineProps<{ value: number; duration?: number }>(), { duration: 1200 });
const el = ref<HTMLElement>();
const setting = useSettingStore();

onMounted(() => {
  if (setting.reduceMotion) {
    el.value!.textContent = String(props.value);
    return;
  }
  const dur = props.duration;
  const start = performance.now();
  const tick = (now: number) => {
    const p = Math.min((now - start) / dur, 1);
    const eased = 1 - Math.pow(1 - p, 3); // easeOutCubic
    el.value!.textContent && (el.value!.textContent = String(Math.round(props.value * eased)));
    if (p < 1) requestAnimationFrame(tick);
  };
  requestAnimationFrame(tick);
});
</script>
