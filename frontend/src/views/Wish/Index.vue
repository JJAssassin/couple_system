<template>
  <div class="wish-page" ref="container">
    <!-- 头部 -->
    <header class="page-head">
      <div class="head-left">
        <h1>愿望清单</h1>
        <IndProgressRing :value="rate" :size="62" :stroke="8" sublabel="完成率" />
      </div>
      <n-button type="primary" round v-press-bounce @click="openAdd">+ 加愿望</n-button>
    </header>

    <!-- 分类 + 状态：单排筛选条 -->
    <div class="filter-bar">
      <n-tabs v-model:value="activeTab" type="segment" class="type-tabs">
        <n-tab-pane :name="1" tab="共同心愿" />
        <n-tab-pane :name="2" tab="礼物心愿" />
        <n-tab-pane :name="3" tab="成长目标" />
      </n-tabs>
      <n-select
        v-model:value="statusFilter"
        :options="statusFilterOptions"
        size="small"
        class="status-select"
      />
    </div>

    <!-- 列表：08 骨架落位（加载骨架 → 内容，同尺寸不跳动） -->
    <SkeletonSettle :loading="loading">
      <template #skeleton>
        <IndSkeleton variant="grid" :rows="6" :columns="3" />
      </template>
      <IndEmpty
        v-if="!filtered.length"
        title="愿望清单还是空的"
        :desc="`这里还没有${tabLabel}～去添加一个吧`"
        actionText="加个愿望"
        @action="openAdd"
      />
      <div v-else ref="listEl">
        <draggable
          v-model="wishDrag"
          item-key="id"
          class="cards"
          handle=".drag-handle"
          :animation="180"
          :force-fallback="true"
          ghost-class="drag-ghost"
          @end="onWishReorder"
        >
          <template #item="{ element: w }">
           <div class="wish-cell">
            <!-- 已完成：外层 SwipeCard 向左滑「抽走」= 归档（可逆，非删除） -->
            <SwipeCard v-if="w.status === 3" class="wish-card" :threshold="90" hint="归档" hint-color="#E8A87C" @dismiss="archiveWish(w)">
            <FlipCard
              :model-value="!!flips[w.id]"
              @update:model-value="(v) => setFlip(w.id, v)"
              interactive
              class="wish done wish-card"
            >
              <template #front>
                <div class="wish-face">
                  <div class="wish-top">
                    <button class="drag-handle" type="button" aria-label="拖动排序" @pointerdown.stop @click.stop>
                      <GripVertical :size="16" />
                    </button>
                    <span class="wish-title title-clamp">{{ w.title }}</span>
                    <n-tag :type="statusMap[w.status]?.type ?? 'default'" size="small" round>{{ statusMap[w.status]?.label ?? '未知' }}</n-tag>
                  </div>
                  <div class="wish-meta sub-text">
                    <span>优先级 {{ '★'.repeat(w.priority) || '—' }}</span>
                    <span v-if="w.expectTime">期望 {{ fmt(w.expectTime) }}</span>
                    <span v-if="w.claimUserName">认领人：{{ w.claimUserName }}</span>
                  </div>
                  <div class="progress anim"><div class="bar" :style="{ width: progressOf(w) + '%' }"></div></div>
                  <div class="wish-actions" @click.stop>
                    <n-button size="small" tertiary @click="openEdit(w)">编辑</n-button>
                    <n-popconfirm @positive-click="onDelete(w.id)">
                      <template #trigger>
                        <n-button size="small" tertiary type="error">删除</n-button>
                      </template>
                      确定删除这个愿望吗？
                    </n-popconfirm>
                    <span class="wish-flip-hint">← 左滑归档 · 点击翻面 →</span>
                  </div>
                </div>
              </template>
              <template #back>
                <div class="wish-face">
                  <div class="wish-back-label">愿望详情</div>
                  <p class="wish-desc-full">{{ w.description || '（暂无描述）' }}</p>
                  <p v-if="w.completeRemark" class="wish-complete sub-text">{{ w.completeRemark }}</p>
                  <div class="wish-back-hint">点击返回正面</div>
                </div>
              </template>
            </FlipCard>
          </SwipeCard>

          <!-- 进行中 / 未开始：卡片翻面看详情 -->
          <FlipCard
            v-else
            :model-value="!!flips[w.id]"
            @update:model-value="(v) => setFlip(w.id, v)"
            interactive
            class="wish wish-card"
            :class="{ done: w.status === 3 }"
          >
            <template #front>
              <div class="wish-face">
                <div class="wish-top">
                  <span class="wish-title title-clamp">{{ w.title }}</span>
                  <n-tag :type="statusMap[w.status]?.type ?? 'default'" size="small" round>{{ statusMap[w.status]?.label ?? '未知' }}</n-tag>
                </div>
                <div class="wish-meta sub-text">
                  <span>优先级 {{ '★'.repeat(w.priority) || '—' }}</span>
                  <span v-if="w.expectTime">期望 {{ fmt(w.expectTime) }}</span>
                  <span v-if="w.claimUserName">认领人：{{ w.claimUserName }}</span>
                </div>
                <div class="progress" :class="{ anim: w.status === 3 }"><div class="bar" :style="{ width: progressOf(w) + '%' }"></div></div>
                <div class="wish-actions" @click.stop>
                  <n-button v-if="w.wishType === 2 && !w.claimUserId" size="small" tertiary type="warning" @click="onClaim(w)">认领</n-button>
                  <n-button v-if="w.status !== 3" size="small" tertiary type="success" @click="openComplete(w)">标记完成</n-button>
                  <n-button size="small" tertiary @click="openEdit(w)">编辑</n-button>
                  <n-popconfirm @positive-click="onDelete(w.id)">
                    <template #trigger>
                      <n-button size="small" tertiary type="error">删除</n-button>
                    </template>
                    确定删除这个愿望吗？
                  </n-popconfirm>
                </div>
              </div>
            </template>
            <template #back>
              <div class="wish-face">
                <div class="wish-back-label">愿望详情</div>
                <p class="wish-desc-full">{{ w.description || '（暂无描述）' }}</p>
                <div class="wish-back-hint">点击返回正面</div>
              </div>
            </template>
          </FlipCard>
           </div>
        </template>
        </draggable>
      </div>
    </SkeletonSettle>

    <IndPager
      v-if="filtered.length"
      mode="more"
      :page="1"
      :page-size="12"
      :total="filtered.length"
      :loading="loading"
      :has-more="hasMore"
      @load-more="loadMore"
    />

    <!-- 新增 / 编辑 愿望：iOS 风表单 -->
    <LoveSheet v-model="showForm" :title="editing ? '编辑愿望' : '加个愿望'" subtitle="一起想做的事、想要的礼物或目标">
      <div class="wish-form">
        <LoveSegmented v-model="form.wishType" label="类型" :options="typeOptions" />
        <LoveInput
          v-model="form.title"
          label="标题"
          placeholder="想一起做的事 / 想要的礼物 / 想达成的目标"
          :maxlength="120"
          counter
          clearable
          :invalid="titleInvalid"
          @update:modelValue="titleInvalid = false"
        />
        <LoveTextarea
          v-model="form.description"
          label="描述"
          placeholder="补充说明（可选）"
          :rows="3"
          :maxlength="1000"
        />
        <LoveDateField v-model="expectTs" label="期望时间" />
        <LoveSegmented v-model="form.priority" label="优先级" :options="priorityOptions" />
        <LoveSegmented v-model="form.status" label="状态" :options="statusOptions" />
      </div>
      <template #footer>
        <LoveSaveBar
          :loading="saving"
          :success="saved"
          cancel-text="取消"
          :save-text="editing ? '保存' : '添加'"
          @cancel="showForm = false"
          @save="submitForm"
        />
      </template>
    </LoveSheet>

    <!-- 标记完成：iOS 风表单 -->
    <LoveSheet v-model="showComplete" title="完成感悟" subtitle="记下这一刻的心情">
      <div class="wish-form">
        <LoveTextarea
          v-model="completeForm.completeRemark"
          label="完成感悟"
          placeholder="写下这一刻的心情～"
          :rows="3"
          :maxlength="1000"
        />
        <div class="lf-field">
          <label class="lf-label">完成照片（可选）</label>
          <ImageField v-model="completeForm.completeImage" />
        </div>
      </div>
      <template #footer>
        <LoveSaveBar
          :loading="savingComplete"
          :success="savedComplete"
          cancel-text="取消"
          save-text="完成啦"
          @cancel="showComplete = false"
          @save="submitComplete"
        />
      </template>
    </LoveSheet>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from 'vue';
