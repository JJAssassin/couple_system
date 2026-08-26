<template>
  <div class="conflict-page" ref="container">
    <header class="page-head">
      <h1>矛盾复盘</h1>
      <n-button type="primary" round v-press-bounce @click="openAdd">+ 记录矛盾</n-button>
    </header>

    <IndSkeleton v-if="loading" variant="list" :rows="6" />
    <IndEmpty
      v-else-if="!list.length"
      title="还没有复盘记录"
      desc="把这次的摩擦记下来，好好聊聊，感情会更稳固～"
    />

    <div v-else>
      <div class="cards">
        <div
          v-for="c in list"
          :key="c.id"
          class="love-card"
          @click="openDetail(c)"
        >
          <div class="card-top">
            <n-tag :type="levelMap[c.conflictLevel]?.type ?? 'default'" size="small" round>
              {{ levelMap[c.conflictLevel]?.label ?? '未知' }}
            </n-tag>
            <span class="card-time sub-text">{{ fmt(c.occurTime) }}</span>
          </div>
          <div class="card-summary title-clamp">{{ c.summary }}</div>
          <div v-if="c.reconcileTime" class="card-reconciled sub-text">已和解</div>
        </div>
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
    <n-modal
      v-model:show="showForm"
      class="conflict-modal"
      preset="card"
      :title="editing ? '编辑复盘' : '记录一次矛盾'"
      style="width: 92%; max-width: 560px;"
    >
      <n-form ref="formRef" :model="form" label-placement="top" class="conflict-form">
        <n-form-item label="发生时间" class="conflict-form-item">
          <n-date-picker v-model:value="form.occurTs" type="datetime" style="width: 100%" class="conflict-picker" />
        </n-form-item>
        <n-form-item label="矛盾摘要" :rule="requiredRule('写一句这次怎么了～')" class="conflict-form-item">
          <n-input v-model:value="form.summary" placeholder="一句话记下这次怎么了" class="conflict-input" />
        </n-form-item>
        <n-form-item label="争吵等级" class="conflict-form-item">
          <n-select v-model:value="form.conflictLevel" :options="levelOptions" class="conflict-select" />
        </n-form-item>
        <n-form-item label="A 方想法" class="conflict-form-item">
          <n-input v-model:value="form.myThoughtA" type="textarea" placeholder="TA 当时在想什么" class="conflict-textarea" />
        </n-form-item>
        <n-form-item label="B 方想法" class="conflict-form-item">
          <n-input v-model:value="form.myThoughtB" type="textarea" placeholder="我当时在想什么" class="conflict-textarea" />
        </n-form-item>
        <n-form-item label="和解时间" class="conflict-form-item">
          <n-date-picker v-model:value="form.reconcileTs" type="datetime" clearable style="width: 100%" class="conflict-picker" />
        </n-form-item>
        <n-form-item label="和解方式" class="conflict-form-item">
          <n-input v-model:value="form.reconcileWay" placeholder="怎么和好的（可选）" class="conflict-input" />
        </n-form-item>
        <n-form-item label="A 方反思" class="conflict-form-item">
          <n-input v-model:value="form.reflectA" type="textarea" placeholder="我后来怎么想（可选）" class="conflict-textarea" />
        </n-form-item>
        <n-form-item label="B 方反思" class="conflict-form-item">
          <n-input v-model:value="form.reflectB" type="textarea" placeholder="对方怎么想（可选）" class="conflict-textarea" />
        </n-form-item>
        <n-form-item label="相处约定" class="conflict-form-item">
          <n-input v-model:value="form.ruleConclusion" type="textarea" placeholder="以后我们约定…（可选）" class="conflict-textarea" />
        </n-form-item>
      </n-form>
      <template #footer>
        <div class="conflict-foot">
          <n-button class="conflict-btn-cancel" @click="showForm = false">取消</n-button>
          <n-button type="primary" :loading="submitting" v-press-bounce @click="submitForm" class="conflict-btn-primary">保存</n-button>
        </div>
      </template>
    </n-modal>

    <!-- 详情 -->
    <n-drawer v-model:show="showDetail" :width="420" placement="right" class="conflict-drawer">
      <n-drawer-content :title="detail?.summary || '矛盾详情'">
        <template v-if="detail">
          <div class="detail-row">
            <span class="k">等级</span>
            <n-tag :type="levelMap[detail.conflictLevel].type" size="small" round>
              {{ levelMap[detail.conflictLevel].label }}
            </n-tag>
          </div>
          <div class="detail-row"><span class="k">发生时间</span><span>{{ fmt(detail.occurTime) }}</span></div>
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
import { ref, reactive, computed, onMounted } from 'vue';
import {
  NButton, NModal, NDrawer, NDrawerContent, NForm, NFormItem,
  NInput, NDatePicker, NSelect, NTag, NPopconfirm,
} from 'naive-ui';
import type { ConflictDto, ConflictReq } from '@/types';
import {
  listConflict, getConflict, createConflict, updateConflict, deleteConflict,
} from '@/api/conflict';
import { useNotifyStore } from '@/store/notifyStore';
import { useStaggerEnter } from '@/composables/useAnimation';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndPager from '@/components/industrial/IndPager.vue';
import { feedback } from '@/utils/feedback';
import { requiredRule } from '@/utils/formRules';
import { usePagedList } from '@/composables/usePagedList';

