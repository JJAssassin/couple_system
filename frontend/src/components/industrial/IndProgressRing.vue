<template>
  <div class="ring" :style="{ width: size + 'px', height: size + 'px' }">
    <svg :width="size" :height="size" :viewBox="`0 0 ${size} ${size}`">
      <circle :cx="size / 2" :cy="size / 2" :r="r" fill="none" :stroke="track" :stroke-width="stroke" />
      <circle
        class="prog"
        :cx="size / 2" :cy="size / 2" :r="r" fill="none"
        :stroke="color" :stroke-width="stroke" stroke-linecap="round"
        :stroke-dasharray="C" :stroke-dashoffset="offset"
        :transform="`rotate(-90 ${size / 2} ${size / 2})`"
      />
    </svg>
    <div class="center">
      <Flame v-if="flame" class="flame" :size="Math.max(16, Math.round(size * 0.2))" />
      <span class="val" :style="{ color: 'var(--color-rose-text)', fontSize }">{{ display }}</span>
      <span v-if="sublabel" class="sub">{{ sublabel }}</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import { Flame } from 'lucide-vue-next';

const props = withDefaults(
  defineProps<{
    value?: number;
    size?: number;
    stroke?: number;
    color?: string;
    track?: string;
    sublabel?: string;
    flame?: boolean;
    suffix?: string;
    centerText?: string;
  }>(),
  {
    value: 0,
    size: 96,
    stroke: 10,
    color: 'var(--color-accent)',
    track: 'var(--color-ink-soft)',
    flame: false,
    suffix: '',
    centerText: '',
  }
);

const r = computed(() => (props.size - props.stroke) / 2);
const C = computed(() => 2 * Math.PI * r.value);
const offset = ref(C.value);
const display = ref(0);
const fontSize = computed(() => Math.max(14, Math.round(props.size * 0.22)) + 'px');

function animateNumber(target: number) {
  const start = display.value;
  const t0 = performance.now();
  const dur = 800;
  const step = (now: number) => {
    const p = Math.min(1, (now - t0) / dur);
    display.value = Math.round(start + (target - start) * p);
    if (p < 1) requestAnimationFrame(step);
  };
  requestAnimationFrame(step);
}

function apply() {
  const v = Math.max(0, Math.min(100, props.value));
  offset.value = C.value * (1 - v / 100);
  if (props.centerText) display.value = Number(props.centerText) || 0;
  else animateNumber(Math.round(v));
}

onMounted(() => requestAnimationFrame(apply));
watch(() => props.value, apply);

defineExpose({ apply });
</script>

<style scoped>
.ring { position: relative; display: inline-grid; place-items: center; }
.prog { transition: stroke-dashoffset 0.9s var(--ease-love); }
.center {
  position: absolute; inset: 0; display: flex; flex-direction: column;
  align-items: center; justify-content: center; gap: 2px; pointer-events: none;
}
.flame { font-size: 1.1em; line-height: 1; }
.val { font-family: var(--font-mono); font-weight: 600; line-height: 1; }
.val::after { content: v-bind(suffix); font-size: 0.7em; margin-left: 1px; }
.sub { font-size: 11px; color: var(--color-ink-3); }
</style>
