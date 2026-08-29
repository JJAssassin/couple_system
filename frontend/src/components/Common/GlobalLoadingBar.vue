<template>
  <div class="glb" :class="{ on: visible, done: finishing }" role="status" aria-label="加载中">
    <div class="glb-bar" />
  </div>
</template>

<script setup lang="ts">
import { useGlobalLoading } from '@/composables/useGlobalLoading';

const { visible, finishing } = useGlobalLoading();
</script>

<style scoped>
/* 固定顶部 3px 细条，不挡交互 */
.glb {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  height: 3px;
  z-index: 9999;
  pointer-events: none;
  opacity: 0;
  transition: opacity 0.25s var(--ease-love);
}
.glb.on {
  opacity: 1;
}
.glb-bar {
  position: absolute;
  top: 0;
  left: 0;
  height: 100%;
  width: 30%;
  transform-origin: left center;
  background: linear-gradient(90deg, transparent, var(--color-rose), transparent);
}

/* 在途：流星式滑动（仅 transform，符合 frame-smith） */
.glb.on:not(.done) .glb-bar {
  will-change: transform;
  animation: glb-inde 1s ease-in-out infinite;
}
@keyframes glb-inde {
  0% {
    transform: translateX(-100%);
  }
  100% {
    transform: translateX(330%);
  }
}

/* 收尾：填满整条并淡出（scaleX 由 30%→100%，无 width 动画） */
.glb.done .glb-bar {
  width: 100%;
  animation: none;
  transform: scaleX(3.34);
  opacity: 0;
  transition: transform 0.3s var(--ease-love), opacity 0.3s var(--ease-love);
}

/* 晕动症：去动画，仅做静态淡入淡出 */
:global(html.reduce-motion) .glb {
  transition: opacity 0.2s;
}
:global(html.reduce-motion) .glb.on .glb-bar {
  animation: none;
  width: 100%;
  transform: none;
}
:global(html.reduce-motion) .glb.done .glb-bar {
  transform: none;
  opacity: 0;
}
</style>
