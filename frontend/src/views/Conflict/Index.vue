<template>
  <div class="conflict-page" ref="container">
    <!-- 品牌条 -->
    <div class="brand block">
      <IpIcon name="module_conflict" :size="28" class="brand-icon" alt="矛盾复盘" />
      <h1 class="ind-label">CONFLICT · 矛盾复盘</h1>
      <span class="brand-status"><IndLed color="green" :size="9" /> 记录中</span>
    </div>

    <!-- 统计瓷砖 -->
    <section class="block stats">
      <IndStatCard label="复盘总数" :value="cfStats.total" sub="次摩擦" />
      <IndStatCard label="已和解" :value="cfStats.reconciled" sub="关系更稳" />
      <IndStatCard label="待和解" :value="cfStats.open" sub="好好聊聊" />
    </section>

    <header class="page-head">
      <IndSectionTitle label="复盘记录" :led="true" />
      <n-button type="primary" round class="uvi-glow-border" v-press-bounce @click="openAdd">+ 记录矛盾</n-button>
    </header>

    <IndSkeleton v-if="loading" variant="list" :rows="6" />
    <IndEmpty
      v-else-if="!list.length"
      title="还没有复盘记录"
      desc="把这次的摩擦记下来，好好聊聊，感情会更稳固～"
    />

    <div v-else>
      <div class="cards">
        <TiltCard
          v-for="c in list"
          :key="c.id"
          class="conflict-card-wrap"
        >
        <div
          class="love-card"
          :class="{ reconciled: c.reconcileTime }"
          role="button"
          tabindex="0"
          @click="openDetail(c)"
          @keydown.enter="openDetail(c)"
          @keydown.space.prevent="openDetail(c)"
        >
          <div class="card-top">
            <n-tag :type="levelMap[c.conflictLevel]?.type ?? 'default'" size="small" round>
              {{ levelMap[c.conflictLevel]?.label ?? '未知' }}
            </n-tag>
            <span class="card-time sub-text">{{ fmt(c.occurTime) }}<span v-if="relDays(c.occurTime) > 1" class="cf-rel"> · {{ relTime(c.occurTime) }}</span></span>
          </div>
          <div class="card-summary title-clamp">{{ c.summary }}</div>
          <div v-if="c.reconcileTime" class="card-reconciled sub-text">已和解</div>
        </div>
        </TiltCard>
      </div>
      <IndPager
        mode="more"
        :page="page"
        :page-size="pageSize"
        :total="total"
        :loading="loading"
        :has-more="hasMore"
        @load-more="nextPage"
      />
    </div>

    <!-- 新增 / 编辑 -->
    <LoveSheet v-model="showForm" :title="editing ? '编辑复盘' : '记录一次矛盾'">
      <LoveDateField v-model="form.occurTs" label="发生时间" mode="datetime" />
      <LoveInput
        v-model="form.summary"
        label="矛盾摘要"
        placeholder="一句话记下这次怎么了"
        :invalid="summaryInvalid"
      />
      <LoveSegmented v-model="form.conflictLevel" label="争吵等级" :options="levelOptions" />
      <LoveTextarea v-model="form.myThoughtA" label="A 方想法" placeholder="TA 当时在想什么" />
      <LoveTextarea v-model="form.myThoughtB" label="B 方想法" placeholder="我当时在想什么" />
      <LoveDateField v-model="form.reconcileTs" label="和解时间" mode="datetime" />
      <LoveInput v-model="form.reconcileWay" label="和解方式" placeholder="怎么和好的（可选）" />
      <LoveTextarea v-model="form.reflectA" label="A 方反思" placeholder="我后来怎么想（可选）" />
      <LoveTextarea v-model="form.reflectB" label="B 方反思" placeholder="对方怎么想（可选）" />
      <LoveTextarea v-model="form.ruleConclusion" label="相处约定" placeholder="以后我们约定…（可选）" />
      <template #footer>
        <LoveSaveBar
          :loading="submitting"
          :success="saved"
          cancel-text="取消"
          save-text="保存"
          @cancel="showForm = false"
          @save="submitForm"
        />
      </template>
    </LoveSheet>

    <!-- 详情 -->
    <n-drawer v-model:show="showDetail" :width="detailWidth" placement="right" class="conflict-drawer">
      <n-drawer-content :title="detail?.summary || '矛盾详情'">
        <template v-if="detail">
          <div class="detail-row">
            <span class="k">等级</span>
            <n-tag :type="levelMap[detail.conflictLevel]?.type ?? 'default'" size="small" round>
              {{ levelMap[detail.conflictLevel]?.label ?? '未知' }}
            </n-tag>
          </div>
          <div class="detail-row"><span class="k">发生时间</span><span>{{ fmt(detail.occurTime) }}<span v-if="relDays(detail.occurTime) > 1" class="cf-rel"> · {{ relTime(detail.occurTime) }}</span></span></div>
          <div class="detail-block"><span class="k">A 方想法</span><p>{{ detail.myThoughtA || '—' }}</p></div>
          <div class="detail-block"><span class="k">B 方想法</span><p>{{ detail.myThoughtB || '—' }}</p></div>
          <div class="detail-row">
            <span class="k">和解</span>
            <span v-if="detail.reconcileTime">{{ fmt(detail.reconcileTime) }} · {{ detail.reconcileWay || '—' }}</span>
            <span v-else class="sub-text">尚未和解</span>
          </div>
          <div class="detail-block"><span class="k">A 方反思</span><p>{{ detail.reflectA || '—' }}</p></div>
          <div class="detail-block"><span class="k">B 方反思</span><p>{{ detail.reflectB || '—' }}</p></div>
          <div class="detail-block"><span class="k">相处约定</span><p>{{ detail.ruleConclusion || '—' }}</p></div>

          <div class="detail-actions">
            <n-button v-if="!detail.reconcileTime" type="success" block v-click-burst @click="markReconciled">
              标记和解
            </n-button>
            <n-popconfirm @positive-click="onDelete(detail.id)">
              <template #trigger>
                <n-button block tertiary type="error">删除记录</n-button>
              </template>
              确定删除这条复盘吗？
            </n-popconfirm>
          </div>
        </template>
      </n-drawer-content>
    </n-drawer>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue';
