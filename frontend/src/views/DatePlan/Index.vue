<template>
  <IndSkeleton v-if="loading" variant="grid" :rows="4" :columns="2" />
  <div v-else class="dateplan" ref="container">
    <!-- 品牌条 -->
    <div class="brand block">
      <IpIcon name="module_dateplan" :size="28" class="brand-icon" alt="约会" />
      <h1 class="ind-label">DATE PLAN · 约会计划</h1>
      <span class="brand-status"><IndLed color="green" :size="9" /> 记录中</span>
    </div>

    <!-- 统计瓷砖 -->
    <section class="block stats">
      <IndStatCard label="已完成约会" :value="stats.totalDates" sub="次浪漫回忆" />
      <IndStatCard label="平均体验分" :value="stats.avgScore.toFixed(1)" sub="满分 5★" />
      <IndStatCard label="待执行" :value="pending.length" :sub="pendingSub" />
    </section>

    <!-- 待执行 -->
    <section class="block">
      <div class="block-head">
        <IndSectionTitle label="待执行约会" :led="true" />
        <NButton type="primary" size="small" class="uvi-glow-border" v-press-bounce @click="openCreate">+ 加约会</NButton>
      </div>
      <div v-if="pending.length" class="cards">
        <TiltCard v-for="d in pending" :key="d.id" class="dateplan-card-wrap">
        <div class="love-card card" :class="{ removing: removingId === d.id }">
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
          <div class="card-badges" v-if="isDueSoon(d) || isOverdue(d)">
            <span v-if="isDueSoon(d)" class="date-due-soon">{{ dueText(d) }}</span>
            <span v-if="isOverdue(d)" class="date-overdue" :class="'lv' + overdueLevel(d)">已过期 {{ overdueDays(d) }} 天</span>
          </div>
          <div class="card-loc">{{ d.location || '地点待定' }}</div>
          <div class="sub-text">预算 ¥{{ (d.budget ?? 0).toFixed(2) }}</div>
          <NButton block type="primary" class="card-btn" v-press-bounce @click="openComplete(d)">标记完成</NButton>
        </div>
        </TiltCard>
      </div>
      <IndEmpty v-else title="暂无待执行的约会" desc="去计划一次浪漫的约会吧，给彼此一个小期待" />
    </section>

    <!-- 历史 -->
    <section class="block">
      <IndSectionTitle label="约会回忆" :led="true" />
      <div v-if="history.length" class="cards">
        <TiltCard v-for="d in history" :key="d.id" class="dateplan-card-wrap">
        <div class="love-card card" :class="{ removing: removingId === d.id }">
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
        </TiltCard>
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
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndPager from '@/components/industrial/IndPager.vue';
import IndStatCard from '@/components/industrial/IndStatCard.vue';
import IndSectionTitle from '@/components/industrial/IndSectionTitle.vue';
import IndLed from '@/components/industrial/IndLed.vue';
import IpIcon from '@/components/Common/IpIcon.vue';
import TiltCard from '@/components/Common/TiltCard.vue';
import { useStaggerEnter } from '@/composables/useAnimation';
import { usePagedList } from '@/composables/usePagedList';
import { useRealtime } from '@/composables/useRealtime';
import { useSyncSettle } from '@/composables/useSyncSettle';
import { feedback } from '@/utils/feedback';
import { toLocalISO } from '@/utils/format';

const loading = ref(true);
const stats = ref<dp.DateStats>({ totalDates: 0, avgScore: 0 });
const container = ref<HTMLElement>();
const message = useMessage();
const saving = ref(false);
const created = ref(false);
const completed = ref(false);
const removingId = ref<number | null>(null);
useStaggerEnter(container, '.block', { stagger: 0.1, y: 16 });

const { list, page, pageSize, total, loading: listLoading, hasMore, refresh: refreshList, nextPage } = usePagedList<DateRecordDto>(
  async (p) => {
    const d = await dp.listDate({ page: p.page, pageSize: p.pageSize });
    return { items: d.items, total: d.total };
  },
  { pageSize: 15, mode: 'more' }
);
const pending = computed(() => {
  const arr = list.value.filter((d) => !d.isCompleted);
  // 逾期置顶 → 临近 → 其余；稳定排序，组内保留后端分页顺序（与 Todo/Wish 一致）
  const grp = (d: DateRecordDto) => (isOverdue(d) ? 0 : isDueSoon(d) ? 1 : 2);
  return [...arr].sort((a, b) => grp(a) - grp(b));
});
const history = computed(() => list.value.filter((d) => d.isCompleted));

const fmtTime = (iso?: string) => (iso ? new Date(iso).toLocaleString('zh-CN') : '时间待定');

