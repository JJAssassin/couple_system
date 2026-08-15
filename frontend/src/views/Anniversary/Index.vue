<template>
  <IndSkeleton v-if="loading" variant="grid" :rows="6" :columns="3" />
  <div v-else class="anniv-page" ref="container">
    <!-- 品牌条 -->
    <div class="brand block">
      <span class="ind-label">ANNIVERSARY · 纪念日</span>
      <span class="brand-status"><IndLed color="green" :size="9" /> 已同步</span>
    </div>

    <section class="block head-row">
      <IndSectionTitle label="我们的重要日子" :led="true" />
      <button class="add-btn" @click="openCreate">＋ 新增纪念日</button>
    </section>

    <section class="block">
      <div v-if="items.length" class="anniv-grid">
        <div v-for="a in items" :key="a.id" class="anniv-card" :class="{ pop: poppingId === a.id }">
          <div class="ac-top">
            <component :is="typeMeta(a.anniversaryType).icon" class="ac-type-ico" :size="20" />
            <span class="ac-name">{{ a.name }}</span>
            <NTag v-if="a.isYearly" size="small" type="primary" round class="ac-yearly">每年</NTag>
            <NTag v-else size="small" :bordered="true" class="ac-once">一次性</NTag>
          </div>

          <div class="ac-meta">
            <span>目标日 {{ fmtDate(a.targetDate) }}</span>
            <span class="dot-sep">·</span>
            <span>提前 {{ a.remindDays }} 天提醒</span>
          </div>

          <div class="ac-next">
            <template v-if="a.nextOccurrence">
              下次 <b>{{ fmtDate(a.nextOccurrence) }}</b>
              <span class="ac-left">还有 <GradientText tag="span" class="ac-days">{{ a.daysLeft }}</GradientText> 天</span>
            </template>
            <template v-else>
              <span class="ac-expired">这一天已经过去啦</span>
            </template>
          </div>

          <div class="ac-actions">
            <button class="ac-btn" @click="openEdit(a)">编辑</button>
            <n-popconfirm
              positive-text="删除"
              negative-text="取消"
              @positive-click="onDelete(a)"
            >
              <template #trigger>
                <button class="ac-btn danger">删除</button>
              </template>
              确定删除「{{ a.name }}」吗？相关提醒也会一并移除。
            </n-popconfirm>
          </div>
        </div>
      </div>
      <IndEmpty v-else title="还没有纪念日" desc="点「新增纪念日」，把恋爱纪念日、生日、初见都记下来，每年自动提醒" />
    </section>

    <!-- 新增 / 编辑 弹窗 -->
    <n-modal
      v-model:show="showForm"
      class="anniv-modal"
      preset="card"
      :title="editingId ? '编辑纪念日' : '新增纪念日'"
      style="width: 92%; max-width: 480px;"
    >
      <n-form ref="formRef" :model="form" label-placement="top">
        <n-form-item label="名称" :rule="requiredRule('给纪念日起个名字吧～')">
          <n-input v-model:value="form.name" placeholder="例如：恋爱纪念日 / 我的生日 / 初次相遇" maxlength="30" show-count />
        </n-form-item>
        <n-form-item label="类型">
          <n-select v-model:value="form.anniversaryType" :options="typeOptions" />
        </n-form-item>
        <n-form-item label="目标日期" :rule="dateRule('选个目标日期吧～')">
          <n-date-picker v-model:value="form.dateTs" type="date" clearable style="width: 100%" />
        </n-form-item>
        <n-form-item label="提前提醒（天）">
          <n-select v-model:value="form.remindDays" :options="remindOptions" />
        </n-form-item>
        <n-form-item label="是否每年重复">
          <div class="yearly-row">
            <n-switch v-model:value="form.isYearly" />
            <span class="yearly-hint">{{ form.isYearly ? '每年同一天自动提醒' : '仅此一次，过期不再提醒' }}</span>
          </div>
        </n-form-item>
        <n-form-item label="封面图（可选）">
          <ImageField v-model="form.coverImage" />
        </n-form-item>
      </n-form>
      <template #footer>
        <div class="modal-foot">
          <n-button @click="showForm = false">取消</n-button>
          <n-button type="primary" :loading="submitting" @click="submit">保存</n-button>
        </div>
      </template>
    </n-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, type Component } from 'vue';
