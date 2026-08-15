<template>
  <IndSkeleton v-if="loading" variant="list" :rows="6" />

  <div v-else class="tl-page">
    <!-- 左侧筛选 -->
    <aside class="tl-filter">
      <div class="tl-filter-title">按时间筛选</div>
      <NDatePicker
        v-model:value="monthValue"
        type="month"
        clearable
        placeholder="选择年份/月份"
        style="width: 100%"
      />
      <NButton quaternary block @click="clearFilter">查看全部</NButton>
      <div class="tl-tip sub-text">共 {{ items.length }} 条记录</div>
    </aside>

    <!-- 右侧时间轴 -->
    <section class="tl-wrap">
      <IndEmpty
        v-if="!items.length"
        title="这段时间还没有记录"
        desc="去创造更多回忆吧，把重要时刻都记下来～"
      />
      <div v-else class="tl">
        <span class="tl-line"></span>
        <div v-for="(item, i) in shownItems" :key="item.id" class="tl-item" :style="{ animationDelay: i * 0.05 + 's' }">
          <span class="tl-dot" :class="`t-${item.type}`"></span>
          <div class="love-card tl-card">
            <div class="tl-head">
              <NTag :type="tagType(item.type)" size="small">{{ typeLabel(item.type) }}</NTag>
              <NTag v-if="item.type === 'anniversary' && item.isYearly" size="small" type="primary" round>每年</NTag>
              <span class="tl-date">{{ formatDate(item.date) }}</span>
            </div>
            <div class="tl-title">{{ item.title }}</div>
            <div v-if="item.type === 'anniversary' && item.nextOccurrence" class="tl-summary">
              下次 {{ formatDate(item.nextOccurrence) }} · 还有 <b>{{ daysUntil(item.nextOccurrence) }}</b> 天
            </div>
            <div v-else-if="item.type === 'anniversary' && !item.nextOccurrence" class="tl-summary tl-expired">
              这一天已经过去啦
            </div>
            <div v-else-if="item.summary" class="tl-summary title-clamp">{{ item.summary }}</div>
          </div>
        </div>
      </div>

      <IndPager
        v-if="items.length"
        mode="more"
        :page="1"
        :page-size="15"
        :total="items.length"
        :loading="false"
        :has-more="hasMore"
        @load-more="onLoadMore"
      />
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { NDatePicker, NButton, NTag } from 'naive-ui';
import { listTimeline } from '@/api/timeline';
import type { TimelineItemDto } from '@/types';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndPager from '@/components/industrial/IndPager.vue';

const loading = ref(true);
const items = ref<TimelineItemDto[]>([]);
const monthValue = ref<number | null>(null);
const shown = ref(15);

const shownItems = computed(() => items.value.slice(0, shown.value));
const hasMore = computed(() => items.value.length > shown.value);

const params = computed(() => {
  if (!monthValue.value) return { year: null, month: null };
  const d = new Date(monthValue.value);
  return { year: d.getFullYear(), month: d.getMonth() + 1 };
});

async function load() {
  loading.value = true;
  try {
    items.value = await listTimeline(params.value);
    shown.value = 15;
  } finally {
    loading.value = false;
  }
}

function onLoadMore() {
  shown.value += 15;
}

function clearFilter() {
  monthValue.value = null;
}

const TYPE_META: Record<string, { label: string; tag: 'default' | 'success' | 'warning' | 'error' | 'info' | 'primary' }> = {
  anniversary: { label: '纪念日', tag: 'primary' },
  diary: { label: '日记', tag: 'info' },
  wish: { label: '愿望', tag: 'success' },
  conflict: { label: '矛盾', tag: 'warning' },
};
function typeLabel(t: string) {
  return TYPE_META[t]?.label ?? t;
}
function tagType(t: string) {
  return TYPE_META[t]?.tag ?? 'default';
}
function formatDate(s: string) {
  const d = new Date(s);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}
function daysUntil(s: string) {
  const diff = new Date(s).getTime() - Date.now();
  return Math.max(0, Math.ceil(diff / 86400000));
}

watch(params, load);
onMounted(load);
</script>

<style scoped>
.tl-page { display: flex; gap: 20px; align-items: flex-start; }
.tl-filter {
  width: 200px; flex-shrink: 0; position: sticky; top: 16px;
  background: var(--color-surface); border: 1px solid var(--color-border); border-radius: 16px; padding: 16px;
  box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04), 0 10px 28px -10px rgba(122, 100, 98, 0.16);
}
.tl-filter-title { font-weight: 500; margin-bottom: 12px; }
.tl-tip { margin-top: 12px; }

.tl-wrap { flex: 1; min-width: 0; }
.tl { position: relative; padding-left: 22px; }
.tl-line {
  position: absolute; left: 6px; top: 6px; bottom: 6px; width: 2px;
  background: linear-gradient(var(--color-rose), var(--color-mist));
}
.tl-item { position: relative; margin-bottom: 16px; animation: tlIn 0.4s var(--ease-love) both; }
html.reduce-motion .tl-item { animation: none; }
@keyframes tlIn { from { opacity: 0; transform: translateY(16px); } to { opacity: 1; transform: none; } }
.tl-dot {
  position: absolute; left: -20px; top: 20px; width: 12px; height: 12px;
  border-radius: 50%; background: var(--color-rose); box-shadow: 0 0 0 4px rgba(216, 133, 147, .18);
}
.tl-dot.t-anniversary { background: #D88593; }
.tl-dot.t-diary { background: #5B8DEF; }
.tl-dot.t-wish { background: #44B57B; }
.tl-dot.t-conflict { background: #E0A458; }
.tl-card { padding: 14px 16px; }
.tl-head { display: flex; align-items: center; justify-content: space-between; gap: 8px; margin-bottom: 6px; }
.tl-date { color: var(--color-ink-3); font-size: 12px; }
.tl-title { font-weight: 500; }
.tl-summary { color: var(--color-ink-2); font-size: 13px; margin-top: 4px; }
.tl-summary b { color: var(--color-accent); }
.tl-expired { color: var(--color-ink-3); }

@media (max-width: 767px) {
  .tl-page { flex-direction: column; }
  .tl-filter { width: 100%; position: static; display: flex; align-items: center; gap: 12px; flex-wrap: wrap; }
  .tl-filter-title { margin-bottom: 0; }
  .tl-filter :deep(.n-date-picker) { flex: 1; min-width: 160px; }
  .tl-tip { margin-top: 0; }
}
</style>