import {
  NButton, NDrawer, NDrawerContent, NTag, NPopconfirm,
} from 'naive-ui';
import { LoveSheet, LoveInput, LoveTextarea, LoveSegmented, LoveDateField, LoveSaveBar } from '@/components/loveform';
import type { ConflictDto, ConflictReq } from '@/types';
import {
  listConflict, getConflict, createConflict, updateConflict, deleteConflict,
} from '@/api/conflict';
import { useNotifyStore } from '@/store/notifyStore';
import { useStaggerEnter } from '@/composables/useAnimation';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndPager from '@/components/industrial/IndPager.vue';
import IndStatCard from '@/components/industrial/IndStatCard.vue';
import IndSectionTitle from '@/components/industrial/IndSectionTitle.vue';
import IndLed from '@/components/industrial/IndLed.vue';
import TiltCard from '@/components/Common/TiltCard.vue';
import IpIcon from '@/components/Common/IpIcon.vue';
import { feedback } from '@/utils/feedback';
import { usePagedList } from '@/composables/usePagedList';
import { isMobile } from '@/composables/useDevice';

const notify = useNotifyStore();
const submitting = ref(false);
const container = ref<HTMLElement>();
const saved = ref(false);
const summaryInvalid = ref(false);

// 统一管理延迟回调（保存后关弹窗），卸载时一次性清理，避免过期定时器
const pendingTimers = new Set<number>();
function later(fn: () => void, ms: number) {
  const id = window.setTimeout(() => { pendingTimers.delete(id); fn(); }, ms);
  pendingTimers.add(id);
}

