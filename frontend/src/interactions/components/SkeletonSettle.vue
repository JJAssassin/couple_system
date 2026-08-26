<template>
  <div class="fx-skel">
    <!-- 加载态：骨架与正式内容同尺寸，切换时只做轻位移淡入，杜绝跳动（骨架落位） -->
    <div v-if="loading" class="fx-skel__skeleton" :aria-busy="true">
      <slot name="skeleton">
        <div v-for="i in lines" :key="i" class="sk-base fx-skel__line" :style="{ width: lineWidth(i) }" />
      </slot>
    </div>
    <div v-else class="fx-skel__content fx-settle-in">
      <slot />
    </div>
  </div>
</template>

<script setup lang="ts">
withDefaults(
  defineProps<{ loading: boolean; lines?: number }>(),
  { loading: true, lines: 3 }
);
// 错落宽度，避免骨架像“占位符”一样死板
function lineWidth(i: number): string {
  const widths = ['92%', '78%', '85%', '64%', '70%'];
  return widths[(i - 1) % widths.length];
}
</script>

<style scoped>
.fx-skel { width: 100%; }
.fx-skel__line { height: 14px; margin: 10px 0; border-radius: 7px; }
.fx-skel__line:first-child { margin-top: 0; }
html.reduce-motion .fx-skel__content { animation: none; }
</style>
