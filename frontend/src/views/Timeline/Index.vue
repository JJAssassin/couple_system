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

      <div class="tl-filter-title tl-filter-sub">按类型筛选</div>
      <div class="tl-chips">
        <button
          v-for="t in TYPE_ORDER"
          :key="t"
          class="tl-chip"
          :class="[`t-${t}`, { active: activeTypes.has(t) }]"
          type="button"
          @click="toggleType(t)"
        >
          <span class="tl-chip-dot"></span>{{ TYPE_META[t].label }}
          <span class="tl-chip-count">{{ typeCounts[t] }}</span>
        </button>
      </div>
      <NButton v-if="activeTypes.size" text size="small" class="tl-chip-clear" @click="activeTypes.clear()">
        清除类型筛选
      </NButton>

      <div class="tl-tip sub-text">共 {{ filteredAll.length }} 条记录</div>
    </aside>

    <!-- 右侧时间轴 -->
    <section class="tl-wrap">
      <header class="page-head">
        <h1>我们的时间轴</h1>
      </header>

      <!-- 统计条 -->
      <div v-if="items.length" class="tl-stats love-card">
        <div class="tl-stat tl-stat-total">
          <span class="tl-stat-num">{{ filteredAll.length }}</span>
          <span class="tl-stat-label">条记录</span>
        </div>
        <div v-for="t in TYPE_ORDER" :key="t" class="tl-stat" :class="`t-${t}`">
          <span class="tl-stat-num">{{ typeCounts[t] }}</span>
          <span class="tl-stat-label">{{ TYPE_META[t].label }}</span>
        </div>
      </div>

      <IndEmpty
        v-if="!filteredItems.length"
        title="这段时间还没有记录"
        desc="去创造更多回忆吧，把重要时刻都记下来～"
      />

      <template v-else>
        <div v-for="g in groups" :key="g.year" class="tl-group">
          <div class="tl-year">{{ g.year }}</div>
          <div class="tl">
            <span class="tl-line"></span>
            <div
              v-for="(item, i) in g.items"
              :key="item.id"
              class="tl-item"
              :style="{ animationDelay: i * 0.05 + 's' }"
            >
              <span class="tl-dot" :class="`t-${item.type}`"></span>
              <div class="love-card tl-card">
                <div class="tl-head">
                  <NTag :type="tagType(item.type)" size="small">{{ typeLabel(item.type) }}</NTag>
                  <NTag v-if="item.type === 'anniversary' && item.isYearly" size="small" type="primary" round>每年</NTag>
                  <span class="tl-date">
                    {{ formatDate(item.date) }}
                    <span class="tl-rel">{{ relativeTime(item.date) }}</span>
                  </span>
                </div>
                <div class="tl-title">{{ item.title }}</div>
                <div v-if="item.type === 'anniversary' && item.nextOccurrence" class="tl-summary">
                  下次 {{ formatDate(item.nextOccurrence) }} · 还有 <b>{{ daysUntil(item.nextOccurrence) }}</b> 天
                </div>
                <div v-else-if="item.type === 'anniversary' && !item.nextOccurrence" class="tl-summary tl-expired">
                  这一天已经过去啦
                </div>
                <template v-else-if="item.summary">
                  <div class="tl-summary" :class="{ 'title-clamp': !expanded.has(item.id) }">{{ item.summary }}</div>
                  <button
                    v-if="item.summary.length > 40"
                    class="tl-expand"
                    type="button"
                    @click="toggleExpand(item.id)"
                  >
                    {{ expanded.has(item.id) ? '收起' : '展开' }}
                  </button>
                </template>
              </div>
            </div>
          </div>
        </div>

        <IndPager
          v-if="filteredItems.length"
          mode="more"
          :page="1"
          :page-size="15"
          :total="filteredItems.length"
          :loading="false"
          :has-more="hasMore"
          @load-more="onLoadMore"
        />
      </template>
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
const activeTypes = ref(new Set<string>());
const expanded = ref(new Set<number>());

const TYPE_ORDER = ['anniversary', 'diary', 'wish', 'conflict'] as const;

const filteredAll = computed(() => {
  const arr = activeTypes.value.size
    ? items.value.filter((x) => activeTypes.value.has(x.type))
    : items.value;
  return arr;
});

const filteredItems = computed(() => filteredAll.value.slice(0, shown.value));

const groups = computed(() => {
  const map = new Map<number, TimelineItemDto[]>();
  for (const it of filteredItems.value) {
    const y = new Date(it.date).getFullYear();
    if (!map.has(y)) map.set(y, []);
    map.get(y)!.push(it);
  }
  return [...map.entries()]
    .sort((a, b) => b[0] - a[0])
    .map(([year, list]) => ({ year, items: list }));
});

const hasMore = computed(() => {
  const total = activeTypes.value.size
    ? items.value.filter((x) => activeTypes.value.has(x.type)).length
    : items.value.length;
  return total > shown.value;
});

const typeCounts = computed(() => {
  const c: Record<string, number> = { anniversary: 0, diary: 0, wish: 0, conflict: 0 };
  for (const x of items.value) if (c[x.type] !== undefined) c[x.type]++;
  return c;
});

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
    activeTypes.value.clear();
    expanded.value.clear();
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