import {
  NButton, NTag, NPopconfirm, NTabs, NTabPane,
} from 'naive-ui';
import type { WishDto, WishReq } from '@/types';
import {
  listWish, createWish, updateWish, deleteWish, claimWish, completeWish, reorderWishes,
} from '@/api/wish';
import { useNotifyStore } from '@/store/notifyStore';
import { useStaggerEnter } from '@/composables/useAnimation';
import { useRealtime, overlaySyncMap } from '@/composables/useRealtime';
import { useSyncSettle } from '@/composables/useSyncSettle';
import IndProgressRing from '@/components/industrial/IndProgressRing.vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import ImageField from '@/components/Common/ImageField.vue';
import { SkeletonSettle, FlipCard, SwipeCard } from '@/interactions';
import draggable from 'vuedraggable';
import { GripVertical } from 'lucide-vue-next';
import {
  LoveSheet, LoveInput, LoveTextarea, LoveSegmented, LoveDateField, LoveSaveBar,
} from '@/components/loveform';
import { feedback } from '@/utils/feedback';
import { fireConfetti } from '@/composables/useConfetti';

const notify = useNotifyStore();
const loading = ref(true);
const container = ref<HTMLElement>();
const listEl = ref<HTMLElement>();

const activeTab = ref<number>(1);
const statusFilter = ref<string>('all');
const wishes = ref<WishDto[]>([]);

