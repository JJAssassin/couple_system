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

    <!-- 本月预算 -->
    <section class="block">
      <div class="block-head">
        <h2>本月预算</h2>
        <div class="month-pick">
          <NDatePicker v-model:value="budgetMonthTs" type="month" size="small" style="width: 150px" @update:value="onBudgetMonthChange" />
          <NButton size="small" quaternary type="primary" @click="openBudget">设预算</NButton>
        </div>
      </div>

      <div v-if="budget" class="budget-body">
        <div v-if="budget.totalBudget != null" class="budget-overall">
          <div class="bo-top">
            <span>支出 / 预算</span>
            <span :class="budget.isOverspent ? 'over' : ''">
              ¥{{ budget.expense.toFixed(2) }} / ¥{{ (budget.totalBudget ?? 0).toFixed(2) }}
            </span>
          </div>
          <div class="bar">
            <div class="bar-fill" :class="budget.isOverspent ? 'over' : ''" :style="{ width: budgetPct + '%' }"></div>
          </div>
          <div class="bo-foot">
            <span v-if="budget.isOverspent" class="tag over">超支 ¥{{ Math.abs(budget.remaining).toFixed(2) }}</span>
            <span v-else class="tag ok">剩余 ¥{{ budget.remaining.toFixed(2) }}</span>
          </div>
        </div>
        <div v-else class="budget-empty">
          还没设 {{ budget.year }} 年 {{ budget.month }} 月的总预算，点「设预算」规划一下吧～
        </div>

        <div v-if="budget.categories.length" class="cat-list">
          <div v-for="c in budget.categories" :key="c.category" class="cat-row">
            <span class="cat-name">{{ c.category || '未分类' }}</span>
            <span class="cat-amt">¥{{ c.amount.toFixed(2) }}</span>
            <span v-if="c.budget != null" class="cat-budget">预算 ¥{{ c.budget.toFixed(2) }}</span>
            <span v-if="c.isOverspent" class="tag over sm">超支</span>
          </div>
        </div>
      </div>
    </section>

    <!-- 消费分类（当月支出构成，跟随预算月份） -->
    <section class="block">
      <div class="block-head">
        <h2>当月消费分类</h2>
        <div class="month-pick">
          <NButton size="small" quaternary @click="showPoster = true">生成海报</NButton>
          <span class="sub-text">钱都花哪了，一目了然</span>
        </div>
      </div>
      <ChartWrap v-if="catPieData.length" :option="catPieOption" />
      <IndEmpty v-else title="本月还没有支出" desc="记几笔支出，就能看到钱都花在哪啦" />
    </section>

    <!-- 月度趋势 -->
    <section class="block">
      <div class="block-head">
        <h2>近 6 个月收支趋势</h2>
        <span class="sub-text">和 TA 一起看看小金库的走势</span>
      </div>
      <ChartWrap v-if="stats?.trend?.length" :option="trendOption" />
      <IndEmpty v-else title="暂无趋势数据" desc="记账后这里会展示你们的收支走向" />
    </section>

    <!-- 记账列表 + 记一笔 -->
    <section class="block">
      <div class="block-head">
        <h2>账单明细</h2>
        <div class="month-pick">
          <NButton size="small" quaternary @click="exportCsv">导出 CSV</NButton>
          <NButton type="primary" size="small" @click="openCreate">+ 记一笔</NButton>
        </div>
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

    <NModal v-model:show="showModal" :title="editing ? '编辑记录' : '记一笔'" preset="card" style="max-width: 440px" class="account-modal">
      <NForm ref="formRef" :model="form" :rules="rules" class="account-form">
        <NFormItem label="类型" path="recordType" class="account-form-item">
          <NSelect v-model:value="form.recordType" :options="typeOptions" class="account-input" />
        </NFormItem>
        <NFormItem label="分类" path="category" class="account-form-item">
          <NInput v-model:value="form.category" placeholder="如：餐饮 / 工资" class="account-input" />
        </NFormItem>
        <NFormItem label="金额" path="amount" class="account-form-item">
          <NInputNumber v-model:value="form.amount" :min="0" :precision="2" style="width: 100%" class="account-input" />
        </NFormItem>
        <NFormItem label="时间" path="time" class="account-form-item">
          <NDatePicker v-model:value="form.time" type="date" style="width: 100%" class="account-picker" />
        </NFormItem>
        <NFormItem label="备注" path="remark" class="account-form-item">
          <NInput v-model:value="form.remark" type="textarea" placeholder="可选" class="account-textarea" />
        </NFormItem>
      </NForm>
      <template #footer>
        <div class="account-foot">
          <NButton class="account-btn-cancel" @click="showModal = false">取消</NButton>
          <NButton type="primary" :loading="loading" @click="save" class="account-btn-primary">保存</NButton>
        </div>
      </template>
    </NModal>

    <NModal v-model:show="showBudget" title="设置预算" preset="card" style="max-width: 460px" class="account-modal budget-modal">
      <NForm class="budget-form">
        <NFormItem label="月份" class="budget-form-item">
          <NDatePicker v-model:value="bForm.monthTs" type="month" style="width: 100%" class="budget-picker" />
        </NFormItem>
        <NFormItem label="当月总预算（留空 = 不设）" class="budget-form-item">
          <NInputNumber v-model:value="bForm.total" :min="0" :precision="2" style="width: 100%" placeholder="如 3000" class="budget-input" />
        </NFormItem>
        <NButton type="primary" block :disabled="bForm.total == null" @click="saveTotal" class="budget-btn-primary">保存总预算</NButton>
      </NForm>

      <NDivider>分类预算</NDivider>
      <div class="cat-budgets">
        <div v-for="b in catBudgets" :key="b.id" class="cb-row">
          <span class="cb-name">{{ b.category }}</span>
          <span class="cb-amt">¥{{ b.limitAmount.toFixed(2) }}</span>
          <NPopconfirm @positive-click="removeCatBudget(b)">
            <template #trigger><NButton size="small" quaternary type="error">删</NButton></template>
            删除该分类预算？
          </NPopconfirm>
        </div>
        <IndEmpty v-if="!catBudgets.length" title="还没有分类预算" desc="按分类设额度，超支会标红提醒" />
      </div>
      <div class="cb-add">
        <NInput v-model:value="bForm.catName" placeholder="分类名（如 餐饮）" style="flex: 1" class="budget-input" />
        <NInputNumber v-model:value="bForm.catLimit" :min="0" :precision="2" placeholder="额度" class="budget-input" />
        <NButton type="primary" size="small" :disabled="!bForm.catName || bForm.catLimit == null" @click="addCatBudget" class="budget-btn-add">添加</NButton>
      </div>
    </NModal>

    <!-- 月度消费海报 -->
    <NModal v-model:show="showPoster" preset="card" title="分享你的本月消费" style="max-width: 420px" class="account-modal poster-modal">
      <ExpensePoster
        ref="posterRef"
        :total-expense="summary.expend"
        :categories="catPieData.map(c => ({ category: c.category, amount: c.amount, percent: (c.amount / Math.max(summary.expend, 0.01)) * 100 }))"
        :date-text="`${budgetYear} 年 ${budgetMonth} 月`"
      />
      <template #footer>
        <div class="account-foot">
          <NButton class="account-btn-cancel" @click="showPoster = false">关闭</NButton>
          <NButton type="primary" @click="posterRef?.download" class="account-btn-primary">保存到相册</NButton>
        </div>
      </template>
    </NModal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import {
  NButton, NModal, NForm, NFormItem, NInput, NInputNumber, NDatePicker, NSelect, NTag, NPopconfirm, NTabs, NTabPane, NDivider,
} from 'naive-ui';
import type { FormItemRule } from 'naive-ui';
import type { EChartsOption } from 'echarts';
import type { AccountRecordDto, MonthlyBudgetDto, BudgetDto, AccountStatisticsDto } from '@/types';
import * as ac from '@/api/account';
import * as bg from '@/api/budget';
import ChartWrap from '@/components/ChartWrap.vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndPager from '@/components/industrial/IndPager.vue';
import ExpensePoster from '@/components/Common/ExpensePoster.vue';
import { useStaggerEnter } from '@/composables/useAnimation';
import { usePagedList } from '@/composables/usePagedList';
import { feedback } from '@/utils/feedback';
import { selectRule, dateRule } from '@/utils/formRules';

