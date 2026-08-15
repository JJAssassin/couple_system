<template>
  <IndSkeleton v-if="loading" variant="list" :rows="5" />
  <div v-else class="account" ref="container">
    <!-- 余额大字 -->
    <section class="hero">
      <div class="hero-title">共同小金库余额</div>
      <div class="hero-balance">¥{{ summary.balance.toFixed(2) }}</div>
      <div class="hero-sub">
        <span>收入 ¥{{ summary.income.toFixed(2) }}</span>
        <span>支出 ¥{{ summary.expend.toFixed(2) }}</span>
      </div>
    </section>

    <!-- 收支图表 -->
    <section class="block">
      <h2>收支占比</h2>
      <ChartWrap :option="pieOption" />
    </section>

    <!-- 记账列表 + 记一笔 -->
    <section class="block">
      <div class="block-head">
        <h2>账单明细</h2>
        <NButton type="primary" size="small" @click="openCreate">+ 记一笔</NButton>
      </div>
      <n-tabs v-model:value="recFilter" type="segment" class="rec-tabs">
        <n-tab-pane name="all" tab="全部" />
        <n-tab-pane name="in" tab="收入" />
        <n-tab-pane name="out" tab="支出" />
      </n-tabs>
      <div v-if="filtered.length" class="records">
        <div v-for="r in filtered" :key="r.id" class="love-card rec">
          <div class="rec-left">
            <NTag :type="r.recordType === 1 ? 'success' : 'warning'" size="small">
              {{ r.recordType === 1 ? '收入' : '支出' }}
            </NTag>
            <div class="rec-cat">{{ r.category || '未分类' }}</div>
          </div>
          <div class="rec-mid">
            <div class="rec-amt" :class="r.recordType === 1 ? 'in' : 'out'">
              {{ r.recordType === 1 ? '+' : '-' }}¥{{ r.amount.toFixed(2) }}
            </div>
            <div class="sub-text">{{ fmtTime(r.recordTime) }}<span v-if="r.remark"> · {{ r.remark }}</span></div>
          </div>
          <div class="rec-ops">
            <NButton size="small" quaternary @click="openEdit(r)">改</NButton>
            <NPopconfirm @positive-click="remove(r)">
              <template #trigger>
                <NButton size="small" quaternary type="error">删</NButton>
              </template>
              确认删除这笔记录？
            </NPopconfirm>
          </div>
        </div>
      </div>
      <IndEmpty v-else title="还没有记账记录" desc="和 TA 一起记一笔，看看共同小金库怎么花最甜" />
      <IndPager v-if="list.length" mode="more" :page="page" :page-size="pageSize" :loading="listLoading" :has-more="hasMore" :total="total" @load-more="nextPage" />
    </section>

    <NModal v-model:show="showModal" :title="editing ? '编辑记录' : '记一笔'" preset="card" style="max-width: 440px">
      <NForm ref="formRef" :model="form" :rules="rules">
        <NFormItem label="类型" path="recordType">
          <NSelect v-model:value="form.recordType" :options="typeOptions" />
        </NFormItem>
        <NFormItem label="分类" path="category">
          <NInput v-model:value="form.category" placeholder="如：餐饮 / 工资" />
        </NFormItem>
        <NFormItem label="金额" path="amount">
          <NInputNumber v-model:value="form.amount" :min="0" :precision="2" style="width: 100%" />
        </NFormItem>
        <NFormItem label="时间" path="time">
          <NDatePicker v-model:value="form.time" type="date" style="width: 100%" />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="form.remark" type="textarea" placeholder="可选" />
        </NFormItem>
      </NForm>
      <template #footer>
        <div class="modal-foot">
          <NButton @click="showModal = false">取消</NButton>
          <NButton type="primary" @click="save">保存</NButton>
        </div>
      </template>
    </NModal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import {
  NButton, NModal, NForm, NFormItem, NInput, NInputNumber, NDatePicker, NSelect, NTag, NPopconfirm, NTabs, NTabPane,
} from 'naive-ui';
import type { FormItemRule } from 'naive-ui';
import type { EChartsOption } from 'echarts';
import type { AccountRecordDto } from '@/types';
import * as ac from '@/api/account';
import ChartWrap from '@/components/ChartWrap.vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndPager from '@/components/industrial/IndPager.vue';
import { useStaggerEnter } from '@/composables/useAnimation';
import { usePagedList } from '@/composables/usePagedList';
import { feedback } from '@/utils/feedback';
import { selectRule, dateRule } from '@/utils/formRules';

const loading = ref(true);
const summary = ref<ac.AccountSummary>({ income: 0, expend: 0, balance: 0 });
const { list, page, pageSize, total, loading: listLoading, hasMore, refresh: refreshList, nextPage } = usePagedList<AccountRecordDto>(
  async (p) => {
    const d = await ac.listAccount({ page: p.page, pageSize: p.pageSize });
    return { items: d.items, total: d.total };
  },
  { pageSize: 15, mode: 'more' }
);