// 卡片翻面状态（09 卡片翻面）：逐卡记录是否翻到背面
const flips = reactive<Record<number, boolean>>({});
function setFlip(id: number, v: boolean) {
  flips[id] = v;
}

const tabLabel = computed(() => ['', '共同心愿', '礼物心愿', '成长目标'][activeTab.value]);
const rate = computed(() => {
  if (!wishes.value.length) return 0;
  const done = wishes.value.filter((w) => w.status === 3 || w.status === 4).length;
  return Math.round((done / wishes.value.length) * 100);
});
const filtered = computed(() =>
  wishes.value.filter((w) => {
    if (w.wishType !== activeTab.value) return false;
    if (statusFilter.value === 'all') return w.status !== 4; // 已归档不进默认列表，左滑归档即从列表抽走
    if (statusFilter.value === 'done') return w.status === 3 || w.status === 4;
    return w.status === 1 || w.status === 2;
  })
);

const displayCount = ref(12);
const displayList = computed(() => filtered.value.slice(0, displayCount.value));
const hasMore = computed(() => displayCount.value < filtered.value.length);
function loadMore() {
  displayCount.value += 12;
}
watch(filtered, () => {
  displayCount.value = 12;
});

// 拖拽排序：维护一份可被 vuedraggable 直接改写的本地列表，@end 时把当前可见顺序回写后端；
// 与后端「按 Status 分组、组内按 SortOrder」一致，跨状态拖拽会在各自分组内重排。
const wishDrag = ref<WishDto[]>([]);
watch(displayList, (v) => { wishDrag.value = [...v]; }, { immediate: true });

async function onWishReorder() {
  const ids = wishDrag.value.map((w) => w.id);
  if (ids.length < 2) return;
  try {
    await reorderWishes(ids);
  } catch {
    feedback.warn('排序保存失败，已恢复顺序');
  } finally {
    await load();
  }
}

const typeOptions = [
  { label: '共同心愿', value: 1 },
  { label: '礼物心愿', value: 2 },
  { label: '成长目标', value: 3 },
];
const statusOptions = [
  { label: '未开始', value: 1 },
  { label: '进行中', value: 2 },
  { label: '已完成', value: 3 },
  { label: '已归档', value: 4 },
];
const statusFilterOptions = [
  { label: '全部', value: 'all' },
  { label: '进行中', value: 'active' },
  { label: '已完成', value: 'done' },
];
const statusMap: Record<number, { label: string; type: 'default' | 'info' | 'success' | 'warning' }> = {
  1: { label: '未开始', type: 'default' },
  2: { label: '进行中', type: 'info' },
  3: { label: '已完成', type: 'success' },
  4: { label: '已归档', type: 'warning' },
};

