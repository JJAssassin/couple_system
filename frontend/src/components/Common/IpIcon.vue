<script setup lang="ts">
import { computed } from 'vue';

const props = withDefaults(
  defineProps<{
    name: string;
    size?: number | string;
    alt?: string;
  }>(),
  { size: 40 }
);

const url = computed(() => {
  try {
    return new URL(`../../assets/icons/ip/${props.name}.png`, import.meta.url).href;
  } catch {
    return '';
  }
});

const px = computed(() => (typeof props.size === 'number' ? `${props.size}px` : props.size));

function hideOnError(e: Event) {
  const t = e.currentTarget as HTMLElement | null;
  if (t) t.style.display = 'none';
}
</script>

<template>
  <img
    :src="url"
    :alt="alt || name"
    class="ip-icon"
    loading="lazy"
    :style="{ width: px, height: px }"
    @error="hideOnError"
  />
</template>

<style scoped>
.ip-icon {
  object-fit: contain;
  display: inline-flex;
  vertical-align: middle;
  flex-shrink: 0;
}
</style>
