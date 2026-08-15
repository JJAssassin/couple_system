<template>
  <!--
    纯 CSS 整页淡入淡出（opacity）。
    不再使用 gsap / JS 钩子（:css=false）：那种写法在异步路由组件加载时序下，
    容易把内容卡在 opacity:0 导致整页白屏。
    - 首次加载：不做动画，元素直接以默认 opacity:1 显示（绝不会透明）。
    - 路由切换：旧页淡出 → 新页淡入（mode=out-in，避免两页叠放）。
    - 全局 .reduce-motion：关闭过渡，元素直接显示。
  -->
  <transition name="page" mode="out-in">
    <slot />
  </transition>
</template>

<script setup lang="ts">
// 转场完全由下方全局 CSS 控制，无需任何脚本逻辑。
</script>

<!-- 注意：transition 生成的 .page-* 类是全局的，必须写在不带 scoped 的 <style> 中 -->
<style>
.page-enter-active,
.page-leave-active {
  transition: opacity var(--dur-page) var(--ease-love);
  will-change: opacity;
}
.page-enter-from,
.page-leave-to {
  opacity: 0;
}
.page-enter-to,
.page-leave-from {
  opacity: 1;
}
:global(html.reduce-motion) .page-enter-active,
:global(html.reduce-motion) .page-leave-active {
  transition: none;
}
</style>
