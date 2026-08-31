<template>
  <PullRefresh @refresh="onRefresh">
    <div class="board-page" ref="container">
    <!-- 品牌条 -->
    <div class="brand block">
      <h1 class="ind-label">BOARD · 留言板</h1>
      <span class="brand-status"><IndLed color="green" :size="9" /> 已同步</span>
    </div>
    <p class="lead">公开墙 + 私密信箱，把想说的话都留下来</p>

    <!-- 统计瓷砖 -->
    <section class="block stats">
      <IndStatCard label="公开墙" :value="publicCount" sub="所有人可见" />
      <IndStatCard label="私密信箱" :value="privateCount" sub="仅你们可见" />
      <IndStatCard label="置顶" :value="pinnedCount" sub="常驻顶部" />
    </section>

    <!-- 标签页 -->
    <n-tabs v-model:value="activeTab" type="segment" class="tabs">
      <n-tab-pane name="public" tab="公开墙">
        <template #tab>
          <span>公开墙</span>
        </template>
      </n-tab-pane>
      <n-tab-pane name="private" tab="私密信箱">
        <template #tab>
          <span>私密信箱</span>
        </template>
      </n-tab-pane>
    </n-tabs>

    <!-- 留言编辑区 -->
    <div class="composer love-card">
      <n-input
        v-model:value="draft"
        type="textarea"
        :autosize="{ minRows: 2, maxRows: 5 }"
        placeholder="写点什么给对方吧～"
        aria-label="留言内容"
        @keydown.ctrl.enter="send"
      />
      <div class="composer-bar">
        <div class="colors">
          <button
            v-for="c in colorPresets"
            :key="c"
            class="swatch"
            :class="{ on: draftColor === c }"
            :style="{ background: c, '--c': c }"
            :aria-label="`颜色 ${c}`"
            @click="hapticForAction('tap'); draftColor = draftColor === c ? '' : c"
          />
          <button class="swatch none" :class="{ on: !draftColor }" aria-label="无颜色" @click="hapticForAction('tap'); draftColor = ''">无</button>
        </div>
        <div class="composer-actions">
          <ImageField v-model="draftImage" />
          <n-button type="primary" round class="uvi-shine" :disabled="!draft.trim()" :loading="sending" v-click-burst @click="send">
            {{ activeTab === 'public' ? '贴上墙' : '发送私信' }}
          </n-button>
        </div>
      </div>
    </div>

    <!-- 列表 -->
    <IndSkeleton v-if="loading" variant="list" :rows="6" />
    <IndEmpty
      v-else-if="!displayList.length"
      :title="activeTab === 'public' ? '公开墙还是空的' : '还没有私密消息'"
      :desc="activeTab === 'public' ? '留下第一条悄悄话，让 TA 一打开就看到你的心意～' : '给 TA 写一封私密消息吧～'"
    />
    <div v-else class="wall">
      <div
        v-for="m in displayList"
        :key="m.id"
        class="msg love-card"
        :class="{ mine: m.createUserId === meId, pinned: m.pinned, pop: poppingId === m.id }"
        :style="m.color ? { borderLeftColor: m.color } : {}"
      >
        <div class="msg-top">
          <span v-if="m.pinned" class="pin-tag"><Pin :size="13" :stroke-width="2.2" /> 置顶</span>
          <span v-if="m.createUserId === meId" class="mine-tag">我</span>
          <span class="author">{{ m.authorName || (m.createUserId === meId ? '我' : 'TA') }}</span>
          <span class="time sub-text">{{ fmt(m.createTime) }}</span>
        </div>
        <img v-if="m.imageUrl" :src="m.imageUrl" class="msg-img" alt="配图" loading="lazy" />
        <p class="msg-body" :style="m.color ? { color: m.color } : {}">{{ m.content }}</p>

        <!-- 已产生的反应胶囊 -->
        <div v-if="reactionList(m).length" class="msg-reactions">
          <button
            v-for="r in reactionList(m)"
            :key="r.key"
            class="reaction-pill"
            :class="{ mine: r.mine, 'uvi-heartbeat': r.key === 'emoji_heart' }"
            :aria-pressed="r.mine"
            :aria-label="`${r.count} 人反应，${r.mine ? '你已反应，点击取消' : '点击也反应'}`"
            @click="toggleReaction(m, r.key)"
          >
            <IpIcon :name="r.key" :size="16" />
            <span class="reaction-count">{{ r.count }}</span>
          </button>
        </div>

        <div class="msg-actions">
          <div class="react-wrap">
            <button
              class="msg-btn react-trigger"
              :class="{ on: reactingId === m.id }"
              :aria-expanded="reactingId === m.id"
              aria-label="添加反应"
              @click="hapticForAction('tap'); reactingId = reactingId === m.id ? null : m.id"
            >
              <IpIcon name="emoji_heart" :size="15" /> 反应
            </button>
            <div v-if="reactingId === m.id" class="reaction-picker" role="menu">
              <button
                v-for="r in REACTIONS"
                :key="r.key"
                class="reaction-opt"
                :class="{ active: hasReacted(m, r.key), 'uvi-heartbeat': r.key === 'emoji_heart' }"
                :aria-label="`反应：${r.label}`"
                @click="toggleReaction(m, r.key)"
              >
                <IpIcon :name="r.key" :size="20" />
              </button>
            </div>
          </div>
          <button class="msg-btn" @click="onPin(m)">{{ m.pinned ? '取消置顶' : '置顶' }}</button>
          <button v-if="m.createUserId === meId" class="msg-btn" @click="openEdit(m)">编辑</button>
          <n-popconfirm v-if="m.createUserId === meId" @positive-click="onDelete(m.id)">
            <template #trigger>
              <button class="msg-btn danger">删除</button>
            </template>
            确定删除这条留言吗？
          </n-popconfirm>
        </div>
      </div>
    </div>

    <IndPager
      v-if="messages.length"
      mode="more"
      :page="1"
      :page-size="20"
      :total="filteredList.length"
      :loading="loading"
      :has-more="hasMore"
      @load-more="loadMore"
    />

    <!-- 编辑 模态 -->
    <LoveSheet v-model="showEdit" title="编辑留言">
      <LoveTextarea v-model="editDraft" label="内容" placeholder="写下你的心里话" :rows="3" />
      <div class="edit-color-block">
        <span class="edit-color-label">颜色标记</span>
        <div class="colors edit-colors">
          <button
            v-for="c in colorPresets"
            :key="c"
            class="swatch"
            :class="{ on: editColor === c }"
            :style="{ background: c, '--c': c }"
            @click="hapticForAction('tap'); editColor = editColor === c ? '' : c"
          />
          <button class="swatch none" :class="{ on: !editColor }" @click="hapticForAction('tap'); editColor = ''">无</button>
        </div>
      </div>
      <template #footer>
        <LoveSaveBar :loading="sending" :success="saved" cancel-text="取消" save-text="保存" @cancel="showEdit = false" @save="submitEdit" />
      </template>
    </LoveSheet>
  </div>
  </PullRefresh>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue';
