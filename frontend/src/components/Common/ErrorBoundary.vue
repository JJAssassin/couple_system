<script setup lang="ts">
import { ref, onErrorCaptured } from 'vue';

withDefaults(
  defineProps<{
    /** 友好标题，可在外层覆盖 */
    title?: string;
  }>(),
  { title: '页面出了点小差错' }
);

const emit = defineEmits<{ (e: 'retry'): void }>();

const error = ref<Error | null>(null);

// 捕获子树（含路由页面 <component :is="Component" />）在 setup / 渲染 /
// 生命周期 / watcher 中抛出的错误，阻止其向上冒泡导致整页白屏。
// 返回 false 表示已处理，Vue 不再继续向上传递。
onErrorCaptured((err) => {
  error.value = err instanceof Error ? err : new Error(String(err));
  return false;
});

function onRetry() {
  error.value = null;
  emit('retry'); // 由外层（AppRoot）强制重建当前路由组件
}
</script>

<template>
  <slot v-if="!error" />
  <div v-else class="err-boundary" role="alert" aria-live="assertive">
    <div class="err-card">
      <div class="err-emoji" aria-hidden="true">💔</div>
      <h2 class="err-title">{{ title }}</h2>
      <p class="err-desc">别担心，你的数据都还在。可以重试一下，或稍后再来看我。</p>
      <pre v-if="error.message" class="err-detail">{{ error.message }}</pre>
      <button class="err-btn" type="button" @click="onRetry">重试一下</button>
    </div>
  </div>
</template>

<style scoped>
.err-boundary {
  min-height: 60vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
  box-sizing: border-box;
}
.err-card {
  max-width: 420px;
  width: 100%;
  text-align: center;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 20px;
  padding: 32px 24px;
  box-shadow: 0 24px 60px -20px rgba(0, 0, 0, 0.35);
}
.err-emoji {
  font-size: 46px;
  line-height: 1;
}
.err-title {
  margin: 16px 0 8px;
  font-size: 19px;
  font-weight: 800;
  color: var(--color-ink);
}
.err-desc {
  margin: 0 0 16px;
  font-size: 14px;
  line-height: 1.7;
  color: var(--color-ink-2);
}
.err-detail {
  margin: 0 0 16px;
  max-height: 120px;
  overflow: auto;
  text-align: left;
  font-size: 12px;
  line-height: 1.5;
  color: var(--color-ink-3);
  background: var(--color-surface-2);
  border-radius: 10px;
  padding: 10px 12px;
  white-space: pre-wrap;
  word-break: break-word;
}
.err-btn {
  appearance: none;
  border: none;
  cursor: pointer;
  padding: 11px 24px;
  border-radius: 999px;
  font-size: 14px;
  font-weight: 700;
  color: var(--color-on-primary);
  background: linear-gradient(135deg, var(--color-rose), var(--color-rose-vivid));
  transition:
    transform var(--dur-micro) var(--ease-love),
    filter var(--dur-micro) var(--ease-love);
}
.err-btn:hover {
  filter: brightness(1.04);
}
.err-btn:active {
  transform: scale(0.97);
}
:global(.reduce-motion) .err-btn {
  transition: none;
}
</style>
