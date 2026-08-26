<template>
  <span class="fx-burger" :class="{ active: modelValue }" role="img" :aria-label="modelValue ? '菜单已展开' : '菜单已收起'">
    <span class="fx-burger__bar" />
    <span class="fx-burger__bar" />
    <span class="fx-burger__bar" />
  </span>
</template>

<script setup lang="ts">
// 纯展示型图标：由外层按钮负责点击与可访问性，这里只做“汉堡→叉”的单一元件形变，
// 用户不会在形变过程中丢失视觉锚点。
defineProps<{ modelValue: boolean }>();
</script>

<style scoped>
/* 单一元件形变：三道横杠旋转成叉，用户不会丢失视觉锚点 */
.fx-burger {
  position: relative; display: block; width: 22px; height: 18px; color: currentColor;
}
.fx-burger__bar {
  position: absolute; left: 0; right: 0; height: 2px; border-radius: 2px;
  background: currentColor;
  transition: transform var(--fx-dur-pop, 320ms) var(--fx-ease-back, ease),
              opacity var(--fx-dur-micro, 140ms) ease, top var(--fx-dur-pop, 320ms) var(--fx-ease-back, ease);
}
.fx-burger__bar:nth-child(1) { top: 0; }
.fx-burger__bar:nth-child(2) { top: 8px; }
.fx-burger__bar:nth-child(3) { top: 16px; }
.fx-burger.active .fx-burger__bar:nth-child(1) { top: 8px; transform: rotate(45deg); }
.fx-burger.active .fx-burger__bar:nth-child(2) { opacity: 0; }
.fx-burger.active .fx-burger__bar:nth-child(3) { top: 8px; transform: rotate(-45deg); }
html.reduce-motion .fx-burger__bar { transition: none; }
</style>