import { NButton, NPopconfirm, NTabs, NTabPane } from 'naive-ui';
import { LoveSheet, LoveTextarea, LoveSaveBar } from '@/components/loveform';
import { Pin } from 'lucide-vue-next';
import type { BoardMessageDto, BoardMessageReq } from '@/types';
import {
  listBoard, createBoard, updateBoard, deleteBoard, pinBoard, addReaction,
} from '@/api/board';
import { useNotifyStore } from '@/store/notifyStore';
import { useStaggerEnter } from '@/composables/useAnimation';
import { useRealtime, overlaySyncMap } from '@/composables/useRealtime';
import { useSyncSettle } from '@/composables/useSyncSettle';
import { useAuthStore } from '@/store/authStore';
import { usePartnerStore } from '@/store/partnerStore';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndPager from '@/components/industrial/IndPager.vue';
import IndLed from '@/components/industrial/IndLed.vue';
import IndStatCard from '@/components/industrial/IndStatCard.vue';
import ImageField from '@/components/Common/ImageField.vue';
import IpIcon from '@/components/Common/IpIcon.vue';
import { feedback } from '@/utils/feedback';
import PullRefresh from '@/components/Common/PullRefresh.vue';
import { hapticForAction } from '@/composables/useHaptic';

const auth = useAuthStore();
const notify = useNotifyStore();
const partner = usePartnerStore();
const meId = computed(() => auth.profile?.id ?? 0);
const partnerId = computed(() => partner.status?.partner?.id ?? null);

const loading = ref(true);
const sending = ref(false);
const saved = ref(false);
const container = ref<HTMLElement>();
const messages = ref<BoardMessageDto[]>([]);
const activeTab = ref<'public' | 'private'>('public');
const poppingId = ref<number | null>(null);