import { Heart, Cake, Handshake, Sparkles } from 'lucide-vue-next';
import { NButton, NModal, NForm, NFormItem, NInput, NSelect, NDatePicker, NSwitch, NTag, NPopconfirm } from 'naive-ui';
import type { AnniversaryDto, AnniversaryReq } from '@/types';
import {
  listAnniversaries, createAnniversary, updateAnniversary, deleteAnniversary,
} from '@/api/anniversary';
import { useRealtime } from '@/composables/useRealtime';
import { useStaggerEnter } from '@/composables/useAnimation';
import IndSectionTitle from '@/components/industrial/IndSectionTitle.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndLed from '@/components/industrial/IndLed.vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import ImageField from '@/components/Common/ImageField.vue';
import GradientText from '@/components/Common/GradientText.vue';
import { feedback } from '@/utils/feedback';
import { requiredRule, dateRule } from '@/utils/formRules';

const { onSync } = useRealtime();
const loading = ref(true);
const items = ref<AnniversaryDto[]>([]);
const container = ref<HTMLElement>();
const formRef = ref();
const showForm = ref(false);
const submitting = ref(false);
const editingId = ref<number | null>(null);
const poppingId = ref<number | null>(null);

const typeOptions = [
  { label: '恋爱纪念日', value: 1 },
  { label: '生日', value: 2 },
  { label: '初见', value: 3 },
  { label: '自定义', value: 4 },
];
const remindOptions = [
  { label: '当天（不提前）', value: 0 },
  { label: '提前 1 天', value: 1 },
  { label: '提前 3 天', value: 3 },
  { label: '提前 7 天', value: 7 },
  { label: '提前 15 天', value: 15 },
];
const typeIcon: Record<number, Component> = { 1: Heart, 2: Cake, 3: Handshake, 4: Sparkles };
function typeMeta(t: number) {
  return { icon: typeIcon[t] ?? Sparkles, label: typeOptions.find((o) => o.value === t)?.label ?? '自定义' };
}

const emptyForm = () => ({
  name: '', anniversaryType: 1, dateTs: null as number | null, remindDays: 3, isYearly: true, coverImage: '',
});
const form = ref(emptyForm());

async function load() {
  loading.value = true;
  try {
    const r = await listAnniversaries(1, 100);
    items.value = r.items.sort((a, b) => (a.nextOccurrence ?? '9999').localeCompare(b.nextOccurrence ?? '9999'));
  } catch { /* 拦截器已提示 */ }
  finally { loading.value = false; }
}

function fmtDate(s?: string | null) {
  if (!s) return '—';
  const d = new Date(s);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function openCreate() {
  editingId.value = null;
  form.value = emptyForm();
  showForm.value = true;
}
function openEdit(a: AnniversaryDto) {
  editingId.value = a.id;
  form.value = {
    name: a.name,
    anniversaryType: a.anniversaryType,
    dateTs: new Date(a.targetDate).getTime(),
    remindDays: a.remindDays,
    isYearly: a.isYearly,
    coverImage: a.coverImage ?? '',
  };
  showForm.value = true;
}

function toReq(): AnniversaryReq {
  return {
    name: form.value.name.trim(),
    anniversaryType: form.value.anniversaryType,
    targetDate: form.value.dateTs ? new Date(form.value.dateTs).toISOString().slice(0, 10) : '',
    remindDays: form.value.remindDays,
    isYearly: form.value.isYearly,
    coverImage: form.value.coverImage.trim() || undefined,
  };
}

async function submit() {
  try {
    await formRef.value?.validate();
  } catch { return; }
  submitting.value = true;
  try {
    if (editingId.value) {
      const updated = await updateAnniversary(editingId.value, toReq());
      const i = items.value.findIndex((x) => x.id === updated.id);
      if (i >= 0) items.value[i] = updated;
      feedback.updated('纪念日');
    } else {
      const created = await createAnniversary(toReq());
      items.value.unshift(created);
      feedback.created('纪念日');
    }
    showForm.value = false;
  } finally { submitting.value = false; }
}

async function onDelete(a: AnniversaryDto) {
  try {
    await deleteAnniversary(a.id);
    items.value = items.value.filter((x) => x.id !== a.id);
    poppingId.value = a.id;
    setTimeout(() => (poppingId.value = null), 300);
    feedback.deleted('纪念日');
  } catch { /* 忽略 */ }
}

useStaggerEnter(container, '.block', { stagger: 0.1, y: 16 });

onMounted(async () => {
  await load();
  loading.value = false;
  onSync('anniversary', () => load());
});
</script>

<style scoped>
.anniv-page { max-width: 880px; margin: 0 auto; }
.brand {
  display: flex; align-items: center; gap: 14px; padding: 12px 16px; margin-bottom: 8px;
  background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg);
  box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04), 0 10px 28px -10px rgba(122, 100, 98, 0.16);
}
.brand-status {
  margin-left: auto; display: inline-flex; align-items: center; gap: 6px;
  font-size: 12px; font-weight: 500;
  color: var(--color-ink-2);
  padding: 4px 12px; border-radius: 999px;
  background: var(--color-surface-2); border: 1px solid var(--color-border);
}
.ind-label { font-family: var(--font-mono); font-weight: 500; letter-spacing: 0.1em; font-size: 13px; color: var(--color-ink); }

