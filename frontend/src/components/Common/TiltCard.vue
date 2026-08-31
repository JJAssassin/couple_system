<template>
  <component
    :is="tag"
    ref="root"
    class="uvi-tilt"
    :class="{ 'is-tilting': tilting }"
    @mousemove="onMove"
    @mouseenter="onEnter"
    @mouseleave="onLeave"
  >
    <slot />
    <span class="uvi-glare" aria-hidden="true" />
  </component>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';

const props = withDefaults(
  defineProps<{
    /** 最大倾斜角度（度） */
    max?: number;
    /** 根元素标签，默认 div；可传 'section' 等 */
    tag?: string;
  }>(),
  { max: 9, tag: 'div' }
);

const root = ref<HTMLElement | null>(null);
const tilting = ref(false);
let reduce = false;
// 进入时测量一次「静止态」矩形，鼠标移动时复用：3D 旋转会改变 getBoundingClientRect 的
// 投影框，若每次 move 都重新测量会把旋转结果反馈回鼠标位置比 → 自激振荡（经典 TiltCard
// 抖动根因，日记页即此现象）。enter 时旋转为 0，测得的是未变换的布局框，全程稳定。
let rectLeft = 0;
let rectTop = 0;
let rectW = 0;
let rectH = 0;

onMounted(() => {
  reduce =
    document.documentElement.classList.contains('reduce-motion') ||
    (!!window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
});

function measure() {
  const el = root.value;
  if (!el) return;
  const r = el.getBoundingClientRect();
  rectLeft = r.left;
  rectTop = r.top;
  rectW = r.width;
  rectH = r.height;
}

function onEnter() {
  tilting.value = true;
  measure();
}
function onMove(e: MouseEvent) {
  if (reduce) return;
  const el = root.value;
  if (!el || !rectW || !rectH) return;
  const px = (e.clientX - rectLeft) / rectW - 0.5;
  const py = (e.clientY - rectTop) / rectH - 0.5;
  el.style.setProperty('--uvi-rx', `${(-py * props.max).toFixed(2)}deg`);
  el.style.setProperty('--uvi-ry', `${(px * props.max).toFixed(2)}deg`);
  el.style.setProperty('--uvi-gx', `${(px * 100 + 50).toFixed(1)}%`);
  el.style.setProperty('--uvi-gy', `${(py * 100 + 50).toFixed(1)}%`);
  el.style.setProperty('--uvi-glare', '0.55');
}
function onLeave() {
  tilting.value = false;
  const el = root.value;
  if (!el) return;
  el.style.setProperty('--uvi-rx', '0deg');
  el.style.setProperty('--uvi-ry', '0deg');
  el.style.setProperty('--uvi-glare', '0');
}
</script>
