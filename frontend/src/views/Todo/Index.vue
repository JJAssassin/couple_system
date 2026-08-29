<template>
  <div class="todo-page" ref="container">
    <!-- 头部 -->
    <header class="page-head">
      <div class="head-left">
        <h1>待办清单</h1>
        <IndProgressRing :value="rate" :size="62" :stroke="8" sublabel="完成率" />
      </div>
      <n-button type="primary" round v-press-bounce @click="openAdd">+ 加待办</n-button>
    </header>

    <!-- 筛选条 -->
    <div class="filter-bar">
      <n-tabs v-model:value="activeTab" type="segment" class="cat-tabs">
        <n-tab-pane v-for="c in categoryTabs" :key="c.value" :name="c.value" :tab="c.label" />
      </n-tabs>
      <n-select
        v-model:value="statusFilter"
        :options="statusFilterOptions"
        size="small"
        class="status-select"
      />
    </div>

    <!-- 列表 -->
    <IndSkeleton v-if="loading" variant="grid" :rows="6" :columns="3" />
    <IndEmpty
      v-else-if="!filtered.length"
      title="待办清单还是空的"
      :desc="activeTab === 'all' ? '一起计划点什么吧～' : `「${activeTabLabel}」分类下还没有待办`"
      actionText="加个待办"
      @action="openAdd"
    />
    <!-- 列表：未完成 / 已完成 两组分别可拖拽排序（组内按手动顺序）。拖拽手柄带 @pointerdown.stop，
         与 SwipeCard 的左滑「完成」手势彻底隔离；force-fallback 保证触屏拖拽一致。 -->
    <div v-else ref="listEl">
      <draggable
        v-if="statusFilter !== 'done'"
        v-model="activeDrag"
        item-key="id"
        class="cards"
        handle=".drag-handle"
        :animation="180"
        :force-fallback="true"
        fallback-class="drag-fallback"
        ghost-class="drag-ghost"
        @end="onTodoReorder('active')"
      >
        <template #item="{ element: t }">
          <SwipeCard
            :threshold="90"
            hint="完成"
            hint-color="#7BC47F"
            @dismiss="onSwipeDone(t)"
          >
            <div class="love-card todo">
              <button class="drag-handle" type="button" aria-label="拖动排序" @pointerdown.stop @click.stop>
                <GripVertical :size="16" />
              </button>
              <div class="todo-top">
                <button class="check" :aria-label="'标记完成'" @click="onToggle(t)"></button>
                <span class="todo-title title-clamp">{{ t.title }}</span>
                <n-tag v-if="t.category" size="small" round type="info" class="cat-tag">{{ t.category }}</n-tag>
              </div>

              <p v-if="t.description" class="todo-desc sub-text title-clamp">{{ t.description }}</p>

              <div class="todo-meta sub-text">
                <span>优先级 {{ '★'.repeat(t.priority) || '—' }}</span>
                <span v-if="t.dueTime">期限 {{ fmt(t.dueTime) }}</span>
                <span v-if="t.assigneeName">负责人：{{ t.assigneeName }}</span>
              </div>

              <div class="todo-actions">
                <n-popselect
                  :value="t.assigneeUserId ?? -1"
                  :options="assignOptions"
                  size="small"
                  trigger="click"
                  @update:value="(v: number) => onAssign(t, v)"
                >
                  <n-button size="small" tertiary>指派</n-button>
                </n-popselect>
                <n-button size="small" tertiary @click="openEdit(t)">编辑</n-button>
                <n-popconfirm @positive-click="onDelete(t.id)">
                  <template #trigger>
                    <n-button size="small" tertiary type="error">删除</n-button>
                  </template>
                  确定删除这个待办吗？
                </n-popconfirm>
              </div>
            </div>
          </SwipeCard>
        </template>
      </draggable>

      <draggable
        v-if="statusFilter !== 'active'"
        v-model="doneDrag"
        item-key="id"
        class="cards done-cards"
        handle=".drag-handle"
        :animation="180"
        :force-fallback="true"
        ghost-class="drag-ghost"
        @end="onTodoReorder('done')"
      >
        <template #item="{ element: t }">
          <div class="love-card todo done">
            <button class="drag-handle" type="button" aria-label="拖动排序" @pointerdown.stop @click.stop>
              <GripVertical :size="16" />
            </button>
            <div class="todo-top">
              <button class="check on" :aria-label="'标记未完成'" @click="onToggle(t)">
                <SuccessCheck :active="true" :size="16" :show-circle="false" color="#fff" />
              </button>
              <span class="todo-title title-clamp">{{ t.title }}</span>
              <n-tag v-if="t.category" size="small" round type="info" class="cat-tag">{{ t.category }}</n-tag>
            </div>

            <p v-if="t.description" class="todo-desc sub-text title-clamp">{{ t.description }}</p>

            <div class="todo-meta sub-text">
              <span>优先级 {{ '★'.repeat(t.priority) || '—' }}</span>
              <span v-if="t.dueTime">期限 {{ fmt(t.dueTime) }}</span>
              <span v-if="t.assigneeName">负责人：{{ t.assigneeName }}</span>
              <span v-if="t.doneUserName">完成者：{{ t.doneUserName }}</span>
            </div>

            <div class="todo-actions">
              <n-button size="small" tertiary @click="openEdit(t)">编辑</n-button>
              <n-popconfirm @positive-click="onDelete(t.id)">
                <template #trigger>
                  <n-button size="small" tertiary type="error">删除</n-button>
                </template>
                确定删除这个待办吗？
              </n-popconfirm>
            </div>
          </div>
        </template>
      </draggable>
    </div>

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

    <!-- 新增 / 编辑 待办：iOS 风表单 -->
    <LoveSheet v-model="showForm" :title="editing ? '编辑待办' : '加个待办'" subtitle="要一起做的事、要买的东西或任务">
      <div class="todo-form">
        <LoveInput
          v-model="form.title"
          label="标题"
          placeholder="要一起做的事 / 要买的东西 / 要完成的任务"
          :maxlength="120"
          counter
          clearable
          :invalid="titleInvalid"
          @update:modelValue="titleInvalid = false"
        />
        <LoveTextarea v-model="form.description" label="描述" placeholder="补充说明（可选）" :rows="3" :maxlength="1000" />
        <LoveInput v-model="form.category" label="分类" placeholder="购物 / 家务 / 出行 …（可选）" clearable />
        <LoveDateField v-model="dueTs" label="期限" />
        <LoveSegmented v-model="form.priority" label="优先级" :options="priorityOptions" />
        <LoveSegmented v-model="formAssignee" label="责任人" :options="assignOptions" />
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
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch, onUnmounted } from 'vue';
import {
  NButton, NTag, NPopconfirm, NTabs, NTabPane, NPopselect,
} from 'naive-ui';
import { SuccessCheck, SwipeCard } from '@/interactions';
import draggable from 'vuedraggable';
import { GripVertical } from 'lucide-vue-next';
import type { TodoDto, TodoReq } from '@/types';
import {
  listTodo, createTodo, updateTodo, deleteTodo, toggleTodo, assignTodo, reorderTodos,
} from '@/api/todo';
import { useNotifyStore } from '@/store/notifyStore';
import { useStaggerEnter } from '@/composables/useAnimation';
import { useRealtime, overlaySyncMap } from '@/composables/useRealtime';
import { useSyncSettle } from '@/composables/useSyncSettle';
import { useAuthStore } from '@/store/authStore';
import { usePartnerStore } from '@/store/partnerStore';
import IndProgressRing from '@/components/industrial/IndProgressRing.vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndPager from '@/components/industrial/IndPager.vue';
import { feedback } from '@/utils/feedback';
import { toLocalISO } from '@/utils/format';
import {
  LoveSheet, LoveInput, LoveTextarea, LoveSegmented, LoveDateField, LoveSaveBar,
} from '@/components/loveform';

