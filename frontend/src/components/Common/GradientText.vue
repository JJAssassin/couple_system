<script setup lang="ts">
/**
 * GradientText —— Shimmer 渐变文字（21st.dev "shining / magic text" 的纯 CSS 移植）。
 * 文字用品牌色渐变 + background-clip:text；高光为一条 transform 平移的白色光带（overlay 混合），
 * 仅用 transform，随全局 reduce-motion 自动关闭。对中文同样生效。
 */
withDefaults(
  defineProps<{
    /** 文案标签语义，默认 span；标题可传 h1/h2/h3 */
    tag?: string
    /** 柔和版：用次级文字色 → 玫瑰的过渡，适合副标题 */
    soft?: boolean
  }>(),
  { tag: 'span', soft: false }
)
</script>

<template>
  <component :is="tag" class="gt" :class="{ 'gt-soft': soft }">
    <span class="gt-text"><slot /></span>
    <span class="gt-sheen" aria-hidden="true" />
  </component>
</template>

<style scoped>
.gt {
  position: relative;
  display: inline-block;
  isolation: isolate; /* 让 sheen 的 mix-blend 限制在自身内 */
  line-height: 1.2;
}
.gt-text {
  background: linear-gradient(
    100deg,
    var(--color-rose) 0%,
    var(--color-rose-vivid) 50%,
    var(--color-rose) 100%
  );
  -webkit-background-clip: text;
  background-clip: text;
  color: transparent;
}
.gt-soft .gt-text {
  background: linear-gradient(
    100deg,
    var(--color-ink-2) 0%,
    var(--color-rose-vivid) 55%,
    var(--color-rose) 100%
  );
  -webkit-background-clip: text;
  background-clip: text;
  color: transparent;
}
/* 高光：一条白色光带沿 X 轴平移扫过（transform-only），overlay 混合到渐变文字上形成流光 */
.gt-sheen {
  position: absolute;
  inset: 0;
  pointer-events: none;
  background: linear-gradient(
    105deg,
    transparent 35%,
    rgba(255, 255, 255, 0.78) 50%,
    transparent 65%
  );
  transform: translateX(-120%);
  mix-blend-mode: overlay;
  animation: gt-sweep 3.6s var(--ease-love) infinite;
}
@keyframes gt-sweep {
  0% { transform: translateX(-120%); }
  55%, 100% { transform: translateX(120%); }
}
/* 暗色下白色高光偏弱，改用浅玫瑰高光保持可见 */
:global(html.dark) .gt-sheen {
  background: linear-gradient(
    105deg,
    transparent 35%,
    rgba(255, 233, 236, 0.5) 50%,
    transparent 65%
  );
}
:global(.reduce-motion) .gt-sheen { animation: none !important; display: none; }
</style>
