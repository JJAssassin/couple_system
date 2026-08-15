<template>
  <transition name="lb-fade">
    <div
      v-if="open"
      class="lb"
      @click.self="close"
    >
      <!-- 顶部工具条 -->
      <div class="lb-bar">
        <span class="lb-count">{{ current + 1 }} / {{ images.length }}</span>
        <div class="lb-actions">
          <button class="lb-btn" :class="{ on: isFav }" :title="isFav ? '取消收藏' : '收藏'" @click.stop="cur && emit('toggleFav', cur.id)">
            <Heart :size="17" :fill="isFav ? 'currentColor' : 'none'" />
          </button>
          <button class="lb-btn" title="缩小" @click.stop="zoomBy(-0.5)">－</button>
          <button class="lb-btn" title="放大" @click.stop="zoomBy(0.5)">＋</button>
          <button class="lb-btn" title="适应 / 1:1" @click.stop="resetView">⤢</button>
          <button class="lb-btn close" title="关闭 (Esc)" @click.stop="close">✕</button>
        </div>
      </div>

      <!-- 上一张 / 下一张 -->
      <button class="lb-nav prev" title="上一张 (←)" @click.stop="prev">‹</button>
      <button class="lb-nav next" title="下一张 (→)" @click.stop="next">›</button>

      <!-- 图片舞台 -->
      <div
        class="lb-stage"
        @wheel.prevent="onWheel"
        @dblclick="onDbl"
        @touchstart.passive="onTouchStart"
        @touchmove.passive="onTouchMove"
        @touchend="onTouchEnd"
        @pointerdown="onDown"
        @pointermove="onMove"
        @pointerup="onUp"
        @pointercancel="onUp"
      >
        <img
          v-if="cur"
          :src="curUrl"
          :alt="curRemark || 'photo'"
          class="lb-img"
          :class="{ grabbed: dragging }"
          :style="imgStyle"
          @load="loaded = true"
          @error="loaded = true"
          draggable="false"
        />
        <div v-if="cur && !loaded" class="lb-spinner"><span class="ring" /></div>
      </div>

      <!-- 标题 / 说明 -->
      <transition name="lb-cap">
        <div v-if="curRemark" class="lb-cap">{{ curRemark }}</div>
      </transition>
    </div>
  </transition>
</template>

<script setup lang="ts">
import { ref, computed, watch, onBeforeUnmount } from 'vue';
import { Heart } from 'lucide-vue-next';

export interface LightboxImage {
  id: number;
  url: string;
  remark?: string;
}

const props = defineProps<{
  images: LightboxImage[];
  modelValue: number; // 当前索引；-1 表示关闭
  favs?: Set<number>;
}>();
const emit = defineEmits<{
  (e: 'update:modelValue', v: number): void;
  (e: 'toggleFav', id: number): void;
}>();

const open = computed(() => props.modelValue >= 0 && props.images.length > 0);
const current = computed(() => Math.max(0, Math.min(props.modelValue, props.images.length - 1)));
const cur = computed(() => (open.value ? props.images[current.value] : null));
const curUrl = computed(() => cur.value?.url ?? '');
const curRemark = computed(() => cur.value?.remark ?? '');
const isFav = computed(() => (cur.value && props.favs?.has(cur.value.id)) || false);

const scale = ref(1);
const panX = ref(0);
const panY = ref(0);
const loaded = ref(true);
const dragging = ref(false);

const imgStyle = computed(() => ({
  transform: `translate(${panX.value}px, ${panY.value}px) scale(${scale.value})`,
  cursor: scale.value > 1 ? (dragging.value ? 'grabbing' : 'grab') : 'zoom-in',
}));

watch(
  () => props.modelValue,
  () => {
    scale.value = 1;
    panX.value = 0;
    panY.value = 0;
    loaded.value = !cur.value?.url;
  }
);

function close() {
  emit('update:modelValue', -1);
}
function setIndex(i: number) {
  const n = props.images.length;
  emit('update:modelValue', ((i % n) + n) % n);
}
function prev() {
  if (scale.value > 1) return;
  setIndex(current.value - 1);
}
function next() {
  if (scale.value > 1) return;
  setIndex(current.value + 1);
}
function resetView() {
  scale.value = 1;
  panX.value = 0;
  panY.value = 0;
}
function zoomBy(d: number) {
  scale.value = Math.min(5, Math.max(1, scale.value + d));
  if (scale.value === 1) {
    panX.value = 0;
    panY.value = 0;
  }
}
function onWheel(e: WheelEvent) {
  zoomBy(e.deltaY < 0 ? 0.4 : -0.4);
}
function onDbl() {
  if (scale.value > 1) resetView();
  else {
    scale.value = 2.5;
  }
}

/* 拖拽平移（scale>1 时） */
let startX = 0;
let startY = 0;
let baseX = 0;
let baseY = 0;
function onDown(e: PointerEvent) {
  if (scale.value <= 1) return;
  dragging.value = true;
  startX = e.clientX;
  startY = e.clientY;
  baseX = panX.value;
  baseY = panY.value;
}
function onMove(e: PointerEvent) {
  if (!dragging.value) return;
  panX.value = baseX + (e.clientX - startX);
  panY.value = baseY + (e.clientY - startY);
}
function onUp() {
  dragging.value = false;
}

