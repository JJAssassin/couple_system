<template>
  <div class="hearts" aria-hidden="true">
    <span v-for="h in hearts" :key="h.id" class="heart" :style="h.style">
      <svg viewBox="0 0 24 24" class="h-svg">
        <path :d="HEART_PATH" :fill="h.color" />
      </svg>
    </span>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useSettingStore } from '@/store/settingStore';

withDefaults(defineProps<{ count?: number }>(), { count: 14 });

const setting = useSettingStore();

const HEART_PATH =
  'M12 21s-7-4.5-9.5-9C1 9 2.5 5.5 6 5.5c2 0 3.2 1.2 4 2.3.8-1.1 2-2.3 4-2.3 3.5 0 5 3.5 3.5 6.5C19 16.5 12 21 12 21z';
const COLORS = ['#ff6f7d', '#D88593', '#F4A9B8', '#E8EEF2', '#7A6462'];

interface Heart {
  id: number;
  color: string;
  style: Record<string, string>;
}

function rand(min: number, max: number) {
  return min + Math.random() * (max - min);
}

// 漂浮爱心：从底部缓缓升起、轻微左右摇摆、缩放呼吸。reduceMotion 时完全不渲染，避免晕动。
const hearts = ref<Heart[]>([]);
if (!setting.reduceMotion) {
  const list: Heart[] = [];
  for (let i = 0; i < 14; i++) {
    const left = rand(0, 100);
    const dur = rand(7, 14);
    const delay = rand(0, 12);
    const scale = rand(0.7, 1.5);
    const drift = rand(-26, 26);
    list.push({
      id: i,
      color: COLORS[Math.floor(Math.random() * COLORS.length)],
      style: {
        left: `${left}%`,
        bottom: `${rand(-10, 10)}%`,
        fontSize: `${rand(14, 26)}px`,
        animationDuration: `${dur}s`,
        animationDelay: `${delay}s`,
        '--drift': `${drift}px`,
        '--scale': String(scale),
        '--rise-opacity': String(rand(0.25, 0.7)),
      },
    });
  }
  hearts.value = list;
}
</script>

<style scoped>
.hearts {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
  z-index: 0;
}
.heart {
  position: absolute;
  will-change: transform, opacity;
  animation-name: floatUp;
  animation-timing-function: ease-in-out;
  animation-iteration-count: infinite;
}
.h-svg { width: 1em; height: 1em; display: block; filter: drop-shadow(1px 2px 2px rgba(31, 41, 55, 0.12)); }
@keyframes floatUp {
  0% {
    transform: translate(0, 0) scale(var(--scale));
    opacity: 0;
  }
  12% {
    opacity: var(--rise-opacity, 0.6);
  }
  50% {
    transform: translate(calc(var(--drift) * 0.6), -52vh) scale(calc(var(--scale) * 1.08));
  }
  88% {
    opacity: var(--rise-opacity, 0.6);
  }
  100% {
    transform: translate(var(--drift), -104vh) scale(var(--scale));
    opacity: 0;
  }
}
.reduce-motion .hearts { display: none; }
</style>
