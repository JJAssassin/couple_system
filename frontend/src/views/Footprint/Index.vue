<template>
  <IndSkeleton v-if="loading" variant="grid" :rows="6" :columns="3" />
  <div v-else class="footprint-page" ref="container">
    <!-- 品牌条 -->
    <div class="brand block">
      <h1 class="ind-label">FOOTPRINT · 足迹</h1>
      <span class="brand-status"><IndLed color="green" :size="9" /> 记录中</span>
    </div>

    <section class="block head-row">
      <IndSectionTitle label="我们的小确幸" :led="true" />
      <button class="add-btn" v-press-bounce @click="openAdd">＋ 新增足迹</button>
    </section>

    <section class="block">
      <div v-if="items.length" class="fp-grid">
        <div
          v-for="f in items"
          :key="f.id"
          class="fp-card"
          :class="{ pop: poppingId === f.id, reached: f.targetCount != null && f.count >= f.targetCount }"
          @click="onIncrement(f)"
        >
          <div class="fp-actions">
            <button class="edit-float" @click.stop="openEdit(f)" title="修改"><Pencil :size="14" /></button>
            <n-popconfirm
              positive-text="删除"
              negative-text="取消"
              @positive-click="onDelete(f)"
            >
              <template #trigger>
                <button class="del-float" @click.stop title="删除"><X :size="16" /></button>
              </template>
              确定删除「{{ f.title }}」吗？计数记录会一并清除。
            </n-popconfirm>
          </div>
          <div class="fp-emoji">{{ f.emoji }}</div>
          <div class="fp-title">{{ f.title }}</div>
          <div v-if="f.description" class="fp-desc">{{ f.description }}</div>
          <div class="fp-count">{{ f.count }}</div>
          <div class="fp-label">次</div>
          <div v-if="f.targetCount != null" class="fp-progress">
            <div class="fp-bar"><span :style="{ width: progPct(f) + '%' }" :class="{ done: f.count >= f.targetCount }" /></div>
            <div class="fp-target" :class="{ done: f.count >= f.targetCount }">
              {{ f.count }} / {{ f.targetCount }} <template v-if="f.count >= f.targetCount">已达成</template>
              <template v-else>目标</template>
            </div>
          </div>
          <div class="fp-time" v-if="f.lastIncrementTime">最近 · {{ fmt(f.lastIncrementTime) }}</div>
          <div class="fp-time" v-else>还没记录过</div>
        </div>
      </div>
      <IndEmpty v-else title="还没有足迹" desc="点「新增足迹」，把抱抱、亲亲、一起看过的电影都变成可 +1 的小确幸吧" />
    </section>

    <!-- 新增 / 编辑弹窗 -->
    <LoveSheet v-model="showForm" :title="editingId == null ? '新增足迹' : '修改足迹'">
      <LoveInput
        v-model="form.title"
        label="名称"
        placeholder="例如：抱抱 / 亲亲 / 一起看的电影"
        :maxlength="30"
        :counter="true"
        :invalid="titleInvalid"
      />
      <div class="lf-field">
        <span class="lf-label">图标 Emoji</span>
        <div class="emoji-pick">
          <button
            v-for="e in emojiPresets"
            :key="e"
            class="emoji-chip"
            :class="{ on: form.emoji === e }"
            @click="form.emoji = e"
          >{{ e }}</button>
          <input v-model="form.emoji" placeholder="✨" maxlength="4" class="emoji-input" />
        </div>
      </div>
      <LoveInput
        v-model="form.targetCount"
        label="目标次数（可选）"
        type="number"
        inputmode="numeric"
        placeholder="留空表示不设目标"
      />
      <LoveTextarea
        v-model="form.description"
        label="说明（可选）"
        placeholder="比如：这个月要一起看 10 部电影"
        :maxlength="200"
        :counter="true"
      />
      <template #footer>
        <LoveSaveBar
          :loading="submitting"
          :success="saved"
          cancel-text="取消"
          :save-text="editingId == null ? '创建' : '保存'"
          @cancel="showForm = false"
          @save="submit"
        />
      </template>
    </LoveSheet>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { NPopconfirm } from 'naive-ui';
import { Pencil, X } from 'lucide-vue-next';
import { LoveSheet, LoveInput, LoveTextarea, LoveSaveBar } from '@/components/loveform';
import type { FootprintDto } from '@/types';
import { listFootprints, createFootprint, deleteFootprint, incrementFootprint, updateFootprint } from '@/api/footprint';
import { useRealtime, overlaySyncMap } from '@/composables/useRealtime';
import { useStaggerEnter } from '@/composables/useAnimation';
import { useSyncSettle } from '@/composables/useSyncSettle';
import IndSectionTitle from '@/components/industrial/IndSectionTitle.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndLed from '@/components/industrial/IndLed.vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import { feedback } from '@/utils/feedback';

