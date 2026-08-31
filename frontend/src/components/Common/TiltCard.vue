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

onMounted(() => {
  reduce =
    document.documentElement.classList.contains('reduce-motion') ||
    (!!window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
});

function onEnter() {
  tilting.value = true;
}
function onMove(e: MouseEvent) {
  if (reduce) return;
  const el = root.value;
  if (!el) return;
  const r = el.getBoundingClientRect();
  const px = (e.clientX - r.left) / r.width - 0.5;
  const py = (e.clientY - r.top) / r.height - 0.5;
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
