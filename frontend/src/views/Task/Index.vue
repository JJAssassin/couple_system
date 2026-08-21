<template>
  <div class="task-page" ref="page">
    <!-- 顶部统计 -->
    <section class="hero">
      <div class="hero-title">我们的打卡</div>
      <div class="hero-stats">
        <div class="stat">
          <div class="stat-num">{{ stats?.totalPoints ?? 0 }}</div>
          <div class="stat-lbl">累计积分</div>
        </div>
        <div class="stat">
          <div class="stat-num">{{ stats?.streakDays ?? 0 }}</div>
          <div class="stat-lbl">连续打卡</div>
        </div>
        <div class="stat">
          <div class="stat-num">{{ stats?.todayCheckedInCount ?? 0 }}/{{ stats?.activeTaskCount ?? 0 }}</div>
          <div class="stat-lbl">今日完成</div>
        </div>
      </div>
    </section>

    <!-- 今日任务列表 -->
    <section class="block">
      <div class="block-head">
        <h2>今日任务</h2>
        <div class="block-actions">
          <NButton size="small" type="default" @click="doExport">导出任务数据</NButton>
          <NButton size="small" type="primary" @click="openCreate">+ 新建任务</NButton>
        </div>
      </div>
      <div v-if="templates.length" class="task-list">
        <div v-for="t in templates" :key="t.id" class="task-item" :class="{ done: isTodayDone(t.id) }">
          <div class="task-icon">{{ t.icon || '📌' }}</div>
          <div class="task-body">
            <div class="task-title">{{ t.title }}</div>
            <div class="task-meta">{{ freqText(t.frequency) }} · +{{ t.points }} 积分</div>
            <div v-if="t.description" class="task-desc">{{ t.description }}</div>
          </div>
          <div class="task-actions">
            <NButton
              size="small"
              :type="isTodayDone(t.id) ? 'default' : 'primary'"
              :disabled="isTodayDone(t.id)"
              @click="openCheckIn(t)"
            >{{ isTodayDone(t.id) ? '已打卡' : '打卡' }}</NButton>
            <NButton size="small" type="default" @click="openEdit(t)">编辑</NButton>
            <NButton size="small" type="error" @click="doDelete(t)">删除</NButton>
          </div>
        </div>
      </div>
      <IndEmpty v-else title="还没有任务" desc="点击右上角创建第一个打卡任务吧" />
    </section>

    <!-- 最近记录 -->
    <section class="block" v-if="recent.length">
      <h2>最近打卡</h2>
      <div class="rec-list">
        <div v-for="r in recent" :key="r.id" class="rec-item">
          <span class="rec-icon">{{ r.templateIcon || '✅' }}</span>
          <div class="rec-body">
            <div class="rec-title">{{ r.templateTitle }}</div>
            <div v-if="r.remark" class="rec-remark">{{ r.remark }}</div>
          </div>
          <div class="rec-right">
            <span class="rec-date">{{ fmtDate(r.completeDate) }}</span>
            <span class="rec-pts">+{{ r.earnedPoints }}</span>
          </div>
        </div>
      </div>
    </section>

    <!-- 新建 / 编辑弹窗 -->
    <NModal v-model:show="showCreate" preset="card" :title="editingId ? '编辑任务' : '新建任务'" style="max-width: 420px" class="task-modal">
      <NForm ref="formRef" :model="form" :rules="rules" label-placement="top" class="task-form">
        <NFormItem label="任务名称" path="title" class="task-form-item">
          <NInput v-model:value="form.title" placeholder="如：喝水、运动、晚安" class="task-input" />
        </NFormItem>
        <NFormItem label="描述" class="task-form-item">
          <NInput v-model:value="form.description" type="textarea" placeholder="可选" class="task-textarea" />
        </NFormItem>
        <NFormItem label="图标（emoji）" class="task-form-item">
          <NInput v-model:value="form.icon" placeholder="如 💪" class="task-input" />
        </NFormItem>
        <NFormItem label="积分" class="task-form-item">
          <NInputNumber v-model:value="form.points" :min="1" :max="1000" style="width: 100%" class="task-input" />
        </NFormItem>
        <NFormItem label="频率" class="task-form-item">
          <NSelect v-model:value="form.frequency" :options="freqOptions" class="task-select" />
        </NFormItem>
      </NForm>
      <template #footer>
        <div class="task-foot">
          <NButton class="task-btn-cancel" @click="showCreate = false">取消</NButton>
          <NButton type="primary" :loading="saving" @click="save" class="task-btn-primary">保存</NButton>
        </div>
      </template>
    </NModal>

    <!-- 打卡备注弹窗 -->
    <NModal v-model:show="showCheckIn" preset="card" title="打卡" style="max-width: 420px" class="task-modal checkin-modal">
      <NForm label-placement="top" class="task-form">
        <NFormItem label="备注（可选）" class="task-form-item">
          <NInput v-model:value="checkInRemark" type="textarea" placeholder="今天完成了吗？记录一下心情~" class="task-textarea" />
        </NFormItem>
      </NForm>
      <template #footer>
        <div class="task-foot">
          <NButton class="task-btn-cancel" @click="showCheckIn = false">取消</NButton>
          <NButton type="success" :loading="checkingIn" @click="doCheckIn" class="task-btn-success">确认打卡</NButton>
        </div>
      </template>
    </NModal>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { NButton, NModal, NForm, NFormItem, NInput, NInputNumber, NSelect, useMessage } from 'naive-ui';
