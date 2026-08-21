<template>
  <IndSkeleton v-if="loading" variant="grid" :rows="4" :columns="2" />
  <div v-else class="dateplan" ref="container">
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
        <NButton type="primary" size="small" @click="openCreate">+ 加约会</NButton>
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
          <NButton block type="primary" class="card-btn" @click="openComplete(d)">标记完成</NButton>
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
    <NModal v-model:show="showCreate" title="加约会" preset="card" style="max-width: 440px" class="dateplan-modal create-modal">
      <NForm ref="formRef" :model="cform" :rules="cRules" class="dateplan-form">
        <NFormItem label="计划时间" path="planTime" class="dateplan-form-item">
          <NDatePicker v-model:value="cform.planTime" type="datetime" style="width: 100%" class="dateplan-picker" />
        </NFormItem>
        <NFormItem label="地点" class="dateplan-form-item">
          <NInput v-model:value="cform.location" placeholder="如：海边餐厅" class="dateplan-input" />
        </NFormItem>
        <NFormItem label="预算" class="dateplan-form-item">
          <NInputNumber v-model:value="cform.budget" :min="0" :precision="2" style="width: 100%" class="dateplan-input" />
        </NFormItem>
        <NFormItem label="备注" class="dateplan-form-item">
          <NInput v-model:value="cform.remark" type="textarea" placeholder="可选" class="dateplan-textarea" />
        </NFormItem>
      </NForm>
      <template #footer>
        <div class="dateplan-foot">
          <NButton class="dateplan-btn-cancel" @click="showCreate = false">取消</NButton>
          <NButton type="primary" :loading="saving" @click="saveCreate" class="dateplan-btn-primary">保存</NButton>
        </div>
      </template>
    </NModal>

    <!-- 完成 -->
    <NModal v-model:show="showComplete" title="完成约会" preset="card" style="max-width: 440px" class="dateplan-modal complete-modal">
      <NForm :model="completeForm" v-if="active" class="dateplan-form">
        <NFormItem label="实际花费" class="dateplan-form-item">
          <NInputNumber v-model:value="completeForm.realCost" :min="0" :precision="2" style="width: 100%" class="dateplan-input" />
        </NFormItem>
        <NFormItem label="体验评分" class="dateplan-form-item">
          <NRate v-model:value="completeForm.score" class="dateplan-rate" />
        </NFormItem>
      </NForm>
      <template #footer>
        <div class="dateplan-foot">
          <NButton class="dateplan-btn-cancel" @click="showComplete = false">取消</NButton>
          <NButton type="success" :loading="saving" @click="saveComplete" class="dateplan-btn-success">完成啦</NButton>
        </div>
      </template>
    </NModal>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import {
  NButton, NModal, NForm, NFormItem, NInput, NInputNumber, NDatePicker, NRate, NTag, NPopconfirm, useMessage,
} from 'naive-ui';
import type { FormItemRule } from 'naive-ui';
import type { DateRecordDto } from '@/types';
import * as dp from '@/api/dateplan';
import LoveCount from '@/components/Common/LoveCount.vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndPager from '@/components/industrial/IndPager.vue';
import { useStaggerEnter } from '@/composables/useAnimation';
import { usePagedList } from '@/composables/usePagedList';
import { feedback } from '@/utils/feedback';
import { dateRule } from '@/utils/formRules';

const loading = ref(true);
const stats = ref<dp.DateStats>({ totalDates: 0, avgScore: 0 });
const container = ref<HTMLElement>();
const message = useMessage();
const saving = ref(false);
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

const formRef = ref<InstanceType<typeof NForm>>();
const cRules = { planTime: [dateRule('请选择计划时间')] };

const fmtTime = (iso?: string) => (iso ? new Date(iso).toLocaleString('zh-CN') : '时间待定');

async function loadStats() {
  stats.value = await dp.dateStats();
}
async function refresh() {
  await Promise.all([loadStats(), refreshList()]);
}

function toIso(ts: number | null) { return ts ? new Date(ts).toISOString() : undefined; }

const showCreate = ref(false);
const cform = ref<{ planTime: number | null; location: string; budget: number | null; remark?: string }>({
  planTime: Date.now(), location: '', budget: null, remark: '',
});
function openCreate() {
  cform.value = { planTime: Date.now(), location: '', budget: null, remark: '' };
  showCreate.value = true;
}
async function saveCreate() {
  try {
    await formRef.value?.validate();
  } catch {
    return;
  }
  saving.value = true;
  try {
    await dp.createDate({
      isCompleted: false,
      planTime: toIso(cform.value.planTime),
      location: cform.value.location,
      budget: cform.value.budget ?? undefined,
      remark: cform.value.remark,
    });
    showCreate.value = false;
    feedback.created('约会');
    await refresh();
  } finally {
    saving.value = false;
  }
}