.head-row { display: flex; align-items: center; justify-content: space-between; }
.add-btn {
  border: 1px solid var(--color-border); cursor: pointer; padding: 9px 16px; border-radius: 999px;
  color: var(--color-rose); font-size: 13px; background: var(--color-rose-soft);
  box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04);
  transition: all var(--dur-micro) var(--ease-love);
}
.add-btn:active { transform: scale(0.97); }

.block { margin: 22px 0; }

.anniv-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 16px; }
.anniv-card {
  position: relative; padding: 18px 18px 14px; border-radius: var(--radius-lg);
  background: var(--color-surface); border: 1px solid var(--color-border);
  box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04), 0 10px 28px -10px rgba(122, 100, 98, 0.16);
  transition: transform var(--dur-pop) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love);
}
.anniv-card:hover { transform: translateY(-3px); box-shadow: 0 4px 12px rgba(31, 41, 55, 0.06), 0 18px 44px -12px rgba(122, 100, 98, 0.22); }
.anniv-card.pop { animation: acPop 0.3s var(--ease-love); }
@keyframes acPop { 0% { opacity: 1; } 50% { opacity: 0.3; } 100% { opacity: 1; } }

.ac-top { display: flex; align-items: center; gap: 8px; margin-bottom: 12px; }
.ac-type-ico { color: var(--color-rose); }
.ac-name { font-size: 15px; font-weight: 700; color: var(--color-ink); }
.ac-yearly { margin-left: auto; }
.ac-once { margin-left: auto; color: var(--color-ink-3); }

.ac-meta { font-size: 12px; color: var(--color-ink-2); margin-bottom: 8px; font-family: var(--font-mono); }
.dot-sep { margin: 0 6px; color: var(--color-ink-3); }

.ac-next { font-size: 13px; color: var(--color-ink); margin-bottom: 14px; }
.ac-next b { color: var(--color-accent); }
.ac-left { margin-left: 8px; }
.ac-days { font-weight: 800; font-size: 16px; }
.ac-expired { color: var(--color-ink-3); }

.ac-actions { display: flex; gap: 10px; }
.ac-btn {
  flex: 1; border: 1px solid var(--color-border); cursor: pointer; padding: 8px 0; border-radius: var(--radius-md); font-size: 13px;
  background: var(--color-surface-2); color: var(--color-ink-2);
  box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04);
  transition: all var(--dur-micro) var(--ease-love);
}
.ac-btn:active { transform: scale(0.98); }
.ac-btn:hover { color: var(--color-rose); border-color: var(--color-rose-soft); background: var(--color-rose-soft); }
.ac-btn.danger { color: var(--color-rose); }

.yearly-row { display: flex; align-items: center; gap: 12px; }
.yearly-hint { font-size: 12px; color: var(--color-ink-3); }
.modal-foot { display: flex; justify-content: flex-end; gap: 10px; }
</style>