// 反应表情：8 个可选表情，名称与 src/assets/icons/ip/emoji_*.png 一一对应
const REACTIONS = [
  { key: 'emoji_heart', label: '比心' },
  { key: 'emoji_kiss', label: '亲亲' },
  { key: 'emoji_hug', label: '抱抱' },
  { key: 'emoji_laugh', label: '笑哭' },
  { key: 'emoji_star', label: '星星' },
  { key: 'emoji_surprised', label: '惊讶' },
  { key: 'emoji_cry', label: '哭哭' },
  { key: 'emoji_angry', label: '生气' },
];
// 当前展开表情选择器的留言 id（null=全部收起）
const reactingId = ref<number | null>(null);

// 统一管理延迟回调（保存后关弹窗 / 删除动画后再移除），卸载时一次性清理，避免过期定时器
const pendingTimers = new Set<number>();
function later(fn: () => void, ms: number) {
  const id = window.setTimeout(() => { pendingTimers.delete(id); fn(); }, ms);
  pendingTimers.add(id);
}

const draft = ref('');
const draftColor = ref('');
const draftImage = ref<string | undefined>(undefined);
const colorPresets = ['#ff6f7d', '#ff9a76', '#7c83fd', '#43c6ac', '#f6c453', '#c77dff'];

const showEdit = ref(false);
const editId = ref<number>(0);
const editDraft = ref('');
const editColor = ref('');

const displayCount = ref(20);
const filteredList = computed(() => messages.value.filter(m => {
  if (activeTab.value === 'public') return !m.isPrivate;
  return m.isPrivate && (m.receiverUserId === meId.value || m.createUserId === meId.value);
}));
const displayList = computed(() => filteredList.value.slice(0, displayCount.value));
// 分页基于「当前标签页过滤后」的数量，而非全量 messages，避免切到条数少的标签仍显示「加载更多」
const hasMore = computed(() => displayCount.value < filteredList.value.length);
function loadMore() { displayCount.value += 20; }
// 切换标签时重置分页游标，两个标签各自从第一页开始
watch(activeTab, () => { displayCount.value = 20; });

// 统计瓷砖
const publicCount = computed(() => messages.value.filter(m => !m.isPrivate).length);
const privateCount = computed(() => messages.value.filter(m => m.isPrivate).length);
const pinnedCount = computed(() => messages.value.filter(m => m.pinned).length);

// 反应：把留言的 reactions 字典整理成「有数量」的列表，并标记当前用户是否已点
function reactionList(m: BoardMessageDto) {
  const r = m.reactions ?? {};
  return Object.entries(r)
    .filter(([, users]) => (users?.length ?? 0) > 0)
    .map(([key, users]) => ({ key, count: users.length, mine: users.includes(meId.value) }))
    .sort((a, b) => b.count - a.count);
}
function hasReacted(m: BoardMessageDto, key: string) {
  return (m.reactions?.[key] ?? []).includes(meId.value);
}
// 切换某条留言的某个表情：已点则取消，未点则加上；用返回的最新 reactions 就地更新，避免整页刷新打断滚动
async function toggleReaction(m: BoardMessageDto, key: string) {
  reactingId.value = null;
  try {
    const res = await addReaction({ id: m.id, emojiKey: key });
    const idx = messages.value.findIndex(x => x.id === m.id);
    if (idx >= 0) messages.value[idx] = { ...messages.value[idx], reactions: res.reactions };
    hapticForAction('tap');
  } catch {
    feedback.warn('反应失败，请重试');
  }
}

