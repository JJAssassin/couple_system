<template>
  <div class="ind-pager">
    <!-- 分页模式：上一页 / 下一页 + 页码信息 -->
    <template v-if="mode === 'pager'">
      <button class="pg-btn" :disabled="page <= 1 || loading" @click="go(page - 1)">‹ 上一页</button>
      <span class="pg-info">第 {{ page }} / {{ pages }} 页 · 共 {{ total }} 条</span>
      <button class="pg-btn" :disabled="page >= pages || loading" @click="go(page + 1)">下一页 ›</button>
    </template>

    <!-- 加载更多模式：移动端 / 信息流友好 -->
    <template v-else>
      <button v-if="hasMore" class="pg-btn" :disabled="loading" @click="$emit('load-more')">
        <span v-if="loading" class="pg-spin" />{{ loading ? '加载中…' : '加载更多' }}
      </button>
      <span v-else class="pg-info">— 已经到底啦 —</span>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = withDefaults(
  defineProps<{
    page: number;
    pageSize: number;
    total: number;
    mode?: 'pager' | 'more';
    loading?: boolean;
    hasMore?: boolean;
  }>(),
  { mode: 'pager', loading: false, hasMore: false }
);

const emit = defineEmits<{ (e: 'update:page', v: number): void; (e: 'load-more'): void }>();

const pages = computed(() => Math.max(1, Math.ceil(props.total / Math.max(1, props.pageSize))));
function go(p: number) {
  if (p < 1 || p > pages.value || p === props.page) return;
  emit('update:page', p);
}
</script>

<style scoped>
.ind-pager {
  display: flex; align-items: center; justify-content: center; gap: 14px;
  padding: 18px 0 8px; color: var(--color-ink-3); font-size: 13px;
}
.pg-info { font-family: var(--font-mono); letter-spacing: 0.04em; }
.pg-btn {
  min-width: 92px; display: inline-flex; align-items: center; justify-content: center; gap: 6px;
  padding: 8px 14px; border-radius: var(--radius-md); cursor: pointer; font-weight: 600;
  background: var(--color-surface); color: var(--color-ink);
  border: 1px solid var(--color-border);
  transition: all var(--dur-micro) var(--ease-love);
}
.pg-btn:hover:not(:disabled) { color: var(--color-rose); border-color: var(--color-rose-soft); background: var(--color-rose-soft); }
.pg-btn:active:not(:disabled) { transform: scale(0.98); }
.pg-btn:disabled { opacity: 0.45; cursor: not-allowed; }
.pg-spin {
  width: 14px; height: 14px; border: 2px solid var(--color-ink-soft);
  border-top-color: var(--color-rose); border-radius: 50%;
  animation: pg-spin 0.7s linear infinite;
}
@keyframes pg-spin { to { transform: rotate(360deg); } }
.reduce-motion .pg-spin { animation: none; }
</style>
