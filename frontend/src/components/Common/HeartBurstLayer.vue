<template>
  <div class="hb-layer" aria-hidden="true">
    <div v-for="b in bursts" :key="b.id" class="hb-burst" :style="{ left: b.x + 'px', top: b.y + 'px' }">
      <span
        v-for="h in b.hearts"
        :key="h.id"
        class="hb-heart"
        :style="{ '--dx': h.dx + 'px', '--dy': h.dy + 'px', '--delay': h.delay + 'ms', '--sc': h.scale }"
      >♥</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { useSettingStore } from '@/store/settingStore';

const setting = useSettingStore();
const bursts = ref<
  { id: number; x: number; y: number; hearts: { id: number; dx: number; dy: number; delay: number; scale: number }[] }[]
>([]);
let idc = 0;

function spawn(x: number, y: number) {
  const n = 5 + Math.floor(Math.random() * 3);
  const hearts = Array.from({ length: n }, () => ({
    id: idc++,
    dx: (Math.random() * 2 - 1) * 64,
    dy: -(58 + Math.random() * 64),
    delay: Math.floor(Math.random() * 160),
    scale: 0.8 + Math.random() * 0.7,
  }));
  const b = { id: idc++, x, y, hearts };
  bursts.value.push(b);
  window.setTimeout(() => {
    bursts.value = bursts.value.filter((z) => z.id !== b.id);
  }, 1600);
}

function onDblClick(e: MouseEvent) {
  // 表单控件内的双击（如选词、选日期）不触发彩蛋，避免干扰
  const t = e.target as HTMLElement | null;
  if (t && typeof t.closest === 'function' && (t.closest('input,textarea,select,.no-heartburst') || t.isContentEditable)) return;
  if (setting.reduceMotion) return;
  spawn(e.clientX, e.clientY);
}

// 供其他组件以编程方式触发心动特效（如首页里程碑彩蛋）
function onCustom(e: Event) {
  if (setting.reduceMotion) return;
  const ce = e as CustomEvent<{ x?: number; y?: number }>;
  const x = typeof ce.detail?.x === 'number' ? ce.detail.x : window.innerWidth / 2;
  const y = typeof ce.detail?.y === 'number' ? ce.detail.y : window.innerHeight * 0.4;
  spawn(x, y);
}

onMounted(() => {
  document.addEventListener('dblclick', onDblClick);
  window.addEventListener('cl-heartburst', onCustom as EventListener);
});
onUnmounted(() => {
  document.removeEventListener('dblclick', onDblClick);
  window.removeEventListener('cl-heartburst', onCustom as EventListener);
});
</script>

<style scoped>
.hb-layer {
  position: fixed;
  inset: 0;
  pointer-events: none;
  z-index: 9999;
  overflow: hidden;
}
.hb-burst {
  position: absolute;
}
.hb-heart {
  position: absolute;
  left: 0;
  top: 0;
  color: var(--color-rose);
  font-size: 18px;
  transform: translate(-50%, -50%);
  text-shadow: 0 2px 8px rgba(255, 111, 125, 0.35);
  animation: hb-float 1.3s var(--ease-love) forwards;
  animation-delay: var(--delay);
}
@keyframes hb-float {
  0% {
    opacity: 0;
    transform: translate(-50%, -50%) scale(0.3);
  }
  20% {
    opacity: 1;
  }
  100% {
    opacity: 0;
    transform: translate(calc(-50% + var(--dx)), calc(-50% + var(--dy))) scale(var(--sc));
  }
}
.reduce-motion .hb-heart {
  animation: none;
}
</style>