import type { FormItemRule } from 'naive-ui';
import { listTaskTemplates, createTaskTemplate, updateTaskTemplate, deleteTaskTemplate, checkInTask, getTaskStats, listRecentTaskRecords, exportTaskData } from '@/api/task';
import type { TaskTemplateDto, TaskRecordDto, TaskStatsDto, TaskTemplateReq, TaskFrequency } from '@/types';
import IndEmpty from '@/components/industrial/IndEmpty.vue';

const msg = useMessage();
const loading = ref(true);
const templates = ref<TaskTemplateDto[]>([]);
const recent = ref<TaskRecordDto[]>([]);
const stats = ref<TaskStatsDto | null>(null);
const showCreate = ref(false);
const showCheckIn = ref(false);
const editingId = ref<number | null>(null);
const checkInId = ref<number | null>(null);
const checkInRemark = ref('');
const formRef = ref();
const saving = ref(false);
const checkingIn = ref(false);
const form = ref<TaskTemplateReq>({ title: '', description: '', icon: '', points: 10, taskType: 2, frequency: 1 });
const rules = {
  title: [{ required: true, message: '请输入任务名称', trigger: ['input', 'blur'] }],
};

const freqOptions = [
  { label: '每日', value: 1 },
  { label: '每周', value: 2 },
  { label: '每月', value: 3 },
  { label: '一次性', value: 4 },
];

function freqText(f: TaskFrequency | number): string {
  const map: Record<number, string> = { 1: '每日', 2: '每周', 3: '每月', 4: '一次性' };
  return map[f] || '每日';
}

const todaySet = ref<Set<number>>(new Set());
function isTodayDone(templateId: number) { return todaySet.value.has(templateId); }

async function load() {
  loading.value = true;
  try {
    const [tpls, st, recs] = await Promise.all([
      listTaskTemplates(1, 100, true),
      getTaskStats(),
      listRecentTaskRecords(20),
    ]);
    templates.value = tpls.items;
    stats.value = st;
    recent.value = recs;
    todaySet.value = new Set(recs.filter(r => r.completeDate === new Date().toISOString().slice(0, 10)).map(r => r.templateId));
  } finally {
    loading.value = false;
  }
}

function openCreate() {
  editingId.value = null;
  form.value = { title: '', description: '', icon: '', points: 10, taskType: 2, frequency: 1 };
  showCreate.value = true;
}
function openEdit(t: TaskTemplateDto) {
  editingId.value = t.id;
  form.value = { title: t.title, description: t.description, icon: t.icon || '', points: t.points, taskType: t.taskType, frequency: t.frequency };
  showCreate.value = true;
}
function openCheckIn(t: TaskTemplateDto) {
  checkInId.value = t.id;
  checkInRemark.value = '';
  showCheckIn.value = true;
}
async function save() {
  try {
    await formRef.value?.validate();
  } catch {
    return;
  }
  saving.value = true;
  try {
    if (editingId.value) {
      await updateTaskTemplate(editingId.value, form.value);
      msg.success('任务已更新');
    } else {
      await createTaskTemplate(form.value);
      msg.success('任务已创建');
    }
    showCreate.value = false;
    load();
  } finally {
    saving.value = false;
  }
}
async function doCheckIn() {
  if (!checkInId.value) return;
  checkingIn.value = true;
  try {
    await checkInTask({ templateId: checkInId.value, remark: checkInRemark.value });
    msg.success('打卡成功！');
    showCheckIn.value = false;
    load();
  } catch (e: any) {
    msg.error(e?.message || '打卡失败');
  } finally {
    checkingIn.value = false;
  }
}
async function doDelete(t: TaskTemplateDto) {
  if (!confirm(`确定删除「${t.title}」吗？打卡记录将一并撤销。`)) return;
  await deleteTaskTemplate(t.id);
  msg.success('已删除');
  load();
}
async function doExport() {
  await exportTaskData();
  msg.success('导出已开始，请检查下载');
}