const auth = useAuthStore();
const partner = usePartnerStore();
const notify = useNotifyStore();
const loading = ref(true);
const container = ref<HTMLElement>();
const listEl = ref<HTMLElement>();

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

const meId = computed(() => auth.profile?.id ?? 0);
const mateId = computed(() => partner.status?.partner?.id ?? null);
const mateName = computed(() => partner.status?.partner?.nickName ?? 'TA');

const todos = ref<TodoDto[]>([]);
const activeTab = ref<string>('all');
const statusFilter = ref<string>('active');

// 分类标签：从数据动态聚合 + “全部”
const categoryTabs = computed(() => {
  const set = new Set<string>();
  todos.value.forEach((t) => { if (t.category) set.add(t.category!); });
  const cats = [...set];
  return [
    { label: '全部', value: 'all' },
    ...cats.map((c) => ({ label: c, value: c })),
  ];
});
const activeTabLabel = computed(() => (activeTab.value === 'all' ? '' : activeTab.value));

const filtered = computed(() =>
  todos.value.filter((t) => {
    if (activeTab.value !== 'all' && t.category !== activeTab.value) return false;
    if (statusFilter.value === 'all') return true;
    if (statusFilter.value === 'done') return t.isDone;
    return !t.isDone; // active
  })
);

const rate = computed(() => {
  if (!todos.value.length) return 0;
  const done = todos.value.filter((t) => t.isDone).length;
  return Math.round((done / todos.value.length) * 100);
});