function progressOf(w: WishDto) {
  if (w.status === 3 || w.status === 4) return 100;
  if (w.status === 2) return 50;
  return 0;
}
function fmt(s: string) {
  const d = new Date(s);
  return `${d.getMonth() + 1}月${d.getDate()}日`;
}

// ---- 表单（新增 / 编辑）----
const showForm = ref(false);
const editing = ref<WishDto | null>(null);
const expectTs = ref<number | null>(null);
const saving = ref(false);
const saved = ref(false);
const titleInvalid = ref(false);
const form = reactive<WishReq>({
  wishType: 1, title: '', description: undefined, expectTime: undefined, priority: 2, status: 1,
});

const priorityOptions = [
  { label: '低', value: 1 },
  { label: '中', value: 2 },
  { label: '高', value: 3 },
];

function resetForm() {
  Object.assign(form, {
    wishType: activeTab.value, title: '', description: undefined,
    expectTime: undefined, priority: 2, status: 1,
  });
  expectTs.value = null;
  titleInvalid.value = false;
  saving.value = false;
  saved.value = false;
}

function openAdd() {
  editing.value = null;
  resetForm();
  showForm.value = true;
}
function openEdit(w: WishDto) {
  editing.value = w;
  Object.assign(form, {
    wishType: w.wishType, title: w.title, description: w.description,
    priority: w.priority, status: w.status,
  });
  expectTs.value = w.expectTime ? new Date(w.expectTime).getTime() : null;
  titleInvalid.value = false;
  saving.value = false;
  saved.value = false;
  showForm.value = true;
}
async function submitForm() {
  if (!form.title.trim()) {
    titleInvalid.value = true;
    feedback.warn('给愿望起个标题吧～');
    return;
  }
  saving.value = true;
  try {
    form.expectTime = expectTs.value ? new Date(expectTs.value).toISOString() : undefined;
    if (editing.value) {
      await updateWish(editing.value.id, { ...form });
    } else {
      await createWish({ ...form });
    }
    saved.value = true;
    window.setTimeout(async () => {
      showForm.value = false;
      if (editing.value) feedback.updated('愿望');
      else feedback.created('愿望');
      await load();
    }, 720);
  } finally {
    saving.value = false;
  }
}

// ---- 完成 ----
const showComplete = ref(false);
const savingComplete = ref(false);
const savedComplete = ref(false);
const completeForm = reactive<{ id: number; completeRemark?: string; completeImage?: string }>({
  id: 0, completeRemark: undefined, completeImage: undefined,
});
function openComplete(w: WishDto) {
  Object.assign(completeForm, { id: w.id, completeRemark: w.completeRemark, completeImage: w.completeImage });
  savingComplete.value = false;
  savedComplete.value = false;
  showComplete.value = true;
}
async function submitComplete() {
  savingComplete.value = true;
  try {
    await completeWish({ ...completeForm });
    savedComplete.value = true;
    fireConfetti();
    window.setTimeout(async () => {
      showComplete.value = false;
      feedback.saved('愿望');
      await load();
    }, 720);
  } finally {
    savingComplete.value = false;
  }
}

async function onClaim(w: WishDto) {
  await claimWish(w.id);
  notify.success('已认领');
  await load();
}
async function onDelete(id: number) {
  await deleteWish(id);
  feedback.deleted('愿望');
  await load();
}

// 11 卡片抽走：已完成愿望向左滑「抽走」= 归档（status→4 已归档，可逆、非删除）
async function archiveWish(w: WishDto) {
  try {
    await updateWish(w.id, {
      wishType: w.wishType, title: w.title, description: w.description,
      expectTime: w.expectTime, priority: w.priority, status: 4,
    });
    feedback.saved('已归档愿望');
    await load();
  } catch {
    // 归档失败：保留原卡片，下次刷新自然恢复
  }
}

async function load() {
  loading.value = true;
  try {
    const p = await listWish({ page: 1, pageSize: 200 });
    wishes.value = p.items;
  } finally { loading.value = false; }
}