const { useModuleSync } = useRealtime();
const loading = ref(true);
const items = ref<FootprintDto[]>([]);
const container = ref<HTMLElement>();
const showForm = ref(false);
const submitting = ref(false);
const saved = ref(false);
const titleInvalid = ref(false);
const poppingId = ref<number | null>(null);
const editingId = ref<number | null>(null);
const form = ref({ title: '', emoji: '✨', targetCount: '' as string, description: '' });
const emojiPresets = ['✨', '🤗', '💋', '💍', '🎬', '🍜', '🌟', '📷', '☕', '🌙'];

function resetForm() {
  form.value = { title: '', emoji: '✨', targetCount: '', description: '' };
  editingId.value = null;
}

function openAdd() {
  resetForm();
  saved.value = false;
  titleInvalid.value = false;
  showForm.value = true;
}

function openEdit(f: FootprintDto) {
  editingId.value = f.id;
  form.value = {
    title: f.title,
    emoji: f.emoji,
    targetCount: f.targetCount != null ? String(f.targetCount) : '',
    description: f.description ?? '',
  };
  saved.value = false;
  titleInvalid.value = false;
  showForm.value = true;
}

function progPct(f: FootprintDto) {
  if (f.targetCount == null || f.targetCount <= 0) return 0;
  return Math.min(100, Math.round((f.count / f.targetCount) * 100));
}

async function load() {
  loading.value = true;
  try {
    items.value = await listFootprints();
  } catch { /* 拦截器已提示 */ }
  finally { loading.value = false; }
}