const displayCount = ref(12);
const displayList = computed(() => filtered.value.slice(0, displayCount.value));
const activeList = computed(() => displayList.value.filter((t) => !t.isDone));
const doneList = computed(() => displayList.value.filter((t) => t.isDone));
const hasMore = computed(() => displayCount.value < filtered.value.length);
function loadMore() { displayCount.value += 12; }
watch(filtered, () => { displayCount.value = 12; });

// 拖拽排序：active / done 各维护一份可被 vuedraggable 直接改写的本地数组，
// 与后端「按 IsDone 分组、组内按 SortOrder」的语义一一对应；@end 时把当前顺序回写后端。
const activeDrag = ref<TodoDto[]>([]);
const doneDrag = ref<TodoDto[]>([]);
watch(activeList, (v) => { activeDrag.value = [...v]; }, { immediate: true });
watch(doneList, (v) => { doneDrag.value = [...v]; }, { immediate: true });

async function onTodoReorder(group: 'active' | 'done') {
  const ids = (group === 'active' ? activeDrag.value : doneDrag.value).map((t) => t.id);
  if (ids.length < 2) return;
  try {
    await reorderTodos(ids);
  } catch {
    feedback.warn('排序保存失败，已恢复顺序');
  } finally {
    await load();
  }
}

const statusFilterOptions = [
  { label: '进行中', value: 'active' },
  { label: '已完成', value: 'done' },
  { label: '全部', value: 'all' },
];

// 责任人选项：-1 表示“双方共同”
const assignOptions = computed(() => {
  const opts: { label: string; value: number }[] = [
    { label: '双方共同', value: -1 },
    { label: '我', value: meId.value },
  ];
  if (mateId.value != null) opts.push({ label: `对方（${mateName.value}）`, value: mateId.value });
  return opts;
});

function fmt(s: string) {
  const d = new Date(s);
  return `${d.getMonth() + 1}月${d.getDate()}日`;
}

// ---- 表单 ----
const showForm = ref(false);
const editing = ref<TodoDto | null>(null);
const dueTs = ref<number | null>(null);
const saving = ref(false);
const saved = ref(false);
const titleInvalid = ref(false);
const formAssignee = ref<number>(-1);
const form = reactive<TodoReq>({
  title: '', description: undefined, priority: 2, dueTime: undefined, category: undefined, assigneeUserId: null,
});

const priorityOptions = [
  { label: '低', value: 1 },
  { label: '中', value: 2 },
  { label: '高', value: 3 },
];

function resetForm() {
  Object.assign(form, { title: '', description: undefined, priority: 2, dueTime: undefined, category: undefined, assigneeUserId: null });
  formAssignee.value = -1;
  dueTs.value = null;
  titleInvalid.value = false;
  saving.value = false;
  saved.value = false;
}
function openAdd() {
  editing.value = null;
  resetForm();
  showForm.value = true;
}
function openEdit(t: TodoDto) {
  editing.value = t;
  Object.assign(form, {
    title: t.title, description: t.description, priority: t.priority,
    category: t.category, assigneeUserId: t.assigneeUserId ?? null,
  });
  formAssignee.value = t.assigneeUserId ?? -1;
  dueTs.value = t.dueTime ? new Date(t.dueTime).getTime() : null;
  titleInvalid.value = false;
  saving.value = false;
  saved.value = false;
  showForm.value = true;
}
async function submitForm() {
  if (!form.title.trim()) {
    titleInvalid.value = true;
    feedback.warn('给待办起个标题吧～');
    return;
  }
  saving.value = true;
  try {
    form.dueTime = toLocalISO(dueTs.value);
    form.assigneeUserId = formAssignee.value === -1 ? null : formAssignee.value;
    if (editing.value) {
      await updateTodo(editing.value.id, { ...form });
      feedback.updated('待办');
    } else {
      await createTodo({ ...form });
      feedback.created('待办');
    }
    saved.value = true;
    later(async () => {
      showForm.value = false;
      await load();
    }, 720);
  } finally { saving.value = false; }
}