function fmt(s: string) {
  const d = new Date(s);
  return `${d.getMonth() + 1}/${d.getDate()} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
}

async function send() {
  const content = draft.value.trim();
  if (!content) return;
  if (activeTab.value === 'private' && !partnerId.value) {
    feedback.warn('请先绑定伴侣再发送私密消息');
    return;
  }
  sending.value = true;
  try {
    const req: BoardMessageReq = {
      content,
      color: draftColor.value || undefined,
      imageUrl: draftImage.value,
      isPrivate: activeTab.value === 'private',
      receiverUserId: activeTab.value === 'private' ? partnerId.value! : undefined,
    };
    await createBoard(req);
    hapticForAction('success');
    feedback.created('留言');
    draft.value = '';
    draftColor.value = '';
    draftImage.value = undefined;
    await load();
  } finally { sending.value = false; }
}

function openEdit(m: BoardMessageDto) {
  editId.value = m.id;
  editDraft.value = m.content;
  editColor.value = m.color ?? '';
  saved.value = false;
  showEdit.value = true;
}
async function submitEdit() {
  if (!editDraft.value.trim()) { feedback.warn('留言内容不能为空'); return; }
  sending.value = true;
  saved.value = false;
  try {
    await updateBoard(editId.value, { content: editDraft.value.trim(), color: editColor.value || undefined });
    feedback.updated('留言');
    saved.value = true;
    later(async () => {
      showEdit.value = false;
      await load();
    }, 680);
  } finally { sending.value = false; }
}

async function onPin(m: BoardMessageDto) {
  await pinBoard({ id: m.id });
  notify.success(m.pinned ? '已取消置顶' : '已置顶');
  await load();
}
async function onDelete(id: number) {
  await deleteBoard(id);
  feedback.deleted('留言');
  // 先标记 pop 动画，等 0.3s 动画播完再从本地移除；修复原「先移除导致动画永不触发」
  poppingId.value = id;
  later(() => {
    messages.value = messages.value.filter(m => m.id !== id);
    poppingId.value = null;
  }, 300);
}

async function load() {
  loading.value = true;
  try {
    const p = await listBoard({ page: 1, pageSize: 300 });
    messages.value = p.items;
  } finally { loading.value = false; }
}

/** 下拉刷新：done 由 PullRefresh 传入，必须调用以收起指示器 */
async function onRefresh(done?: () => void) {
  try {
    await load();
  } finally {
    done?.();
  }
}

// 点击空白处收起表情选择器
function onDocClick(e: MouseEvent) {
  if (reactingId.value == null) return;
  const el = e.target as HTMLElement | null;
  if (el && el.closest('.react-wrap')) return;
  reactingId.value = null;
}

useStaggerEnter(container, '.block', { stagger: 0.1, y: 16 });
useStaggerEnter(container, '.msg', { stagger: 0.05, y: 12 });
const { useModuleSync } = useRealtime();
onMounted(async () => {
  await load();
  useModuleSync('board', { items: messages, getId: (i) => i.id, load, map: overlaySyncMap });
  // 伴侣发来新留言时，消息卡错落入场（避开编辑区 composer）
  useSyncSettle('board', container, messages, '.msg');
  document.addEventListener('click', onDocClick);
});
onUnmounted(() => {
  pendingTimers.forEach((id) => clearTimeout(id));
  pendingTimers.clear();
  document.removeEventListener('click', onDocClick);
});
</script>

<style scoped>
.board-page { max-width: 880px; margin: 0 auto; }
.block { margin: 22px 0; }

/* 品牌条 */
.brand {
  display: flex; align-items: center; gap: 14px; padding: 12px 16px; margin-bottom: 8px;
  background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg);
  box-shadow: var(--shadow-card);
}
.brand-status {
  margin-left: auto; display: inline-flex; align-items: center; gap: 6px;
  font-size: 12px; font-weight: 500; color: var(--color-ink-2);
  padding: 4px 12px; border-radius: 999px;
  background: var(--color-surface-2); border: 1px solid var(--color-border);
}
.ind-label { font-family: var(--font-mono); font-weight: 500; letter-spacing: 0.1em; font-size: 13px; color: var(--color-ink); margin: 0; }
.lead { margin: 0 0 18px; font-size: 13px; color: var(--color-ink-3); }

/* 统计瓷砖 */
.stats { display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; }

.tabs { margin-bottom: 18px; }

.composer { padding: 14px; margin-bottom: 18px; }
.composer-bar { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-top: 12px; }
.composer-actions { display: flex; align-items: center; gap: 10px; }
.colors { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.swatch {
  width: 22px; height: 22px; border-radius: 50%; cursor: pointer; border: 2px solid transparent;
  box-shadow: 0 0 0 1px var(--color-border); transition: transform var(--dur-micro) var(--ease-love);
}
.swatch.on { border-color: var(--color-ink); transform: scale(1.12); }
/* 选色光晕扩散：使用当前色作为 --c，选中瞬间光晕由内向外扩散 */
.swatch:not(.none).on {
  animation: swatch-spread var(--fx-dur-pop, 320ms) var(--fx-ease-out, ease);
  box-shadow: 0 0 0 3px var(--c), 0 0 14px 3px color-mix(in srgb, var(--c) 45%, transparent);
}
@keyframes swatch-spread {
  0%   { box-shadow: 0 0 0 0 var(--c), 0 0 0 0 transparent; }
  60%  { box-shadow: 0 0 0 4px var(--c), 0 0 22px 7px color-mix(in srgb, var(--c) 60%, transparent); }
  100% { box-shadow: 0 0 0 3px var(--c), 0 0 14px 3px color-mix(in srgb, var(--c) 45%, transparent); }
}
.swatch.none { background: var(--color-surface-2); color: var(--color-ink-3); font-size: 11px; display: grid; place-items: center; }

.wall { display: flex; flex-direction: column; gap: 12px; }
.msg {
  padding: 14px 16px; border-left: 4px solid var(--color-rose-soft);
  transition: transform var(--dur-pop) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love), border-color var(--dur-pop) var(--ease-love);
}
.msg.mine { border-left-color: var(--color-cocoa); }
.msg.pinned { background: color-mix(in srgb, var(--color-rose-soft) 40%, var(--color-surface)); }
.msg:hover { transform: translateY(-3px); box-shadow: var(--elev-3); }
.msg.pop { animation: msgPop 0.3s var(--ease-love); }
@keyframes msgPop { 0% { opacity: 1; } 50% { opacity: 0.25; } 100% { opacity: 1; } }
.msg-top { display: flex; align-items: center; gap: 8px; }
.pin-tag { display: inline-flex; align-items: center; gap: 3px; font-size: 11px; color: var(--color-rose-text); background: var(--color-rose-soft); padding: 2px 8px; border-radius: 999px; }
.mine-tag { display: inline-flex; align-items: center; font-size: 11px; font-weight: 700; color: var(--color-on-primary); background: var(--color-cocoa); padding: 2px 7px; border-radius: 999px; }
.author { font-weight: 600; color: var(--color-ink); font-size: 13px; }
.time { margin-left: auto; font-size: 11px; font-family: var(--font-mono); }
.msg-body { margin: 8px 0 0; white-space: pre-wrap; line-height: 1.6; color: var(--color-ink); }
.msg-img { width: 100%; border-radius: 12px; object-fit: cover; max-height: 260px; margin-top: 8px; }

/* 已产生的反应胶囊 */
.msg-reactions { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 10px; }
.reaction-pill {
  display: inline-flex; align-items: center; gap: 4px; cursor: pointer;
  border: 1px solid var(--color-border); padding: 3px 9px 3px 6px; border-radius: 999px;
  font-size: 12px; background: var(--color-surface-2); color: var(--color-ink-2);
  transition: all var(--dur-micro) var(--ease-love);
}
.reaction-pill:hover { border-color: var(--color-rose-soft); background: var(--color-rose-soft); }
.reaction-pill.mine { background: var(--color-rose-soft); border-color: var(--color-rose-text); color: var(--color-rose-text); }
.reaction-pill .reaction-count { font-variant-numeric: tabular-nums; font-weight: 600; }

.msg-actions { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 10px; }
.react-wrap { position: relative; display: inline-flex; }
.msg-btn {
  display: inline-flex; align-items: center; gap: 5px; cursor: pointer;
  border: 1px solid var(--color-border); padding: 6px 12px; border-radius: var(--radius-md);
  font-size: 13px; background: var(--color-surface-2); color: var(--color-ink-2);
  box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04);
  transition: all var(--dur-micro) var(--ease-love);
}
.msg-btn:active { transform: scale(0.98); }
.msg-btn:hover { color: var(--color-rose-text); border-color: var(--color-rose-soft); background: var(--color-rose-soft); }
.msg-btn.danger { color: var(--color-rose-text); }
.msg-btn.react-trigger.on { color: var(--color-rose-text); border-color: var(--color-rose-soft); background: var(--color-rose-soft); }

/* 表情选择器弹层 */
.reaction-picker {
  position: absolute; top: calc(100% + 6px); left: 0; z-index: 20;
  display: flex; gap: 2px; padding: 6px;
  background: var(--color-surface); border: 1px solid var(--color-border);
  border-radius: var(--radius-md); box-shadow: var(--elev-3);
}
.reaction-opt {
  display: grid; place-items: center; width: 32px; height: 32px; cursor: pointer;
  border: none; background: transparent; border-radius: var(--radius-sm);
  transition: transform var(--dur-micro) var(--ease-love), background var(--dur-micro) var(--ease-love);
}
.reaction-opt:hover { transform: scale(1.18); background: var(--color-surface-2); }
.reaction-opt.active { background: var(--color-rose-soft); }

.edit-color-block { display: flex; flex-direction: column; gap: 8px; }
.edit-color-label { font-size: 13px; font-weight: 500; color: var(--color-ink-2); padding-left: 2px; }

@media (max-width: 767px) {
  .composer-bar { flex-direction: column; align-items: stretch; }
  .stats { grid-template-columns: 1fr; }
}
</style>