function fmt(s: string) {
  const d = new Date(s);
  return `${d.getMonth() + 1}/${d.getDate()} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
}

async function onIncrement(f: FootprintDto) {
  poppingId.value = f.id;
  setTimeout(() => (poppingId.value = null), 420);
  try {
    const updated = await incrementFootprint(f.id);
    const i = items.value.findIndex((x) => x.id === f.id);
    if (i >= 0) items.value[i] = updated;
  } catch { /* 忽略 */ }
}

async function onDelete(f: FootprintDto) {
  try {
    await deleteFootprint(f.id);
    items.value = items.value.filter((x) => x.id !== f.id);
    feedback.deleted('足迹');
  } catch { /* 忽略 */ }
}

async function submit() {
  if (!form.value.title.trim()) {
    titleInvalid.value = true;
    feedback.warn('给足迹起个名字吧～');
    return;
  }
  submitting.value = true;
  saved.value = false;
  const payload = {
    title: form.value.title.trim(),
    emoji: form.value.emoji || '✨',
    targetCount: form.value.targetCount ? Number(form.value.targetCount) : null,
    description: form.value.description.trim() || null,
  };
  try {
    if (editingId.value == null) {
      const created = await createFootprint(payload);
      items.value.unshift(created);
      feedback.created('足迹');
    } else {
      const updated = await updateFootprint(editingId.value, payload);
      const i = items.value.findIndex((x) => x.id === editingId.value);
      if (i >= 0) items.value[i] = updated;
      feedback.updated('足迹');
    }
    saved.value = true;
    window.setTimeout(() => { showForm.value = false; resetForm(); }, 680);
  } finally { submitting.value = false; }
}

useStaggerEnter(container, '.block', { stagger: 0.1, y: 16 });

onMounted(async () => {
  await load();
  loading.value = false;
  useModuleSync('footprint', { items, getId: i => i.id, load, map: overlaySyncMap });
  // 伴侣新增小确幸时，足迹卡错落入场
  useSyncSettle('footprint', container, items, '.fp-card');
});
</script>

<style scoped>
.footprint-page { max-width: 880px; margin: 0 auto; }
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
.ind-label { font-family: var(--font-mono); font-weight: 500; letter-spacing: 0.1em; font-size: 13px; color: var(--color-ink); margin: 0; }

.head-row { display: flex; align-items: center; justify-content: space-between; }
.add-btn {
  border: 1px solid var(--color-border); cursor: pointer; padding: 9px 16px; border-radius: 999px;
  color: var(--color-rose); font-size: 13px; background: var(--color-rose-soft);
  box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04);
  transition: all var(--dur-micro) var(--ease-love);
}
.add-btn:active { transform: scale(0.97); }

.block { margin: 22px 0; }

.fp-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(140px, 1fr)); gap: 16px; }
.fp-card {
  position: relative; display: flex; flex-direction: column; align-items: center; justify-content: center;
  padding: 26px 16px 20px; cursor: pointer; text-align: center; border-radius: var(--radius-lg);
  background: var(--color-surface); border: 1px solid var(--color-border);
  box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04), 0 10px 28px -10px rgba(122, 100, 98, 0.16);
  transition: transform var(--dur-pop) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love);
  user-select: none;
}
.fp-card:hover { transform: translateY(-3px); box-shadow: 0 4px 12px rgba(31, 41, 55, 0.06), 0 18px 44px -12px rgba(122, 100, 98, 0.22); }
.fp-card:active { transform: scale(0.97); }
.fp-card.pop { animation: fpPop 0.42s var(--ease-love); }
@keyframes fpPop {
  0% { transform: scale(1); }
  35% { transform: scale(1.08); }
  100% { transform: scale(1); }
}
.reduce-motion .fp-card.pop { animation: none; }
.fp-emoji { font-size: 38px; margin-bottom: 8px; }
.fp-title { font-size: 14px; font-weight: 600; color: var(--color-ink); margin-bottom: 10px; }
.fp-desc { font-size: 11px; color: var(--color-ink-3); margin-bottom: 8px; line-height: 1.4; padding: 0 4px; }
.fp-count { font-size: 42px; font-weight: 800; color: var(--color-accent); line-height: 1; text-shadow: 0 0 16px rgba(255, 111, 125, 0.3); }
.fp-label { font-size: 12px; color: var(--color-ink-3); margin-top: 4px; }
.fp-time { font-size: 11px; color: var(--color-ink-3); margin-top: 10px; font-family: var(--font-mono); }
.fp-progress { width: 100%; margin-top: 8px; }
.fp-bar { width: 100%; height: 6px; border-radius: 999px; background: var(--color-surface-2); overflow: hidden; border: 1px solid var(--color-border); }
.fp-bar span { display: block; height: 100%; border-radius: 999px; background: var(--color-ink-soft); transition: width var(--dur-pop) var(--ease-love); }
.fp-bar span.done { background: linear-gradient(90deg, #43d17a, #2fb56a); }
.fp-target { font-size: 11px; color: var(--color-ink-3); margin-top: 4px; font-family: var(--font-mono); }
.fp-target.done { color: #2fb56a; font-weight: 600; }
.fp-card.reached { border-color: rgba(67, 209, 122, 0.55); box-shadow: 0 0 0 2px rgba(67, 209, 122, 0.45), 0 1px 2px rgba(31, 41, 55, 0.04), 0 10px 28px -10px rgba(122, 100, 98, 0.16); }
.fp-actions {
  position: absolute; top: 6px; right: 7px; display: flex; gap: 2px; align-items: center;
}
.del-float {
  background: none; border: none;
  font-size: 16px; color: var(--color-ink-3); cursor: pointer; line-height: 1; padding: 2px 5px;
  border-radius: 8px; transition: color var(--dur-micro);
}
.edit-float {
  background: none; border: none;
  font-size: 14px; color: var(--color-ink-3); cursor: pointer; line-height: 1; padding: 3px 5px 1px;
  border-radius: 8px; transition: color var(--dur-micro);
}
.del-float:hover { color: var(--color-accent); }
.edit-float:hover { color: var(--color-accent); }

.emoji-pick { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
.emoji-chip {
  width: 40px; height: 40px; font-size: 20px; border-radius: var(--radius-md); cursor: pointer;
  border: 1px solid var(--color-border); background: var(--color-surface-2); color: var(--color-ink);
  box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04);
  transition: all var(--dur-micro) var(--ease-love);
}
.emoji-chip:hover { border-color: var(--color-rose-soft); background: var(--color-rose-soft); }
.emoji-chip.on { border-color: var(--color-rose); color: var(--color-rose); background: var(--color-rose-soft); }
.emoji-input {
  width: 84px; height: 40px; padding: 0 10px; font-size: 16px; text-align: center;
  border: 1px solid var(--color-border); border-radius: var(--radius-md);
  background: var(--color-surface-2); color: var(--color-ink);
  transition: border-color var(--dur-micro) var(--ease-love), box-shadow var(--dur-micro) var(--ease-love);
}
.emoji-input:focus { outline: none; border-color: var(--color-rose); box-shadow: 0 0 0 3px var(--color-rose-soft); }
</style>