useStaggerEnter(container, '.wish-card', { stagger: 0.06, y: 14 });
// 实时融合：伴侣在别处新增/刷新愿望时，本端卡片错落入场（非自己操作、尊重降级）
useSyncSettle('wish', listEl, wishes, '.wish-card');
const { useModuleSync } = useRealtime();
onMounted(async () => {
  await load();
  useModuleSync('wish', { items: wishes, getId: i => i.id, load, map: overlaySyncMap });
});
</script>

<style scoped>
.wish-page { max-width: 960px; margin: 0 auto; }
.page-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 14px; }
.head-left { display: flex; align-items: center; gap: 16px; }
.page-head h1 { font-size: 22px; margin: 0; }
.filter-bar { display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap; margin-bottom: 18px; }
.type-tabs { flex: 1 1 320px; min-width: 0; }
.status-select { flex: 0 0 auto; width: 118px; }
@media (max-width: 520px) {
  .type-tabs { flex: 1 1 100%; }
  .status-select { width: 100%; }
}
.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 14px; }
/* vuedraggable #item 槽需单一根节点：用 .wish-cell 包裹 v-if/v-else 两分支；flex 让内部 FlipCard/SwipeCard 填满单元格并等高对齐 */
.wish-cell { display: flex; }
.wish-cell > .wish-card { width: 100%; }
.wish-top { display: flex; align-items: flex-start; justify-content: space-between; gap: 8px; }
/* 拖拽手柄：内联在标题左侧，仅在手柄上按下才触发排序（@pointerdown.stop 隔离 SwipeCard 左滑 / @click.stop 隔离 FlipCard 翻面） */
.drag-handle {
  flex: 0 0 auto;
  width: 24px; height: 24px; margin-top: 1px;
  display: grid; place-items: center;
  border: none; background: transparent; color: var(--color-ink-3);
  cursor: grab; border-radius: 7px; touch-action: none;
  opacity: 0.5; transition: opacity var(--dur-micro), background var(--dur-micro);
}
.drag-handle:hover { opacity: 1; background: var(--color-ink-soft); }
.drag-handle:active { cursor: grabbing; }
.drag-ghost { opacity: 0.35; }
.wish-title { font-size: 16px; font-weight: 500; color: var(--color-ink); }
.wish.done .wish-title { color: var(--color-ink-3); text-decoration: line-through; }
.wish-desc { margin: 8px 0 0; }
/* 09/11 翻面 & 抽走：卡片视觉落到 .wish-face（正反面共用，翻转不穿帮） */
.wish-face {
  padding: 16px;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: 16px;
  box-shadow: var(--shadow-card);
  height: 100%;
  display: flex;
  flex-direction: column;
  gap: 8px;
  transition: transform var(--dur-pop) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love);
}
html:not(.reduce-motion) .wish-face:hover {
  transform: translateY(-3px);
  box-shadow: var(--shadow-float);
}
.wish-back-label { font-weight: 600; color: var(--color-ink); }
.wish-desc-full { margin: 0; color: var(--color-ink-2); line-height: 1.6; white-space: pre-wrap; word-break: break-word; }
.wish-back-hint,
.wish-flip-hint { margin-top: auto; font-size: 12px; color: var(--color-ink-3); }
.wish-flip-hint { padding-top: 6px; }
.wish-meta { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 10px; }
.progress { height: 6px; border-radius: 999px; background: var(--color-ink-soft); margin: 12px 0; overflow: hidden; }
.progress .bar { height: 100%; border-radius: 999px; background: linear-gradient(90deg, var(--color-rose), var(--color-rose-hover)); transition: width var(--dur-page) var(--ease-love); }
.progress.anim .bar { animation: pop 0.5s var(--ease-love); }
@keyframes pop { 0% { filter: brightness(1.4); } 100% { filter: brightness(1); } }
.wish-actions { display: flex; flex-wrap: wrap; gap: 8px; }
.wish-complete { margin-top: 10px; color: var(--color-rose); }
.modal-foot { display: flex; justify-content: flex-end; gap: 10px; }

/* 移动端：单列 + 模态全屏 */
@media (max-width: 767px) {
  .cards { grid-template-columns: 1fr; }
}

</style>