const notify = useNotifyStore();
const submitting = ref(false);
const container = ref<HTMLElement>();
const formRef = ref();

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

function fmt(s: string) {
  const d = new Date(s);
  return `${d.getFullYear()}.${d.getMonth() + 1}.${d.getDate()} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
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
  try {
    await formRef.value?.validate();
  } catch { return; }
  submitting.value = true;
  try {
    if (editing.value) await updateConflict(editing.value.id, buildReq());
    else await createConflict(buildReq());
    feedback.saved('复盘');
    showForm.value = false;
    await refresh();
  } finally { submitting.value = false; }
}

// ---- 详情 ----
const showDetail = ref(false);
const detail = ref<ConflictDto | null>(null);

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
const { onSync } = useRealtime();
onMounted(async () => {
  await loadFirst();
  onSync('conflict', () => refresh());
});
</script>

<style scoped>
.conflict-page { max-width: 960px; margin: 0 auto; }
.page-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 16px; }
.page-head h1 { font-size: 22px; margin: 0; }
.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 14px; }
.card-top { display: flex; align-items: center; justify-content: space-between; gap: 8px; }
.card-time { font-size: 12px; }
.card-summary { margin-top: 8px; font-size: 15px; color: var(--color-ink); }
.card-reconciled { margin-top: 8px; color: var(--color-rose); font-size: 13px; }
.modal-foot { display: flex; justify-content: flex-end; gap: 10px; }

.detail-row { display: flex; align-items: center; gap: 10px; margin: 12px 0; }
.detail-row .k, .detail-block .k { color: var(--color-ink-3); font-size: 13px; }
.detail-block { margin: 14px 0; }
.detail-block p { margin: 6px 0 0; white-space: pre-wrap; line-height: 1.8; }
.detail-actions { display: flex; flex-direction: column; gap: 10px; margin-top: 24px; }

@media (max-width: 767px) {
  .cards { grid-template-columns: 1fr; }
}

/* 美化矛盾复盘模态框 */
:global(.conflict-modal) {
  border-radius: 16px !important;
  overflow: hidden;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.12) !important;
}
:global(.conflict-modal .n-modal-header) {
  background: linear-gradient(135deg, #fff5f5, var(--color-surface)) !important;
  padding: 18px 24px !important;
  border-bottom: 1px solid var(--color-border);
}
:global(.conflict-modal .n-modal-header .n-modal-header__close) {
  top: 16px;
  right: 16px;
}
:global(.conflict-modal .n-modal-body) {
  padding: 24px !important;
  max-height: 70vh;
  overflow-y: auto;
}
:global(.conflict-modal .n-modal-footer) {
  padding: 16px 24px !important;
  border-top: 1px solid var(--color-border);
  background: var(--color-surface);
}
.conflict-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.conflict-form-item {
  margin-bottom: 0 !important;
}
.conflict-input,
.conflict-textarea,
.conflict-select,
.conflict-picker {
  border-radius: 10px !important;
}
.conflict-textarea :deep(.n-input__textarea),
.conflict-textarea :deep(textarea) {
  font-size: 15px;
  line-height: 1.7;
  padding: 12px 14px;
  border-radius: 10px;
}
.conflict-foot {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}
.conflict-btn-cancel {
  border-radius: 10px;
  padding: 8px 20px;
  font-weight: 500;
}
.conflict-btn-primary {
  border-radius: 10px;
  padding: 8px 24px;
  font-weight: 600;
  background: linear-gradient(135deg, var(--color-rose), var(--color-rose-deep));
  border: none;
  box-shadow: 0 4px 12px rgba(255, 111, 125, 0.25);
  transition: all var(--dur-micro) var(--ease-love);
}
.conflict-btn-primary:hover {
  box-shadow: 0 6px 16px rgba(255, 111, 125, 0.35);
  transform: translateY(-1px);
}

@media (max-width: 767px) {
  :global(.conflict-modal) {
    width: 100vw !important;
    max-width: 100vw !important;
    height: 100dvh;
    margin: 0;
    border-radius: 0 !important;
  }
  :global(.conflict-modal .n-modal-body) {
    max-height: calc(100dvh - 140px);
  }
}
</style>
