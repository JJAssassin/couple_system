<script setup lang="ts">
/**
 * AuroraBackdrop —— Soft Aurora 柔光背景（reactbits.dev 同名组件的纯 CSS 移植）。
 * 4 个模糊光斑以 transform/opacity 缓慢漂浮，仅用品牌色，零运行时依赖。
 * 颜色随暗色模式（html.dark）与全局 reduce-motion 自动适配 / 关闭。
 */
withDefaults(defineProps<{ strong?: boolean; global?: boolean }>(), { strong: false, global: false })
</script>

<template>
  <div class="aurora" :class="{ 'is-strong': strong, 'is-global': global }" aria-hidden="true">
    <span class="blob b1" />
    <span class="blob b2" />
    <span class="blob b3" />
    <span class="blob b4" />
  </div>
</template>

<style scoped>
.aurora {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
  z-index: 0;
}
.blob {
  position: absolute;
  border-radius: 50%;
  filter: blur(48px);
  opacity: 0.7;
  will-change: transform, opacity;
}
/* 玫瑰主光斑 */
.b1 {
  width: 46vmax;
  height: 46vmax;
  left: -12%;
  top: -18%;
  background: radial-gradient(circle at 30% 30%, rgba(255, 111, 125, 0.55), transparent 62%);
  animation: drift1 26s var(--ease-love) infinite alternate;
}
/* 玫瑰粉次光斑 */
.b2 {
  width: 40vmax;
  height: 40vmax;
  right: -14%;
  top: -10%;
  background: radial-gradient(circle at 70% 30%, rgba(216, 133, 147, 0.5), transparent 62%);
  animation: drift2 32s var(--ease-love) infinite alternate;
}
/* 雾蓝柔光（提亮中央） */
.b3 {
  width: 38vmax;
  height: 38vmax;
  left: 18%;
  bottom: -22%;
  background: radial-gradient(circle at 50% 50%, rgba(232, 238, 242, 0.7), transparent 62%);
  animation: drift3 30s var(--ease-love) infinite alternate;
}
/* 可可暖影（增加层次） */
.b4 {
  width: 30vmax;
  height: 30vmax;
  right: 8%;
  bottom: -16%;
  background: radial-gradient(circle at 50% 50%, rgba(122, 100, 98, 0.28), transparent 62%);
  animation: drift4 34s var(--ease-love) infinite alternate;
}
.is-strong .blob {
  opacity: 0.9;
  filter: blur(40px);
}
/* 全局极淡模式：固定定位，置于内容之下、画布之上，贯穿全站；光斑进一步降透明度 */
.aurora.is-global { position: fixed; inset: 0; z-index: -1; }
.aurora.is-global .blob { opacity: 0.3; }
:global(html.dark) .aurora.is-global .blob { opacity: 0.26; }

@keyframes drift1 {
  from { transform: translate3d(0, 0, 0) scale(1); }
  to { transform: translate3d(8%, 10%, 0) scale(1.12); }
}
@keyframes drift2 {
  from { transform: translate3d(0, 0, 0) scale(1.05); }
  to { transform: translate3d(-10%, 8%, 0) scale(1); }
}
@keyframes drift3 {
  from { transform: translate3d(0, 0, 0) scale(1); }
  to { transform: translate3d(6%, -10%, 0) scale(1.15); }
}
@keyframes drift4 {
  from { transform: translate3d(0, 0, 0) scale(1.1); }
  to { transform: translate3d(-8%, -6%, 0) scale(1); }
}

/* 暗色：光斑更亮、雾蓝降透明度避免发灰，可可换成玫瑰粉做暖光 */
:global(html.dark) .blob { opacity: 0.85; }
:global(html.dark) .b3 {
  background: radial-gradient(circle at 50% 50%, rgba(232, 238, 242, 0.22), transparent 62%);
}
:global(html.dark) .b4 {
  background: radial-gradient(circle at 50% 50%, rgba(216, 133, 147, 0.32), transparent 62%);
}

/* 尊重降级偏好（全局 .reduce-motion 也会强制关闭动画） */
:global(.reduce-motion) .blob { animation: none !important; }
</style>