/* 触摸滑动切图（scale===1 时） */
let tStartX = 0;
let tStartY = 0;
let tMoved = false;
function onTouchStart(e: TouchEvent) {
  if (scale.value > 1) return;
  const t = e.touches[0];
  tStartX = t.clientX;
  tStartY = t.clientY;
  tMoved = false;
}
function onTouchMove(e: TouchEvent) {
  if (scale.value > 1) return;
  const t = e.touches[0];
  if (Math.abs(t.clientX - tStartX) > 8) tMoved = true;
}
function onTouchEnd(e: TouchEvent) {
  if (scale.value > 1) return;
  const t = e.changedTouches[0];
  const dx = t.clientX - tStartX;
  const dy = t.clientY - tStartY;
  if (Math.abs(dx) > 60 && Math.abs(dx) > Math.abs(dy)) {
    if (dx < 0) next();
    else prev();
  } else if (Math.abs(dy) > 80 && Math.abs(dy) > Math.abs(dx)) {
    if (dy > 0 && tMoved) close(); // 下滑关闭
  }
}

function onKey(e: KeyboardEvent) {
  if (!open.value) return;
  if (e.key === 'Escape') close();
  else if (e.key === 'ArrowLeft') prev();
  else if (e.key === 'ArrowRight') next();
}
window.addEventListener('keydown', onKey);
onBeforeUnmount(() => window.removeEventListener('keydown', onKey));
</script>

<style scoped>
.lb {
  position: fixed;
  inset: 0;
  z-index: 300;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(20, 22, 28, 0.72);
  backdrop-filter: blur(10px);
  -webkit-backdrop-filter: blur(10px);
}
.lb-fade-enter-active,
.lb-fade-leave-active {
  transition: opacity 0.26s ease;
}
.lb-fade-enter-from,
.lb-fade-leave-to {
  opacity: 0;
}

.lb-bar {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 16px;
  z-index: 2;
  color: #fff;
}
.lb-count {
  font-family: var(--font-mono);
  font-size: 13px;
  background: rgba(0, 0, 0, 0.35);
  padding: 4px 10px;
  border-radius: 999px;
}
.lb-actions { display: flex; gap: 8px; }
.lb-btn {
  width: 38px;
  height: 38px;
  border-radius: 50%;
  border: none;
  cursor: pointer;
  font-size: 16px;
  line-height: 1;
  color: #fff;
  background: rgba(255, 255, 255, 0.16);
  backdrop-filter: blur(4px);
  transition: transform 0.15s var(--ease-mech), background 0.15s;
}
.lb-btn:hover { background: rgba(255, 255, 255, 0.28); }
.lb-btn:active { transform: scale(0.92); }
.lb-btn.on { background: var(--color-accent); }
.lb-btn.close { background: rgba(255, 111, 125, 0.85); }

.lb-nav {
  position: absolute;
  top: 50%;
  transform: translateY(-50%);
  width: 46px;
  height: 46px;
  border-radius: 50%;
  border: none;
  cursor: pointer;
  font-size: 26px;
  color: #fff;
  background: rgba(255, 255, 255, 0.14);
  z-index: 2;
  transition: background 0.15s, transform 0.15s var(--ease-mech);
}
.lb-nav:hover { background: rgba(255, 255, 255, 0.26); }
.lb-nav:active { transform: translateY(-50%) scale(0.92); }
.lb-nav.prev { left: 16px; }
.lb-nav.next { right: 16px; }

.lb-stage {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  touch-action: none;
}
.lb-img {
  max-width: 92vw;
  max-height: 86vh;
  object-fit: contain;
  border-radius: 8px;
  box-shadow: 0 18px 50px rgba(0, 0, 0, 0.5);
  user-select: none;
  -webkit-user-drag: none;
  transition: transform 0.18s ease;
  will-change: transform;
}
.lb-img.grabbed { transition: none; }

.lb-spinner {
  position: absolute;
  display: grid;
  place-items: center;
}
.lb-spinner .ring {
  width: 34px;
  height: 34px;
  border: 3px solid rgba(255, 255, 255, 0.3);
  border-top-color: #fff;
  border-radius: 50%;
  animation: lb-spin 0.8s linear infinite;
}
@keyframes lb-spin { to { transform: rotate(360deg); } }

.lb-cap {
  position: absolute;
  bottom: 26px;
  left: 50%;
  transform: translateX(-50%);
  max-width: 80vw;
  text-align: center;
  color: #fff;
  font-size: 14px;
  padding: 8px 16px;
  background: rgba(0, 0, 0, 0.35);
  border-radius: 999px;
}
.lb-cap-enter-active,
.lb-cap-leave-active {
  transition: opacity 0.24s ease, transform 0.24s ease;
}
.lb-cap-enter-from,
.lb-cap-leave-to {
  opacity: 0;
  transform: translate(-50%, 10px);
}

@media (max-width: 767px) {
  .lb-nav { width: 40px; height: 40px; font-size: 22px; }
  .lb-img { max-width: 96vw; max-height: 80vh; }
}
</style>