const loading = ref(true);
const summary = ref<ac.AccountSummary>({ income: 0, expend: 0, balance: 0 });
const showPoster = ref(false);
const posterRef = ref<InstanceType<typeof ExpensePoster> | null>(null);
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

// —— 统计：当月消费分类 + 近 6 月趋势 ——
const stats = ref<AccountStatisticsDto | null>(null);
const catPalette = ['#ff6f7d', '#ff9f6e', '#ffc46b', '#7ec8a4', '#6ba7d6', '#b48ad9', '#e584b4', '#8ec6c5', '#c9b26b', '#9aa5b1'];

const catPieData = computed(() => (budget.value?.categories.filter((c) => c.amount > 0) ?? []).sort((a, b) => b.amount - a.amount));

const catPieOption = computed<EChartsOption>(() => ({
  tooltip: {
    trigger: 'item',
    formatter: (p: any) => `${p.name}：¥${Number(p.value).toFixed(2)}（${p.percent}%）`,
  },
  legend: { bottom: 0, type: 'scroll' },
  series: [{
    type: 'pie',
    radius: ['42%', '68%'],
    center: ['50%', '44%'],
    avoidLabelOverlap: true,
    itemStyle: { borderRadius: 6, borderColor: '#fff', borderWidth: 2 },
    label: { formatter: '{b}\n¥{c}' },
    data: catPieData.value.map((c, i) => ({
      name: c.category || '未分类',
      value: Number(c.amount.toFixed(2)),
      itemStyle: { color: catPalette[i % catPalette.length] },
    })),
  }],
}));

