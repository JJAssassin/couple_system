<template>
  <div class="route-progress" :style="{ transform: `scaleX(${progress})`, opacity }" />
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';

const router = useRouter();
const progress = ref(0);
const opacity = ref(0);
let raf = 0;

function start() {
  cancelAnimationFrame(raf);
  progress.value = 0;
  opacity.value = 1;
  const tick = () => {
    // 快速爬升到 0.9，给点击瞬间可见的反馈
    progress.value = Math.min(0.9, progress.value + 0.05);
    if (progress.value < 0.9) raf = requestAnimationFrame(tick);
  };
  raf = requestAnimationFrame(tick);
}
function done() {
  progress.value = 1;
  cancelAnimationFrame(raf);
  window.setTimeout(() => {
    opacity.value = 0;
    window.setTimeout(() => (progress.value = 0), 400);
  }, 250);
}

let offBefore: (() => void) | undefined;
let offAfter: (() => void) | undefined;
onMounted(() => {
  offBefore = router.beforeEach(() => {
    start();
    return true;
  });
  offAfter = router.afterEach(() => {
    done();
  });
});
onUnmounted(() => {
  cancelAnimationFrame(raf);
  offBefore?.();
  offAfter?.();
});
</script>

<style scoped>
.route-progress {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  height: 3px;
  z-index: 9999;
  transform-origin: left center;
  transform: scaleX(0);
  opacity: 0;
  pointer-events: none;
  background: linear-gradient(90deg, var(--color-rose), var(--color-rose-deep));
  box-shadow: 0 0 8px color-mix(in srgb, var(--color-rose) 60%, transparent);
  transition: transform 0.3s var(--ease-love), opacity 0.4s var(--ease-love);
}
:global(html.reduce-motion) .route-progress {
  transition: none;
}
</style>