const formRef = ref<InstanceType<typeof NForm>>();
const amountRule: FormItemRule = {
  validator(_r, v) {
    if (v === null || v === undefined || v === '') return new Error('请输入金额');
    if (v <= 0) return new Error('金额要大于 0 哦');
    return true;
  },
  trigger: ['input', 'blur'],
};
const rules = {
  recordType: [selectRule('请选择收支类型')],
  amount: [amountRule],
  time: [dateRule('请选择记账时间')],
};

const recFilter = ref<'all' | 'in' | 'out'>('all');
const filtered = computed(() =>
  list.value.filter((r) =>
    recFilter.value === 'all' ? true : recFilter.value === 'in' ? r.recordType === 1 : r.recordType === 2
  )
);
const container = ref<HTMLElement>();
useStaggerEnter(container, '.block', { stagger: 0.1, y: 16 });

const fmtTime = (iso: string) => new Date(iso).toLocaleDateString('zh-CN');

const typeOptions = [
  { label: '收入', value: 1 },
  { label: '支出', value: 2 },
];

const pieOption = ref<EChartsOption>({
  tooltip: { trigger: 'item' },
  legend: { bottom: 0 },
  series: [{
    type: 'pie', radius: ['45%', '70%'], center: ['50%', '45%'],
    data: [
      { name: '收入', value: 0, itemStyle: { color: '#5BB98C' } },
      { name: '支出', value: 0, itemStyle: { color: '#ff6f7d' } },
    ],
    label: { formatter: '{b}\n¥{c}' },
  }],
});

function refreshPie() {
  (pieOption.value.series as any)[0].data = [
    { name: '收入', value: Number(summary.value.income.toFixed(2)), itemStyle: { color: '#5BB98C' } },
    { name: '支出', value: Number(summary.value.expend.toFixed(2)), itemStyle: { color: '#ff6f7d' } },
  ];
}

const showModal = ref(false);
const editing = ref<AccountRecordDto | null>(null);
const form = ref<{ recordType: number; category: string; amount: number | null; time: number | null; remark?: string }>({
  recordType: 2, category: '', amount: null, time: Date.now(), remark: '',
});

async function loadSummary() {
  summary.value = await ac.accountSummary();
  refreshPie();
}
async function refresh() {
  await Promise.all([loadSummary(), refreshList()]);
}

function openCreate() {
  editing.value = null;
  form.value = { recordType: 2, category: '', amount: null, time: Date.now(), remark: '' };
  showModal.value = true;
}
function openEdit(r: AccountRecordDto) {
  editing.value = r;
  form.value = {
    recordType: r.recordType,
    category: r.category,
    amount: r.amount,
    time: new Date(r.recordTime).getTime(),
    remark: r.remark,
  };
  showModal.value = true;
}
async function save() {
  try {
    await formRef.value?.validate();
  } catch {
    return;
  }
  const req = {
    recordType: form.value.recordType as 1 | 2,
    category: form.value.category || '未分类',
    amount: form.value.amount as number,
    recordTime: new Date(form.value.time ?? Date.now()).toISOString(),
    remark: form.value.remark,
  };
  if (editing.value) {
    await ac.updateAccount(editing.value.id, req);
    feedback.updated('记录');
  } else {
    await ac.createAccount(req);
    feedback.created('一笔账');
  }
  showModal.value = false;
  await refresh();
}
async function remove(r: AccountRecordDto) {
  await ac.deleteAccount(r.id);
  feedback.deleted('这笔记录');
  await refresh();
}

import { useRealtime } from '@/composables/useRealtime';
const { onSync } = useRealtime();
onMounted(async () => {
  try { await refresh(); } finally { loading.value = false; }
  onSync('account', refresh);
});
</script>

<style scoped>
.account { max-width: 880px; margin: 0 auto; }
.hero { text-align: center; padding: 24px 0 8px; }
.hero-title { color: var(--color-ink-2); }
.hero-balance { font-size: 48px; font-weight: 600; color: var(--color-cocoa); }
.hero-sub { color: var(--color-ink-3); font-size: 13px; display: flex; gap: 18px; justify-content: center; }
.block { margin: 22px 0; }
.block h2 { font-size: 16px; margin: 0 0 12px; }
.block-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px; }
.block-head h2 { margin: 0; }
.rec-tabs { margin-bottom: 14px; }
.records { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 12px; }
.rec { display: flex; align-items: center; gap: 12px; }
.rec-left { display: flex; flex-direction: column; gap: 6px; }
.rec-cat { font-weight: 600; }
.rec-mid { flex: 1; }
.rec-amt { font-size: 18px; font-weight: 600; }
.rec-amt.in { color: #5BB98C; }
.rec-amt.out { color: var(--color-rose); }
.rec-ops { display: flex; gap: 4px; flex-shrink: 0; }
.modal-foot { display: flex; justify-content: flex-end; gap: 10px; }
@media (max-width: 767px) {
  .records { grid-template-columns: 1fr; }
}
</style>