const trendOption = computed<EChartsOption>(() => {
  const t = stats.value?.trend ?? [];
  return {
    tooltip: { trigger: 'axis' },
    legend: { top: 0, right: 0 },
    grid: { left: 8, right: 8, top: 34, bottom: 0, containLabel: true },
    xAxis: { type: 'category', data: t.map((x) => x.month.slice(5).replace('-', '月') + '月') },
    yAxis: { type: 'value', axisLabel: { formatter: '{value}' } },
    series: [
      {
        name: '收入', type: 'bar', barMaxWidth: 20,
        data: t.map((x) => Number(x.income.toFixed(2))),
        itemStyle: { color: '#5BB98C', borderRadius: [4, 4, 0, 0] },
      },
      {
        name: '支出', type: 'bar', barMaxWidth: 20,
        data: t.map((x) => Number(x.expense.toFixed(2))),
        itemStyle: { color: '#ff6f7d', borderRadius: [4, 4, 0, 0] },
      },
    ],
  };
});

async function loadStatistics() {
  stats.value = await ac.accountStatistics(budgetYear.value, budgetMonth.value);
}

function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}

async function exportCsv() {
  try {
    const blob = await ac.exportAccountCsv(budgetYear.value, budgetMonth.value);
    const name = `couple-account-${budgetYear.value}-${String(budgetMonth.value).padStart(2, '0')}.csv`;
    downloadBlob(blob, name);
    feedback.exported('账单 CSV');
  } catch {
    feedback.error('导出失败，请稍后再试');
  }
}

// —— 预算 ——
const budget = ref<MonthlyBudgetDto | null>(null);
const budgetYear = ref(new Date().getFullYear());
const budgetMonth = ref(new Date().getMonth() + 1);
const budgetMonthTs = ref(new Date(new Date().getFullYear(), new Date().getMonth(), 1).getTime());