const { list, page, pageSize, total, loading, hasMore, nextPage, refresh, loadFirst } = usePagedList<ConflictDto>(
  async (p) => {
    const r = await listConflict({ page: p.page, pageSize: p.pageSize });
    return { items: r.items ?? [], total: r.total ?? (r.items?.length ?? 0) };
  },
  { pageSize: 15, mode: 'more' },
);

const levelOptions = [
  { label: '小摩擦', value: 1 },
  { label: '中等争执', value: 2 },
  { label: '严重矛盾', value: 3 },
];
const levelMap: Record<number, { label: string; type: 'success' | 'warning' | 'error' }> = {
  1: { label: '小摩擦', type: 'success' },
  2: { label: '中等争执', type: 'warning' },
  3: { label: '严重矛盾', type: 'error' },
};

// 统计瓷砖：复盘总数 / 已和解 / 待和解
const cfStats = computed(() => {
  const all = list.value;
  const total = all.length;
  const reconciled = all.filter((c) => c.reconcileTime).length;
  return { total, reconciled, open: total - reconciled };
});

function fmt(s: string) {
  const d = new Date(s);
  return `${d.getFullYear()}.${d.getMonth() + 1}.${d.getDate()} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
}
// 矛盾复盘相对时间：fmt 已含完整日期+时分，故仅对 >1 天的旧记录附「N天前」辅助提示（relDays 守卫避免 today/yesterday 冗余）
function relTime(s: string): string {
  const d = new Date(s);
  const now = new Date();
  const diff = now.getTime() - d.getTime();
  const day = 86400000;
  if (diff < 0) return '未来';
  if (diff < day && now.getDate() === d.getDate())
    return `今天 ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
  if (diff < 2 * day) return `昨天 ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
  if (diff < 30 * day) return `${Math.floor(diff / day)} 天前`;
  if (diff < 365 * day) return `${Math.floor(diff / (30 * day))} 个月前`;
  return `${Math.floor(diff / (365 * day))} 年前`;
}
function relDays(s: string): number {
  return (Date.now() - new Date(s).getTime()) / 86400000;
}

// ---- 表单 ----
const showForm = ref(false);
const editing = ref<ConflictDto | null>(null);
const form = reactive<{
  occurTs: number | null;
  summary: string;
  conflictLevel: number;
  myThoughtA?: string;
  myThoughtB?: string;
  reconcileTs: number | null;
  reconcileWay?: string;
  reflectA?: string;
  reflectB?: string;
  ruleConclusion?: string;
}>({
  occurTs: null, summary: '', conflictLevel: 1,
  myThoughtA: undefined, myThoughtB: undefined,
  reconcileTs: null, reconcileWay: undefined,
  reflectA: undefined, reflectB: undefined, ruleConclusion: undefined,
});

function resetForm() {
  Object.assign(form, {
    occurTs: null, summary: '', conflictLevel: 1,
    myThoughtA: undefined, myThoughtB: undefined,
    reconcileTs: null, reconcileWay: undefined,
    reflectA: undefined, reflectB: undefined, ruleConclusion: undefined,
  });
}
function openAdd() {
  editing.value = null;
  resetForm();
  saved.value = false;
  summaryInvalid.value = false;
  showForm.value = true;
}
function buildReq(): ConflictReq {
  return {
    occurTime: form.occurTs ? new Date(form.occurTs).toISOString() : new Date().toISOString(),
    summary: form.summary,
    conflictLevel: form.conflictLevel,
    myThoughtA: form.myThoughtA,
    myThoughtB: form.myThoughtB,
    reconcileTime: form.reconcileTs ? new Date(form.reconcileTs).toISOString() : undefined,
    reconcileWay: form.reconcileWay,
    reflectA: form.reflectA,
    reflectB: form.reflectB,
    ruleConclusion: form.ruleConclusion,
  };
}
async function submitForm() {
  if (!form.summary.trim()) {
    summaryInvalid.value = true;
    feedback.warn('写一句这次怎么了～');
    return;
  }
  submitting.value = true;
  saved.value = false;
  try {
    if (editing.value) await updateConflict(editing.value.id, buildReq());
    else await createConflict(buildReq());
    feedback.saved('复盘');
    saved.value = true;
    later(async () => {
      showForm.value = false;
      await refresh();
    }, 680);
  } finally { submitting.value = false; }
}

// ---- 详情 ----
const showDetail = ref(false);
const detail = ref<ConflictDto | null>(null);
// 抽屉宽度：移动端占满屏宽，避免固定 420px 在窄屏上溢出/截断
const detailWidth = computed(() => (isMobile() ? '100%' : 420));

async function openDetail(c: ConflictDto) {
  detail.value = await getConflict(c.id);
  showDetail.value = true;
}
async function markReconciled() {
  if (!detail.value) return;
  const full = await getConflict(detail.value.id);
  await updateConflict(full.id, {
    occurTime: full.occurTime,
    summary: full.summary,
    conflictLevel: full.conflictLevel,
    myThoughtA: full.myThoughtA,
    myThoughtB: full.myThoughtB,
    reconcileTime: new Date().toISOString(),
    reconcileWay: full.reconcileWay,
    reflectA: full.reflectA,
    reflectB: full.reflectB,
    ruleConclusion: full.ruleConclusion,
  });
  notify.success('已经和好啦');
  showDetail.value = false;
  await refresh();
}
async function onDelete(id: number) {
  await deleteConflict(id);
  feedback.deleted('复盘');
  showDetail.value = false;
  await refresh();
}

useStaggerEnter(container, '.love-card', { stagger: 0.06, y: 14 });
import { useRealtime } from '@/composables/useRealtime';
import { useSyncSettle } from '@/composables/useSyncSettle';
const { onSync } = useRealtime();
onMounted(async () => {
  await loadFirst();
  onSync('conflict', () => refresh());
  // 伴侣新增/和解矛盾时，卡片错落入场
  useSyncSettle('conflict', container, list, '.love-card');
});
onUnmounted(() => {
  pendingTimers.forEach((id) => clearTimeout(id));
  pendingTimers.clear();
});
</script>

<style scoped>
.conflict-page { max-width: 960px; margin: 0 auto; }
.stats { display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; }
.page-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 16px; }
.page-head h1 { font-size: 22px; margin: 0; }
.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 16px; }
.conflict-card-wrap { display: block; transform-style: preserve-3d; }
.card-top { display: flex; align-items: center; justify-content: space-between; gap: 8px; }
.card-time { font-size: 12px; }
.brand-icon { margin-right: 2px; flex: 0 0 auto; }
.cf-rel { color: var(--color-rose); opacity: 0.85; }
.card-summary { margin-top: 8px; font-size: 15px; color: var(--color-ink); }
.card-reconciled { margin-top: 8px; color: var(--color-rose-text); font-size: 13px; }
.love-card.reconciled { border-color: rgba(67, 209, 122, 0.42); box-shadow: 0 0 0 1px rgba(67, 209, 122, 0.28), var(--elev-2); }
.modal-foot { display: flex; justify-content: flex-end; gap: 10px; }

.detail-row { display: flex; align-items: center; gap: 10px; margin: 12px 0; }
.detail-row .k, .detail-block .k { color: var(--color-ink-3); font-size: 13px; }
.detail-block { margin: 14px 0; }
.detail-block p { margin: 6px 0 0; white-space: pre-wrap; line-height: 1.8; }
.detail-actions { display: flex; flex-direction: column; gap: 10px; margin-top: 24px; }

@media (max-width: 767px) {
  .stats { grid-template-columns: 1fr; }
  .cards { grid-template-columns: 1fr; }
}
</style>