const showComplete = ref(false);
const active = ref<DateRecordDto | null>(null);
const completeForm = ref<{ realCost: number | null; score: number }>({ realCost: null, score: 5 });
function openComplete(d: DateRecordDto) {
  active.value = d;
  completeForm.value = { realCost: d.realCost ?? null, score: d.experienceScore ?? 5 };
  showComplete.value = true;
}
async function saveComplete() {
  if (!active.value) return;
  saving.value = true;
  try {
    await dp.updateDate(active.value.id, {
      isCompleted: true,
      planTime: active.value.planTime,
      location: active.value.location,
      budget: active.value.budget,
      realTime: new Date().toISOString(),
      realCost: completeForm.value.realCost ?? undefined,
      experienceScore: completeForm.value.score,
      remark: active.value.remark,
    });
    showComplete.value = false;
    message.success('约会完成');
    await refresh();
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
.stat-v { font-size: 34px; font-weight: 600; color: var(--color-rose); display: flex; align-items: baseline; justify-content: center; gap: 4px; }
.stat-v span { font-size: 15px; color: var(--color-ink-3); }
.stat-k { color: var(--color-ink-3); font-size: 13px; }
.block { margin: 22px 0; }
.block h2 { font-size: 16px; margin: 0 0 12px; }
.block-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px; }
.block-head h2 { margin: 0; }
.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 12px; }
.card { display: flex; flex-direction: column; gap: 6px; }
.card-top { display: flex; align-items: center; justify-content: space-between; }
.card-plan { font-weight: 600; }
.card-loc { color: var(--color-cocoa); }
.card-btn { margin-top: 6px; }
.score { display: flex; align-items: center; gap: 8px; }
.modal-foot { display: flex; justify-content: flex-end; gap: 10px; }
@media (max-width: 767px) {
  .cards { grid-template-columns: 1fr; }
}

/* 美化约会模态框 */
:global(.dateplan-modal) {
  border-radius: 16px !important;
  overflow: hidden;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.12) !important;
}
:global(.dateplan-modal .n-modal-header) {
  background: linear-gradient(135deg, #fdf2f8, var(--color-surface)) !important;
  padding: 18px 24px !important;
  border-bottom: 1px solid var(--color-border);
}
:global(.dateplan-modal .n-modal-header .n-modal-header__close) {
  top: 16px;
  right: 16px;
}
:global(.dateplan-modal .n-modal-body) {
  padding: 24px !important;
}
:global(.dateplan-modal .n-modal-footer) {
  padding: 16px 24px !important;
  border-top: 1px solid var(--color-border);
  background: var(--color-surface);
}
.dateplan-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.dateplan-form-item {
  margin-bottom: 0 !important;
}
.dateplan-input,
.dateplan-textarea,
.dateplan-picker {
  border-radius: 10px !important;
}
.dateplan-textarea :deep(.n-input__textarea),
.dateplan-textarea :deep(textarea) {
  font-size: 15px;
  line-height: 1.7;
  padding: 12px 14px;
  border-radius: 10px;
}
.dateplan-foot {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}
.dateplan-btn-cancel {
  border-radius: 10px;
  padding: 8px 20px;
  font-weight: 500;
}
.dateplan-btn-primary {
  border-radius: 10px;
  padding: 8px 24px;
  font-weight: 600;
  background: linear-gradient(135deg, var(--color-rose), var(--color-rose-deep));
  border: none;
  box-shadow: 0 4px 12px rgba(255, 111, 125, 0.25);
  transition: all var(--dur-micro) var(--ease-love);
}
.dateplan-btn-primary:hover {
  box-shadow: 0 6px 16px rgba(255, 111, 125, 0.35);
  transform: translateY(-1px);
}
.dateplan-btn-success {
  border-radius: 10px;
  padding: 8px 24px;
  font-weight: 600;
  background: linear-gradient(135deg, #52c41a, #389e0d);
  border: none;
  box-shadow: 0 4px 12px rgba(82, 196, 26, 0.25);
  transition: all var(--dur-micro) var(--ease-love);
}
.dateplan-btn-success:hover {
  box-shadow: 0 6px 16px rgba(82, 196, 26, 0.35);
  transform: translateY(-1px);
}

@media (max-width: 767px) {
  :global(.dateplan-modal) {
    width: 100vw !important;
    max-width: 100vw !important;
    height: 100dvh;
    margin: 0;
    border-radius: 0 !important;
  }
}
</style>