async function onToggle(t: TodoDto) {
  await toggleTodo({ id: t.id });
  notify.success(t.isDone ? '已标记未完成' : '已完成，棒棒哒');
  await load();
}
// 11 卡片抽走：未完成项向左滑「抽走」= 标记完成（可逆、非删除）
async function onSwipeDone(t: TodoDto) {
  if (t.isDone) return;
  await toggleTodo({ id: t.id });
  notify.success('已完成，棒棒哒');
  await load();
}
async function onAssign(t: TodoDto, v: number) {
  const assigneeUserId = v === -1 ? null : v;
  await assignTodo({ id: t.id, assigneeUserId });
  feedback.saved('指派');
  await load();
}
async function onDelete(id: number) {
  await deleteTodo(id);
  feedback.deleted('待办');
  await load();
}

async function load() {
  loading.value = true;
  try {
    const p = await listTodo({ page: 1, pageSize: 300 });
    todos.value = p.items;
  } finally { loading.value = false; }
}

useStaggerEnter(container, '.love-card', { stagger: 0.06, y: 14 });
// 实时融合：伴侣在别处新增/刷新待办时，本端卡片错落入场（非自己操作、尊重降级）
useSyncSettle('todo', listEl, todos, '.love-card');
const { useModuleSync } = useRealtime();
onMounted(async () => {
  await load();
  if (!partner.status) partner.load();
  useModuleSync('todo', { items: todos, getId: (i) => i.id, load, map: overlaySyncMap });
});
</script>

<style scoped>
.todo-page { max-width: 960px; margin: 0 auto; }
.page-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 14px; }
.head-left { display: flex; align-items: center; gap: 16px; }
.page-head h1 { font-size: 22px; margin: 0; }
.filter-bar { display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap; margin-bottom: 18px; }
.cat-tabs { flex: 1 1 320px; min-width: 0; }
.status-select { flex: 0 0 auto; width: 118px; }
@media (max-width: 520px) { .cat-tabs { flex: 1 1 100%; } .status-select { width: 100%; } }

.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 14px; }
/* 拖拽手柄：绝对定位在卡片右上角，仅在手柄上按下才触发排序（@pointerdown.stop 隔离 SwipeCard 左滑） */
.todo { position: relative; }
.drag-handle {
  position: absolute; top: 8px; right: 8px; z-index: 3;
  width: 26px; height: 26px; display: grid; place-items: center;
  border: none; background: transparent; color: var(--color-ink-3);
  cursor: grab; border-radius: 8px; touch-action: none;
  opacity: 0.45; transition: opacity var(--dur-micro), background var(--dur-micro);
}
.drag-handle:hover { opacity: 1; background: var(--color-ink-soft); }
.drag-handle:active { cursor: grabbing; }
.drag-ghost { opacity: 0.35; }
.drag-fallback { transform: rotate(2deg); box-shadow: var(--shadow-float); }
.done-cards { margin-top: 18px; }
.todo-top { display: flex; align-items: flex-start; gap: 10px; }
.check {
  flex: 0 0 auto; width: 22px; height: 22px; border-radius: 7px; cursor: pointer;
  border: 2px solid var(--color-ink-soft); background: transparent; color: var(--color-on-primary);
  display: grid; place-items: center; transition: all var(--dur-micro) var(--ease-love);
}
.check.on { background: linear-gradient(135deg, var(--color-rose), var(--color-rose-deep)); border-color: transparent; }
.todo-title { font-size: 16px; font-weight: 500; color: var(--color-ink); flex: 1 1 auto; }
.todo.done .todo-title { color: var(--color-ink-3); text-decoration: line-through; }
.cat-tag { flex: 0 0 auto; }
.todo-desc { margin: 8px 0 0; }
.todo-meta { display: flex; flex-wrap: wrap; gap: 12px; margin-top: 10px; }
.todo-actions { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 12px; }
.modal-foot { display: flex; justify-content: flex-end; gap: 10px; }
.todo-form { display: flex; flex-direction: column; gap: 18px; }

@media (max-width: 767px) { .cards { grid-template-columns: 1fr; } }
:global(.todo-modal) { padding: 0 !important; }
@media (max-width: 767px) {
  :global(.todo-modal) { width: 100vw !important; max-width: 100vw !important; height: 100dvh; margin: 0; border-radius: 0; }
}
</style>