// 计划时间预警（纯前端、零后端改动，与 Todo/Wish 逾期语言一致）：
// 未完成且已过计划时间 → 已过期；0~3 天内 → 临近。
function daysUntil(d: DateRecordDto) {
  if (!d.planTime) return Infinity;
  return Math.round((new Date(d.planTime).getTime() - Date.now()) / 86_400_000);
}
function isOverdue(d: DateRecordDto) {
  if (d.isCompleted || !d.planTime) return false;
  return daysUntil(d) < 0;
}
function overdueDays(d: DateRecordDto) {
  return Math.max(1, -daysUntil(d));
}
function isDueSoon(d: DateRecordDto) {
  if (d.isCompleted || !d.planTime) return false;
  const x = daysUntil(d);
  return x >= 0 && x <= 3;
}
function dueText(d: DateRecordDto) {
  const x = daysUntil(d);
  if (x <= 0) return '今天约会';
  if (x === 1) return '明天约会';
  return `${x} 天后约会`;
}
// 逾期时长分级：1=≤3天(柔和) 2=≤14天(标准) 3=>14天(加重)
function overdueLevel(d: DateRecordDto) {
  const x = overdueDays(d);
  if (x <= 3) return 1;
  if (x <= 14) return 2;
  return 3;
}
const overdueCount = computed(() => pending.value.filter(isOverdue).length);
const dueSoonCount = computed(() => pending.value.filter(isDueSoon).length);
// 待执行统计副标题：有逾期/临近时给出醒目提示
const pendingSub = computed(() => {
  if (overdueCount.value) return `${overdueCount.value} 个已过期`;
  if (dueSoonCount.value) return `${dueSoonCount.value} 个临近`;
  return '个小期待';
});

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
  // 先播收缩动画，再删库并刷新，避免瞬间消失（对标纪念日页删除 pop）
  removingId.value = d.id;
  later(async () => {
    try {
      await dp.deleteDate(d.id);
      feedback.deleted('这段约会');
      await refresh();
    } finally {
      removingId.value = null;
    }
  }, 320);
}

onMounted(async () => {
  try { await refresh(); } finally { loading.value = false; }
  const { onSync } = useRealtime();
  // 伴侣新增/完成/删除约会时，整表刷新并让卡片错落入场
  // 注意：后端 CoupleDateRecord 广播模块名为 "date"（Entities.cs [Broadcast("date")]），须与之一致
  onSync('date', () => refresh());
  useSyncSettle('date', container, list, '.love-card');
});
</script>

<style scoped>
.dateplan { max-width: 880px; margin: 0 auto; }
.brand-icon { margin-right: 2px; flex: 0 0 auto; }
.stats { display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; }
/* 计划时间预警角标（与 Todo/Wish 一致：玫瑰逾期 / 琥珀临近） */
.card-badges { display: flex; flex-wrap: wrap; gap: 6px; }
.date-due-soon {
  color: #fff; background: linear-gradient(135deg, #E8B06A 0%, #D98E3C 100%);
  padding: 2px 9px; border-radius: 999px; font-size: 12px; font-weight: 600; align-self: flex-start;
}
.date-overdue {
  color: #fff; background: linear-gradient(135deg, var(--color-rose) 0%, var(--color-rose-vivid) 100%);
  padding: 2px 9px; border-radius: 999px; font-size: 12px; font-weight: 600; align-self: flex-start;
}
.date-overdue.lv1 { background: linear-gradient(135deg, var(--color-rose-soft) 0%, var(--color-rose) 100%); }
.date-overdue.lv3 { background: linear-gradient(135deg, var(--color-rose-vivid) 0%, #E0394F 100%); }
.block { margin: 22px 0; }
.block h2 { font-size: 16px; margin: 0 0 12px; }
.page-head { margin-bottom: 4px; }
.page-head h1 { font-size: 22px; margin: 0; }
.block-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px; }
.block-head h2 { margin: 0; }
.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 12px; }
.card { display: flex; flex-direction: column; gap: 6px; }
.dateplan-card-wrap { display: block; transform-style: preserve-3d; }
.card-top { display: flex; align-items: center; justify-content: space-between; }
.card-plan { font-weight: 600; }
.card-loc { color: var(--color-cocoa); }
.card-btn { margin-top: 6px; }
.love-card.removing { animation: cardRemove 0.32s var(--ease-love) forwards; pointer-events: none; }
@keyframes cardRemove {
  0% { transform: scale(1); opacity: 1; }
  100% { transform: scale(0.9); opacity: 0; }
}
.reduce-motion .love-card.removing { animation: none; }
.score { display: flex; align-items: center; gap: 8px; }
@media (max-width: 767px) {
  .stats { grid-template-columns: 1fr; }
  .cards { grid-template-columns: 1fr; }
}

.dp-field { display: flex; flex-direction: column; gap: 8px; }
.dp-label { font-size: 13px; font-weight: 500; color: var(--color-ink-2); padding-left: 2px; }
</style>