const budgetPct = computed(() => {
  if (!budget.value || budget.value.totalBudget == null || budget.value.totalBudget === 0) return 0;
  return Math.min(100, Math.round((budget.value.expense / budget.value.totalBudget) * 100));
});

async function loadBudget() {
  budget.value = await bg.getMonthlyBudget(budgetYear.value, budgetMonth.value);
}
function onBudgetMonthChange(ts: number) {
  const d = new Date(ts);
  budgetYear.value = d.getFullYear();
  budgetMonth.value = d.getMonth() + 1;
  loadBudget();
  loadStatistics();
}

const showBudget = ref(false);
const catBudgets = ref<BudgetDto[]>([]);
const bForm = ref<{ monthTs: number; total: number | null; catName: string; catLimit: number | null }>({
  monthTs: budgetMonthTs.value, total: null, catName: '', catLimit: null,
});
async function openBudget() {
  bForm.value.monthTs = budgetMonthTs.value;
  bForm.value.total = budget.value?.totalBudget ?? null;
  await refreshCatBudgets();
  showBudget.value = true;
}
async function refreshCatBudgets() {
  const d = new Date(bForm.value.monthTs);
  catBudgets.value = (await bg.listBudgets(d.getFullYear(), d.getMonth() + 1)).filter((b) => b.category);
}
async function saveTotal() {
  if (bForm.value.total == null) return;
  const d = new Date(bForm.value.monthTs);
  await bg.setBudget({ year: d.getFullYear(), month: d.getMonth() + 1, limitAmount: bForm.value.total });
  feedback.saved('总预算');
  await Promise.all([loadBudget(), refreshCatBudgets()]);
}
async function addCatBudget() {
  if (!bForm.value.catName || bForm.value.catLimit == null) return;
  const d = new Date(bForm.value.monthTs);
  await bg.setBudget({ year: d.getFullYear(), month: d.getMonth() + 1, category: bForm.value.catName, limitAmount: bForm.value.catLimit });
  feedback.created('分类预算');
  bForm.value.catName = '';
  bForm.value.catLimit = null;
  await Promise.all([loadBudget(), refreshCatBudgets()]);
}
async function removeCatBudget(b: BudgetDto) {
  await bg.deleteBudget(b.id);
  feedback.deleted('该分类预算');
  await Promise.all([loadBudget(), refreshCatBudgets()]);
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
  await Promise.all([loadSummary(), refreshList(), loadBudget(), loadStatistics()]);
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
  // 超额提醒：记了一笔支出后，若当月总预算或分类预算超支，主动提醒一次
  if (req.recordType === 2) {
    const b = budget.value;
    if (b?.totalBudget != null && b.isOverspent) {
      feedback.warn(`本月总预算超支 ¥${Math.abs(b.remaining).toFixed(2)}，和 TA 一起控制一下支出吧`);
    } else {
      const over = b?.categories.filter((c) => c.isOverspent) ?? [];
      if (over.length) {
        const c = over[0];
        feedback.warn(`「${c.category || '未分类'}」已超预算 ¥${Math.abs(c.amount - (c.budget ?? 0)).toFixed(2)}，注意控制哦`);
      }
    }
  }
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
  onSync('budget', loadBudget);
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
.month-pick { display: flex; gap: 8px; align-items: center; }

/* 预算 */
.budget-body { display: flex; flex-direction: column; gap: 14px; }
.budget-overall { background: var(--color-bg-soft, #faf6f2); border-radius: 12px; padding: 14px 16px; }
.bo-top { display: flex; justify-content: space-between; font-size: 14px; color: var(--color-ink-2); }
.bo-top .over { color: var(--color-rose); font-weight: 600; }
.bar { height: 10px; background: #efe7e0; border-radius: 6px; overflow: hidden; margin: 10px 0 8px; }
.bar-fill { height: 100%; background: #5BB98C; border-radius: 6px; transition: width .4s ease; }
.bar-fill.over { background: var(--color-rose); }
.bo-foot { display: flex; gap: 8px; }
.tag { font-size: 12px; padding: 2px 10px; border-radius: 999px; }
.tag.ok { background: #e7f6ee; color: #2f9e6b; }
.tag.over { background: #fdeaec; color: var(--color-rose); }
.tag.sm { padding: 1px 8px; }
.budget-empty { color: var(--color-ink-3); font-size: 13px; padding: 8px 0; }
.cat-list { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 10px; }
.cat-row { display: flex; align-items: center; gap: 8px; background: #fff; border: 1px solid var(--color-border, #eee); border-radius: 10px; padding: 8px 12px; }
.cat-name { font-weight: 600; }
.cat-amt { color: var(--color-rose); }
.cat-budget { color: var(--color-ink-3); font-size: 12px; }

/* 分类预算弹窗 */
.cat-budgets { display: flex; flex-direction: column; gap: 8px; margin-bottom: 12px; }
.cb-row { display: flex; align-items: center; gap: 10px; }
.cb-name { font-weight: 600; flex: 1; }
.cb-amt { color: var(--color-ink-2); }
.cb-add { display: flex; gap: 8px; align-items: center; }

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
  .cat-list { grid-template-columns: 1fr; }
}

/* 美化记账模态框 */
:global(.account-modal) {
  border-radius: 16px !important;
  overflow: hidden;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.12) !important;
}
:global(.account-modal .n-modal-header) {
  background: linear-gradient(135deg, #f0fdf4, var(--color-surface)) !important;
  padding: 18px 24px !important;
  border-bottom: 1px solid var(--color-border);
}
:global(.account-modal .n-modal-header .n-modal-header__close) {
  top: 16px;
  right: 16px;
}
:global(.account-modal .n-modal-body) {
  padding: 24px !important;
}
:global(.account-modal .n-modal-footer) {
  padding: 16px 24px !important;
  border-top: 1px solid var(--color-border);
  background: var(--color-surface);
}
.account-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.account-form-item {
  margin-bottom: 0 !important;
}
.account-input,
.account-textarea,
.account-select,
.account-picker {
  border-radius: 10px !important;
}
.account-textarea :deep(.n-input__textarea),
.account-textarea :deep(textarea) {
  font-size: 15px;
  line-height: 1.7;
  padding: 12px 14px;
  border-radius: 10px;
}
.account-foot {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}
.account-btn-cancel {
  border-radius: 10px;
  padding: 8px 20px;
  font-weight: 500;
}
.account-btn-primary {
  border-radius: 10px;
  padding: 8px 24px;
  font-weight: 600;
  background: linear-gradient(135deg, #5BB98C, #3d9e6b);
  border: none;
  box-shadow: 0 4px 12px rgba(91, 185, 140, 0.25);
  transition: all var(--dur-micro) var(--ease-love);
}
.account-btn-primary:hover {
  box-shadow: 0 6px 16px rgba(91, 185, 140, 0.35);
  transform: translateY(-1px);
}
.account-btn-primary:active {
  transform: translateY(0);
}

/* 预算模态框 */
.budget-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.budget-form-item {
  margin-bottom: 0 !important;
}
.budget-input,
.budget-picker {
  border-radius: 10px !important;
}
.budget-btn-primary {
  border-radius: 10px;
  padding: 10px 24px;
  font-weight: 600;
}
.budget-btn-add {
  border-radius: 8px;
  font-weight: 500;
}

/* 海报模态框 */
:global(.poster-modal) {
  border-radius: 16px !important;
  overflow: hidden;
}
:global(.poster-modal .n-modal-header) {
  background: linear-gradient(135deg, #fff5f6, var(--color-surface)) !important;
  padding: 18px 24px !important;
  border-bottom: 1px solid var(--color-border);
}

@media (max-width: 767px) {
  :global(.account-modal),
  :global(.budget-modal),
  :global(.poster-modal) {
    width: 100vw !important;
    max-width: 100vw !important;
    height: 100dvh;
    margin: 0;
    border-radius: 0 !important;
  }
}
</style>
