<script setup lang="ts">
import { computed } from 'vue'

/**
 * 本地 Heroicons 统一入口。
 * 图标源文件位于 src/assets/icons/heroicons/（outline 主集 + *-solid 填充变体），
 * 由 heroicons.com 官方 SVG 落地，随仓库版本管理，不依赖任何 CDN / npm 运行时包。
 *
 * 用法：
 *   <HeroIcon name="heart" />             → outline 线性心形
 *   <HeroIcon name="heart-solid" />       → 填充心形
 *   <HeroIcon name="bell" :size="20" />  → 显式像素尺寸
 * 颜色继承父级 color（currentColor），可用 class / style 覆盖。
 */
const props = withDefaults(
  defineProps<{
    /** 图标名：'heart' 或 'heart-solid' */
    name: string
    /** 尺寸，数字按 px，字符串原样（默认 1em 随字号） */
    size?: string | number
  }>(),
  { size: '1em' }
)

// 构建期将本地 SVG 以原始字符串全部内联，零运行时网络依赖
const files = import.meta.glob('../../assets/icons/heroicons/*.svg', {
  query: '?raw',
  import: 'default',
  eager: true
}) as Record<string, string>

const svg = computed<string>(() => {
  const key = Object.keys(files).find((k) => k.endsWith(`/${props.name}.svg`))
  return key ? (files[key] as string) : ''
})

if (!svg.value) {
  console.warn(`[HeroIcon] 本地未找到图标: ${props.name}（应在 src/assets/icons/heroicons/ 下）`)
}

const px = (v: string | number) => (typeof v === 'number' ? `${v}px` : v)
</script>

<template>
  <span
    v-if="svg"
    class="hero-icon"
    :style="{ width: px(size), height: px(size) }"
    v-html="svg"
  />
</template>

<style scoped>
.hero-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  line-height: 0;
  color: inherit; /* outline 用 stroke=currentColor，solid 用 fill=currentColor */
}
.hero-icon :deep(svg) {
  width: 100%;
  height: 100%;
  display: block;
}
</style>
