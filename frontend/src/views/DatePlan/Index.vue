<template>
  <IndSkeleton v-if="loading" variant="grid" :rows="4" :columns="2" />
  <div v-else class="dateplan" ref="container">
    <header class="page-head">
      <h1>约会计划</h1>
    </header>

    <!-- 统计 -->
    <section class="hero">
      <div class="stat">
        <div class="stat-v"><LoveCount :value="stats.totalDates" /> <span>次</span></div>
        <div class="stat-k">已完成约会</div>
      </div>
      <div class="stat">
        <div class="stat-v">{{ stats.avgScore.toFixed(1) }} <span>★</span></div>
        <div class="stat-k">平均体验分</div>
      </div>
      <div class="stat">
        <div class="stat-v">{{ pending.length }} <span>个</span></div>
        <div class="stat-k">待执行</div>
      </div>
    </section>

    <!-- 待执行 -->
    <section class="block">
      <div class="block-head">
        <h2>待执行约会</h2>
        <NButton type="primary" size="small" v-press-bounce @click="openCreate">+ 加约会</NButton>
      </div>
      <div v-if="pending.length" class="cards">
        <div v-for="d in pending" :key="d.id" class="love-card card">
          <div class="card-top">
            <NTag size="small" type="info">计划中</NTag>
            <NPopconfirm @positive-click="remove(d)">
              <template #trigger>
                <NButton size="small" quaternary type="error">删</NButton>
              </template>
              确认删除？
            </NPopconfirm>
          </div>
          <div class="card-plan">{{ fmtTime(d.planTime) }}</div>
          <div class="card-loc">{{ d.location || '地点待定' }}</div>
          <div class="sub-text">预算 ¥{{ (d.budget ?? 0).toFixed(2) }}</div>
          <NButton block type="primary" class="card-btn" v-press-bounce @click="openComplete(d)">标记完成</NButton>
        </div>
      </div>
      <IndEmpty v-else title="暂无待执行的约会" desc="去计划一次浪漫的约会吧，给彼此一个小期待" />
    </section>

    <!-- 历史 -->
    <section class="block">
      <h2>约会回忆</h2>
      <div v-if="history.length" class="cards">
        <div v-for="d in history" :key="d.id" class="love-card card">
          <div class="card-top">
            <NTag size="small" type="success">已完成</NTag>
            <NPopconfirm @positive-click="remove(d)">
              <template #trigger>
                <NButton size="small" quaternary type="error">删</NButton>
              </template>
              确认删除？
            </NPopconfirm>
          </div>
          <div class="card-plan">{{ fmtTime(d.realTime || d.planTime) }}</div>
          <div class="card-loc">{{ d.location || '地点待定' }}</div>
          <div class="sub-text">实际花费 ¥{{ (d.realCost ?? 0).toFixed(2) }}</div>
          <div class="score"><NRate :value="d.experienceScore ?? 0" readonly size="small" /> <span class="sub-text">{{ d.experienceScore ?? 0 }} 分</span></div>
        </div>
      </div>
      <IndEmpty v-else title="还没有完成的约会" desc="完成一次约会后，回忆会在这里温柔存放" />
    </section>

    <IndPager v-if="list.length" mode="more" :page="page" :page-size="pageSize" :loading="listLoading" :has-more="hasMore" :total="total" @load-more="nextPage" />

    <!-- 新建 -->
    <LoveSheet v-model="showCreate" title="加约会">
      <LoveDateField v-model="cform.planTime" label="计划时间" mode="datetime" />
      <LoveInput v-model="cform.location" label="地点" placeholder="如：海边餐厅" />
      <LoveInput v-model="cform.budget" label="预算（¥）" type="number" inputmode="decimal" placeholder="可选" />
      <LoveTextarea v-model="cform.remark" label="备注" placeholder="可选" />
      <template #footer>
        <LoveSaveBar :loading="saving" :success="created" cancel-text="取消" save-text="保存" @cancel="showCreate = false" @save="saveCreate" />
      </template>
    </LoveSheet>

    <!-- 完成 -->
    <LoveSheet v-model="showComplete" title="完成约会">
      <LoveInput v-model="completeForm.realCost" label="实际花费（¥）" type="number" inputmode="decimal" placeholder="可选" />
      <div class="dp-field">
        <span class="dp-label">体验评分</span>
        <n-rate v-model:value="completeForm.score" aria-label="体验评分" />
      </div>
      <template #footer>
        <LoveSaveBar :loading="saving" :success="completed" cancel-text="取消" save-text="完成啦" @cancel="showComplete = false" @save="saveComplete" />
      </template>
    </LoveSheet>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, onUnmounted } from 'vue';
import {
  NButton, NRate, NTag, NPopconfirm, useMessage,
} from 'naive-ui';
import type { DateRecordDto } from '@/types';
import * as dp from '@/api/dateplan';
import { LoveSheet, LoveInput, LoveTextarea, LoveDateField, LoveSaveBar } from '@/components/loveform';
import LoveCount from '@/components/Common/LoveCount.vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndPager from '@/components/industrial/IndPager.vue';
import { useStaggerEnter } from '@/composables/useAnimation';
import { usePagedList } from '@/composables/usePagedList';
import { feedback } from '@/utils/feedback';
import { toLocalISO } from '@/utils/format';

