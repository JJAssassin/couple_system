<template>
  <div class="ind-skeleton" :class="[`v-${variant}`, { 'rm': reduceMotion }]" :style="rootStyle">
    <!-- 详情 / 头图占位 -->
    <template v-if="variant === 'hero'">
      <div class="sk-block sk-hero sk-base"></div>
      <div class="sk-line sk-base w-60"></div>
      <div class="sk-line sk-base w-40"></div>
      <div class="sk-line sk-base w-70"></div>
    </template>

    <!-- 网格方块占位（相册 / 卡片列表） -->
    <template v-else-if="variant === 'grid'">
      <div class="sk-grid" :style="{ gridTemplateColumns: `repeat(${columns}, 1fr)` }">
        <div v-for="i in rows" :key="i" class="sk-box sk-base"></div>
      </div>
    </template>

    <!-- 列表行占位（消息 / 时间轴 / 书信） -->
    <template v-else-if="variant === 'list'">
      <div v-for="i in rows" :key="i" class="sk-row">
        <div class="sk-dot sk-base"></div>
        <div class="sk-lines">
          <div class="sk-line sk-base w-80"></div>
          <div class="sk-line sk-base w-50"></div>
        </div>
      </div>
    </template>

    <!-- 默认：纯文本行占位 -->
    <template v-else>
      <div v-for="i in rows" :key="i" class="sk-line sk-base" :class="lineCls(i)"></div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useSettingStore } from '@/store/settingStore';

const props = withDefaults(
  defineProps<{
    variant?: 'text' | 'hero' | 'grid' | 'list';
    rows?: number;
    columns?: number;
    maxWidth?: string;
  }>(),
  { variant: 'text', rows: 3, columns: 3, maxWidth: '' }
);

const setting = useSettingStore();
const reduceMotion = computed(() => setting.reduceMotion);
const rootStyle = computed(() => (props.maxWidth ? { maxWidth: props.maxWidth } : {}));

// 让每行宽度有轻微错落，更像真实内容
function lineCls(i: number) {
  const widths = ['w-90', 'w-70', 'w-80', 'w-55', 'w-65'];
  return widths[(i - 1) % widths.length];
}
</script>

<style scoped>
.ind-skeleton { width: 100%; padding: 4px 2px; }
.v-text, .v-hero { max-width: 520px; }
.v-grid, .v-list { max-width: 100%; }

.sk-line { height: 14px; margin: 10px 0; border-radius: var(--radius-sm); }
.sk-block { border-radius: var(--radius-lg); }
.sk-hero { height: 96px; margin-bottom: 14px; }

.sk-grid { display: grid; gap: 12px; }
.sk-box { aspect-ratio: 1 / 1; border-radius: var(--radius-md); }

.sk-row { display: flex; align-items: center; gap: 14px; padding: 12px 0; }
.sk-dot { width: 44px; height: 44px; border-radius: 50%; flex: 0 0 auto; }
.sk-lines { flex: 1; }
.sk-lines .sk-line:first-child { margin-top: 0; }
.sk-lines .sk-line:last-child { margin-bottom: 0; }

.w-40 { width: 40%; }
.w-50 { width: 50%; }
.w-55 { width: 55%; }
.w-60 { width: 60%; }
.w-65 { width: 65%; }
.w-70 { width: 70%; }
.w-80 { width: 80%; }
.w-90 { width: 90%; }
</style>