function fmtDate(iso: string) {
  const d = new Date(iso);
  return `${d.getMonth() + 1}/${d.getDate()}`;
}

onMounted(load);
</script>

<style scoped>
.task-page { }
.hero { margin-bottom: 16px; }
.hero-title { font-size: 20px; font-weight: 700; margin-bottom: 12px; }
.hero-stats { display: flex; gap: 16px; }
.stat { flex: 1; background: var(--color-surface); border-radius: var(--radius-lg); padding: 16px; text-align: center; border: 1px solid var(--color-border); }
.stat-num { font-size: 24px; font-weight: 700; color: var(--color-rose); }
.stat-lbl { font-size: 12px; color: var(--color-ink-3); margin-top: 4px; }
.block-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px; }
.block-actions { display: flex; gap: 8px; }
.task-list { display: flex; flex-direction: column; gap: 10px; }
.task-item { display: flex; align-items: center; gap: 10px; padding: 12px; background: var(--color-surface); border-radius: var(--radius-lg); border: 1px solid var(--color-border); flex-wrap: wrap; }
.task-item.done { opacity: 0.7; }
.task-icon { font-size: 24px; flex-shrink: 0; }
.task-body { flex: 1; min-width: 120px; }
.task-title { font-weight: 600; }
.task-meta { font-size: 12px; color: var(--color-ink-3); margin-top: 2px; }
.task-desc { font-size: 12px; color: var(--color-ink-3); margin-top: 2px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; max-width: 100%; }
.task-actions { display: flex; gap: 6px; flex-wrap: wrap; }
.rec-list { display: flex; flex-direction: column; gap: 8px; }
.rec-item { display: flex; align-items: center; gap: 10px; padding: 10px 0; border-bottom: 1px solid var(--color-border); }
.rec-icon { font-size: 18px; }
.rec-body { flex: 1; }
.rec-title { font-weight: 600; }
.rec-remark { font-size: 12px; color: var(--color-ink-3); margin-top: 2px; }
.rec-right { text-align: right; }
.rec-date { font-size: 12px; color: var(--color-ink-3); display: block; }
.rec-pts { font-weight: 600; color: var(--color-rose); }

/* 美化任务模态框 */
:global(.task-modal) {
  border-radius: 16px !important;
  overflow: hidden;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.12) !important;
}
:global(.task-modal .n-modal-header) {
  background: linear-gradient(135deg, #f0f9ff, var(--color-surface)) !important;
  padding: 18px 24px !important;
  border-bottom: 1px solid var(--color-border);
}
:global(.task-modal .n-modal-header .n-modal-header__close) {
  top: 16px;
  right: 16px;
}
:global(.task-modal .n-modal-body) {
  padding: 24px !important;
}
:global(.task-modal .n-modal-footer) {
  padding: 16px 24px !important;
  border-top: 1px solid var(--color-border);
  background: var(--color-surface);
}
.task-form {
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.task-form-item {
  margin-bottom: 0 !important;
}
.task-input,
.task-textarea,
.task-select {
  border-radius: 10px !important;
}
.task-textarea :deep(.n-input__textarea),
.task-textarea :deep(textarea) {
  font-size: 15px;
  line-height: 1.7;
  padding: 12px 14px;
  border-radius: 10px;
}
.task-foot {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}
.task-btn-cancel {
  border-radius: 10px;
  padding: 8px 20px;
  font-weight: 500;
}
.task-btn-primary {
  border-radius: 10px;
  padding: 8px 24px;
  font-weight: 600;
  background: linear-gradient(135deg, #3b82f6, #2563eb);
  border: none;
  box-shadow: 0 4px 12px rgba(59, 130, 246, 0.25);
  transition: all var(--dur-micro) var(--ease-love);
}
.task-btn-primary:hover {
  box-shadow: 0 6px 16px rgba(59, 130, 246, 0.35);
  transform: translateY(-1px);
}
.task-btn-success {
  border-radius: 10px;
  padding: 8px 24px;
  font-weight: 600;
  background: linear-gradient(135deg, #52c41a, #389e0d);
  border: none;
  box-shadow: 0 4px 12px rgba(82, 196, 26, 0.25);
  transition: all var(--dur-micro) var(--ease-love);
}
.task-btn-success:hover {
  box-shadow: 0 6px 16px rgba(82, 196, 26, 0.35);
  transform: translateY(-1px);
}

@media (max-width: 767px) {
  :global(.task-modal) {
    width: 100vw !important;
    max-width: 100vw !important;
    height: 100dvh;
    margin: 0;
    border-radius: 0 !important;
  }
}
</style>