function toggleType(t: string) {
  const s = activeTypes.value;
  if (s.has(t)) s.delete(t);
  else s.add(t);
  activeTypes.value = new Set(s); // 触发响应式
  shown.value = 15;
}

function toggleExpand(id: number) {
  const s = expanded.value;
  if (s.has(id)) s.delete(id);
  else s.add(id);
  expanded.value = new Set(s);
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
function relativeTime(s: string) {
  const diff = Date.now() - new Date(s).getTime();
  const day = 86400000;
  if (diff < 0) return '';
  if (diff < day) return '今天';
  if (diff < 2 * day) return '昨天';
  if (diff < 7 * day) return `${Math.floor(diff / day)} 天前`;
  if (diff < 30 * day) return `${Math.floor(diff / (7 * day))} 周前`;
  if (diff < 365 * day) return `${Math.floor(diff / (30 * day))} 个月前`;
  const years = Math.floor(diff / (365 * day));
  return `${years} 年前`;
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
.tl-filter-sub { margin-top: 18px; font-size: 13px; color: var(--color-ink-2); }
.tl-tip { margin-top: 12px; }

.tl-chips { display: flex; flex-direction: column; gap: 8px; }
.tl-chip {
  display: flex; align-items: center; gap: 8px; width: 100%;
  padding: 7px 10px; border-radius: 10px; cursor: pointer; font-size: 13px;
  background: var(--color-surface-2, #f7f3f1); border: 1px solid var(--color-border); color: var(--color-ink-2);
  transition: all 0.18s var(--ease-love);
}
.tl-chip:hover { border-color: var(--color-rose); }
.tl-chip.active { background: var(--color-rose-tint, #fbeef0); border-color: var(--color-rose); color: var(--color-ink-1); font-weight: 500; }
.tl-chip-dot { width: 9px; height: 9px; border-radius: 50%; flex-shrink: 0; }
.tl-chip.t-anniversary .tl-chip-dot { background: #D88593; }
.tl-chip.t-diary .tl-chip-dot { background: #5B8DEF; }
.tl-chip.t-wish .tl-chip-dot { background: #44B57B; }
.tl-chip.t-conflict .tl-chip-dot { background: #E0A458; }
.tl-chip-count { margin-left: auto; font-size: 12px; color: var(--color-ink-3); }
.tl-chip-clear { margin-top: 8px; }

.tl-wrap { flex: 1; min-width: 0; overflow-x: clip; }
.tl-stats {
  display: flex; align-items: center; gap: 6px 10px; flex-wrap: wrap;
  padding: 12px 14px; margin-bottom: 18px;
}
.tl-stat { display: flex; flex-direction: column; align-items: center; min-width: 0; flex: 1 1 auto; padding: 0 4px; }
.tl-stat-num { font-size: 20px; font-weight: 600; line-height: 1.1; }
.tl-stat-label { font-size: 11px; color: var(--color-ink-3); margin-top: 2px; }
.tl-stat-total .tl-stat-num { color: var(--color-accent); }
.tl-stat.t-anniversary .tl-stat-num { color: #D88593; }
.tl-stat.t-diary .tl-stat-num { color: #5B8DEF; }
.tl-stat.t-wish .tl-stat-num { color: #44B57B; }
.tl-stat.t-conflict .tl-stat-num { color: #E0A458; }

.tl-group { position: relative; }
.tl-year {
  position: sticky; top: 60px; z-index: 5;
  font-size: 15px; font-weight: 600; color: var(--color-ink-1);
  background: var(--color-bg, #fff); padding: 6px 0; margin: 4px 0 14px;
  border-bottom: 1px solid var(--color-border);
}
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
.tl-rel { margin-left: 6px; color: var(--color-accent); }
.tl-title { font-weight: 500; }
.tl-summary { color: var(--color-ink-2); font-size: 13px; margin-top: 4px; }
.tl-summary b { color: var(--color-accent); }
.tl-expired { color: var(--color-ink-3); }
.tl-expand {
  margin-top: 6px; background: none; border: none; color: var(--color-rose);
  font-size: 12px; cursor: pointer; padding: 0;
}

@media (max-width: 767px) {
  .tl-page { flex-direction: column; width: 100%; max-width: 100%; }
  .tl-wrap { width: 100%; max-width: 100%; }
.page-head { display: flex; align-items: center; margin-bottom: 16px; }
.page-head h1 { font-size: 22px; margin: 0; }
  .tl-filter { width: 100%; position: static; display: flex; align-items: center; gap: 12px; flex-wrap: wrap; }
  .tl-filter-title { margin-bottom: 0; }
  .tl-filter-sub { margin-top: 0; width: 100%; }
  .tl-chips { flex-direction: row; flex-wrap: wrap; width: 100%; }
  .tl-chip { width: auto; flex: 1 1 auto; }
  .tl-chip-count { margin-left: 4px; }
  .tl-filter :deep(.n-date-picker) { flex: 1; min-width: 160px; }
  .tl-tip { margin-top: 0; }
  .tl-stat { flex: 1 1 56px; }
  .tl-stat-num { font-size: 17px; }
}
</style>