const loading = ref(true);
const stats = ref<dp.DateStats>({ totalDates: 0, avgScore: 0 });
const container = ref<HTMLElement>();
const message = useMessage();
const saving = ref(false);
const created = ref(false);
const completed = ref(false);
useStaggerEnter(container, '.block', { stagger: 0.1, y: 16 });

const { list, page, pageSize, total, loading: listLoading, hasMore, refresh: refreshList, nextPage } = usePagedList<DateRecordDto>(
  async (p) => {
    const d = await dp.listDate({ page: p.page, pageSize: p.pageSize });
    return { items: d.items, total: d.total };
  },
  { pageSize: 15, mode: 'more' }
);
const pending = computed(() => list.value.filter((d) => !d.isCompleted));
const history = computed(() => list.value.filter((d) => d.isCompleted));

const fmtTime = (iso?: string) => (iso ? new Date(iso).toLocaleString('zh-CN') : '时间待定');

// 统一延时器管理：避免组件卸载后 setTimeout 仍回调（跨页已确认的真实泄漏模式）
const pendingTimers = new Set<number>();
function later(fn: () => void, ms: number) {
  const id = window.setTimeout(() => { pendingTimers.delete(id); fn(); }, ms);
  pendingTimers.add(id);
}
onUnmounted(() => {
  pendingTimers.forEach((id) => clearTimeout(id));
  pendingTimers.clear();
});

async function loadStats() {
  stats.value = await dp.dateStats();
}
async function refresh() {
  await Promise.all([loadStats(), refreshList()]);
}

function toIso(ts: number | null) { return toLocalISO(ts); }

const showCreate = ref(false);
const cform = ref<{ planTime: number | null; location: string; budget: string; remark?: string }>({
  planTime: Date.now(), location: '', budget: '', remark: '',
});
function openCreate() {
  cform.value = { planTime: Date.now(), location: '', budget: '', remark: '' };
  created.value = false;
  showCreate.value = true;
}
async function saveCreate() {
  if (!cform.value.planTime) {
    feedback.warn('请选择计划时间');
    return;
  }
  saving.value = true;
  created.value = false;
  try {
    await dp.createDate({
      isCompleted: false,
      planTime: toIso(cform.value.planTime),
      location: cform.value.location,
      budget: cform.value.budget ? Number(cform.value.budget) : undefined,
      remark: cform.value.remark,
    });
    created.value = true;
    feedback.created('约会');
    later(async () => {
      showCreate.value = false;
      await refresh();
    }, 680);
  } finally {
    saving.value = false;
  }
}

const showComplete = ref(false);
const active = ref<DateRecordDto | null>(null);
const completeForm = ref<{ realCost: string; score: number }>({ realCost: '', score: 5 });
function openComplete(d: DateRecordDto) {
  active.value = d;
  completeForm.value = { realCost: d.realCost != null ? String(d.realCost) : '', score: d.experienceScore ?? 5 };
  completed.value = false;
  showComplete.value = true;
}
async function saveComplete() {
  if (!active.value) return;
  saving.value = true;
  completed.value = false;
  try {
    await dp.updateDate(active.value.id, {
      isCompleted: true,
      planTime: active.value.planTime,
      location: active.value.location,
      budget: active.value.budget,
      realTime: toLocalISO(Date.now()),
      realCost: completeForm.value.realCost ? Number(completeForm.value.realCost) : undefined,
      experienceScore: completeForm.value.score,
      remark: active.value.remark,
    });
    completed.value = true;
    message.success('约会完成');
    later(async () => {
      showComplete.value = false;
      await refresh();
    }, 680);
  } finally {
    saving.value = false;
  }
}

async function remove(d: DateRecordDto) {
  await dp.deleteDate(d.id);
  feedback.deleted('这段约会');
  await refresh();
}

onMounted(async () => {
  try { await refresh(); } finally { loading.value = false; }
});
</script>

<style scoped>
.dateplan { max-width: 880px; margin: 0 auto; }
.hero { display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; text-align: center; padding: 24px 0 8px; }
.stat-v { font-size: 34px; font-weight: 800; color: var(--color-rose-text); display: flex; align-items: baseline; justify-content: center; gap: 4px; }
.stat-v span { font-size: 15px; color: var(--color-ink-3); }
.stat-k { color: var(--color-ink-3); font-size: 13px; }
.block { margin: 22px 0; }
.block h2 { font-size: 16px; margin: 0 0 12px; }
.page-head { margin-bottom: 4px; }
.page-head h1 { font-size: 22px; margin: 0; }
.block-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px; }
.block-head h2 { margin: 0; }
.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 12px; }
.card { display: flex; flex-direction: column; gap: 6px; }
.card-top { display: flex; align-items: center; justify-content: space-between; }
.card-plan { font-weight: 600; }
.card-loc { color: var(--color-cocoa); }
.card-btn { margin-top: 6px; }
.score { display: flex; align-items: center; gap: 8px; }
@media (max-width: 767px) {
  .cards { grid-template-columns: 1fr; }
}

.dp-field { display: flex; flex-direction: column; gap: 8px; }
.dp-label { font-size: 13px; font-weight: 500; color: var(--color-ink-2); padding-left: 2px; }
</style>
