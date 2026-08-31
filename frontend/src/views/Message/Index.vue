<template>
  <div class="msg-page" ref="container">
    <!-- 品牌条 -->
    <div class="brand block">
      <h1 class="ind-label">MESSAGE · 消息中心</h1>
      <span class="brand-status"><IndLed color="green" :size="9" /> 已同步</span>
    </div>
    <p class="lead">重要的日子与记录会在这里提醒你</p>

    <div class="page-head">
      <div class="ops">
        <NButton size="small" v-press-bounce :loading="loading" @click="onRefreshClick">刷新</NButton>
        <NButton size="small" type="primary" class="uvi-shine" :disabled="unread === 0" v-press-bounce @click="markAllRead">全部已读</NButton>
        <NPopconfirm positive-text="删除" negative-text="取消" @positive-click="deleteReadAll">
          <template #trigger>
            <NButton size="small" type="error" ghost :disabled="!readList.length" v-press-bounce>删除已读</NButton>
          </template>
          确定删除全部已读消息吗？此操作不可恢复。
        </NPopconfirm>
        <NButton size="small" :type="selectMode ? 'primary' : 'default'" v-press-bounce @click="toggleSelectMode">选取</NButton>
      </div>
    </div>

    <!-- 统计瓷砖 -->
    <section class="block stats">
      <IndStatCard label="未读" :value="unread" sub="待你查看" />
      <IndStatCard label="已读" :value="readList.length" sub="已处理" />
      <IndStatCard label="共" :value="list.length" sub="全部消息" />
    </section>

      <IndSkeleton v-if="loading" variant="list" :rows="6" />
      <IndEmpty
        v-else-if="!list.length"
        title="还没有消息"
        desc="重要的日子和记录会在这里提醒你，先去制造些回忆吧～"
      />

      <template v-else>
        <section v-if="unreadList.length" class="grp">
          <IndSectionTitle label="未读" :led="true" />
          <div ref="unreadRef" class="list">
            <div
              v-for="m in unreadList"
              :key="m.id"
              class="m-card love-card unread uvi-glass-pop"
              role="button"
              tabindex="0"
              :aria-label="`${selectMode ? '选择消息：' : '展开消息：'}${m.title}`"
              :class="{ open: expanded.has(m.id), selected: selected.has(m.id) }"
              @click="onCardClick(m, $event)"
              @keydown.enter="onCardClick(m, $event)"
              @keydown.space.prevent="onCardClick(m, $event)"
            >
              <span v-if="selectMode" class="m-check" :class="{ on: selected.has(m.id) }" aria-hidden="true">
                <Check v-if="selected.has(m.id)" :size="14" :stroke-width="2.2" />
              </span>
              <span class="m-ico"><component :is="iconFor(m.messageType)" :size="20" /></span>
              <div class="m-body">
                <div class="m-top">
                  <span class="m-title">{{ m.title }}</span>
                  <span class="m-dot" />
                </div>
                <div class="m-time">{{ fmt(m.createTime) }}</div>
                <div class="m-content" :class="{ clamp: !expanded.has(m.id) }">{{ m.content || '（无正文）' }}</div>

                <!-- 已产生的反应胶囊 -->
                <div v-if="reactionList(m).length" class="msg-reactions">
                  <button
                    v-for="r in reactionList(m)"
                    :key="r.key"
                    class="reaction-pill"
                    :class="{ mine: r.mine, 'uvi-heartbeat': r.key === 'emoji_heart' }"
                    :aria-pressed="r.mine"
                    :aria-label="`${r.count} 人反应，${r.mine ? '你已反应，点击取消' : '点击也反应'}`"
                    @click.stop="toggleReaction(m, r.key)"
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
                      @click.stop="hapticForAction('tap'); reactingId = reactingId === m.id ? null : m.id"
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
                        @click.stop="toggleReaction(m, r.key)"
                      >
                        <IpIcon :name="r.key" :size="20" />
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section v-if="readList.length" class="grp">
          <IndSectionTitle label="已读" :led="true" />
          <div ref="readRef" class="list">
            <div
              v-for="m in readList"
              :key="m.id"
              class="m-card love-card uvi-glass-pop"
              role="button"
              tabindex="0"
              :aria-label="`${selectMode ? '选择消息：' : '展开消息：'}${m.title}`"
              :class="{ open: expanded.has(m.id), selected: selected.has(m.id) }"
              @click="onCardClick(m, $event)"
              @keydown.enter="onCardClick(m, $event)"
              @keydown.space.prevent="onCardClick(m, $event)"
            >
              <span v-if="selectMode" class="m-check" :class="{ on: selected.has(m.id) }" aria-hidden="true">
                <Check v-if="selected.has(m.id)" :size="14" :stroke-width="2.2" />
              </span>
              <span class="m-ico dim"><component :is="iconFor(m.messageType)" :size="20" /></span>
              <div class="m-body">
                <div class="m-top">
                  <span class="m-title">{{ m.title }}</span>
                </div>
                <div class="m-time">{{ fmt(m.createTime) }}</div>
                <div class="m-content" :class="{ clamp: !expanded.has(m.id) }">{{ m.content || '（无正文）' }}</div>

                <!-- 已产生的反应胶囊 -->
                <div v-if="reactionList(m).length" class="msg-reactions">
                  <button
                    v-for="r in reactionList(m)"
                    :key="r.key"
                    class="reaction-pill"
                    :class="{ mine: r.mine, 'uvi-heartbeat': r.key === 'emoji_heart' }"
                    :aria-pressed="r.mine"
                    :aria-label="`${r.count} 人反应，${r.mine ? '你已反应，点击取消' : '点击也反应'}`"
                    @click.stop="toggleReaction(m, r.key)"
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
                      @click.stop="hapticForAction('tap'); reactingId = reactingId === m.id ? null : m.id"
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
                        @click.stop="toggleReaction(m, r.key)"
                      >
                        <IpIcon :name="r.key" :size="20" />
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <IndPager
          mode="more"
          :page="page"
          :page-size="pageSize"
          :total="total"
          :loading="loading"
          :has-more="hasMore"
          @load-more="nextPage"
          v-if="list.length"
        />

        <!-- 选取模式：批量操作条 -->
        <div v-if="selectMode" class="sel-bar" role="toolbar" aria-label="批量操作">
          <button type="button" class="sel-btn uvi-jelly" v-press-bounce @click="toggleAll">{{ allSelected ? '取消全选' : '全选' }}</button>
          <span class="sel-count">已选 {{ selected.size }} 条</span>
          <button type="button" class="sel-btn sel-del uvi-jelly" :disabled="!selected.size" v-press-bounce @click="deleteSelected">删除</button>
          <button type="button" class="sel-btn sel-cancel uvi-jelly" v-press-bounce @click="toggleSelectMode">取消</button>
        </div>
      </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick, type Component } from 'vue';
import { NButton, NPopconfirm } from 'naive-ui';
import { gsap } from 'gsap';
import type { SystemMessageDto } from '@/types';
import * as msgApi from '@/api/message';
import IndSectionTitle from '@/components/industrial/IndSectionTitle.vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndPager from '@/components/industrial/IndPager.vue';
import IndLed from '@/components/industrial/IndLed.vue';
import IndStatCard from '@/components/industrial/IndStatCard.vue';
import { useSettingStore } from '@/store/settingStore';
import { usePagedList } from '@/composables/usePagedList';
import { useStaggerEnter } from '@/composables/useAnimation';
import { useRealtime } from '@/composables/useRealtime';
import { useSyncSettle } from '@/composables/useSyncSettle';
import { feedback } from '@/utils/feedback';
import { Mail, Gem, CheckCircle2, Heart, Star, Image as ImageIcon, PenLine, Check } from 'lucide-vue-next';
import IpIcon from '@/components/Common/IpIcon.vue';
import { hapticForAction } from '@/composables/useHaptic';
import { useAuthStore } from '@/store/authStore';

const setting = useSettingStore();
const auth = useAuthStore();
const meId = computed(() => auth.profile?.id ?? 0);

const expanded = ref<Set<number>>(new Set());
// 选取模式：删除已读 / 批量选取删除
const selectMode = ref(false);
const selected = ref<Set<number>>(new Set());
const allSelected = computed(
  () => list.value.length > 0 && list.value.every((m) => selected.value.has(m.id)),
);
function toggleSelectMode() {
  selectMode.value = !selectMode.value;
  selected.value = new Set();
}
function toggleSel(id: number) {
  const s = new Set(selected.value);
  s.has(id) ? s.delete(id) : s.add(id);
  selected.value = s;
}
function toggleAll() {
  selected.value = allSelected.value ? new Set() : new Set(list.value.map((m) => m.id));
}
function onCardClick(m: SystemMessageDto, e?: Event) {
  if (selectMode.value) {
    e?.preventDefault();
    toggleSel(m.id);
    return;
  }
  open(m);
}
async function deleteReadAll() {
  await msgApi.deleteRead();
  feedback.deleted('已读消息');
  await refresh();
  syncUnread();
}
async function deleteSelected() {
  const ids = [...selected.value];
  if (!ids.length) return;
  await msgApi.batchDeleteMessage(ids);
  feedback.deleted('消息');
  selected.value = new Set();
  selectMode.value = false;
  await refresh();
  syncUnread();
}
const container = ref<HTMLElement>();
const unreadRef = ref<HTMLElement>();
const readRef = ref<HTMLElement>();
const unread = ref(0);
let timer: number | undefined;

const { list, page, pageSize, total, loading, hasMore, nextPage, refresh, loadFirst } = usePagedList<SystemMessageDto>(
  async (p) => {
    const r = await msgApi.listMessage({ page: p.page, pageSize: p.pageSize });
    const items = (r.items ?? []).slice().sort(
      (a, b) => Number(new Date(b.createTime)) - Number(new Date(a.createTime)),
    );
    return { items, total: r.total ?? items.length };
  },
  { pageSize: 50, mode: 'more' },
);
loading.value = true;

function syncUnread() {
  unread.value = list.value.filter((m) => !m.isRead).length;
}

const unreadList = computed(() => list.value.filter((m) => !m.isRead));
const readList = computed(() => list.value.filter((m) => m.isRead));

const ICONS: Record<number, Component> = {
  1: Mail, 2: Gem, 3: CheckCircle2, 4: Heart, 5: Star, 6: ImageIcon, 7: PenLine,
};
function iconFor(type: number): Component {
  return ICONS[type] ?? Mail;
}

function fmt(s: string) {
  const d = new Date(s);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getMonth() + 1}/${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

async function onRefreshClick() {
  await refresh();
  syncUnread();
  await nextTick();
  staggerIn();
}

function staggerIn() {
  if (setting.reduceMotion) return;
  const els = container.value?.querySelectorAll('.m-card');
  if (els && els.length) {
    gsap.fromTo(els, { opacity: 0, y: 14 }, { opacity: 1, y: 0, duration: 0.4, ease: 'power2.out', stagger: 0.05 });
  }
}

function toggleExpand(id: number) {
  const s = new Set(expanded.value);
  s.has(id) ? s.delete(id) : s.add(id);
  expanded.value = s;
}

async function open(m: SystemMessageDto) {
  toggleExpand(m.id);
  if (!m.isRead) {
    try {
      await msgApi.readMessage(m.id);
      m.isRead = true;
      syncUnread();
    } catch { /* 忽略 */ }
  }
}

async function markAllRead() {
  if (unread.value === 0) return;
  await msgApi.readAll();
  await refresh();
  syncUnread();
  feedback.info('已把所有消息标记为已读');
}

// 反应：与留言板共享 8 个表情，名称对应 src/assets/icons/ip/emoji_*.png
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
// 当前展开表情选择器的消息 id（null=全部收起）
const reactingId = ref<number | null>(null);

// 把消息的 reactions 字典整理成「有数量」的列表，并标记当前用户是否已点
function reactionList(m: SystemMessageDto) {
  const r = m.reactions ?? {};
  return Object.entries(r)
    .filter(([, users]) => (users?.length ?? 0) > 0)
    .map(([key, users]) => ({ key, count: users.length, mine: users.includes(meId.value) }))
    .sort((a, b) => b.count - a.count);
}
function hasReacted(m: SystemMessageDto, key: string) {
  return (m.reactions?.[key] ?? []).includes(meId.value);
}
// 切换某条消息的某个表情：已点则取消，未点则加上；用返回的最新 reactions 就地更新，避免整页刷新打断滚动
async function toggleReaction(m: SystemMessageDto, key: string) {
  reactingId.value = null;
  try {
    const res = await msgApi.addReaction({ id: m.id, emojiKey: key });
    const idx = list.value.findIndex(x => x.id === m.id);
    if (idx >= 0) list.value[idx] = { ...list.value[idx], reactions: res.reactions };
    hapticForAction('tap');
  } catch {
    feedback.warn('反应失败，请重试');
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
const { useModuleSync } = useRealtime();
onMounted(async () => {
  try {
    await loadFirst();
    syncUnread();
    await nextTick();
    staggerIn();
  } finally {
    loading.value = false;
  }
  // 伴侣触发的消息变更：整表刷新 + 卡片错落入场
  useModuleSync('message', { items: list, getId: (m) => m.id, load: refresh });
  useSyncSettle('message', container, list, '.m-card');
  timer = window.setInterval(async () => {
    try {
      unread.value = (await msgApi.unreadCount()) ?? 0;
    } catch { /* 忽略 */ }
  }, 30000);
  window.addEventListener('pointerdown', onDocClick);
});
onUnmounted(() => {
  if (timer) window.clearInterval(timer);
  window.removeEventListener('pointerdown', onDocClick);
});
</script>

<style scoped>
.msg-page { max-width: 880px; margin: 0 auto; }
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

/* 操作区 + 统计瓷砖 */
.page-head { display: flex; align-items: flex-end; justify-content: flex-end; margin-bottom: 14px; }
.ops { display: flex; gap: 10px; }
.stats { display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; }

.grp { margin: 22px 0; }
.list { display: flex; flex-direction: column; gap: 12px; }
.m-card {
  display: flex; gap: 14px; align-items: flex-start; cursor: pointer;
  transition: transform var(--dur-pop) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love);
}
.m-card.unread { box-shadow: 0 0 0 1.5px var(--color-rose), var(--shadow-card); }
.m-card:hover { box-shadow: var(--elev-2); }
.m-card.unread:hover { box-shadow: 0 0 0 1.5px var(--color-rose), var(--elev-2); }
.m-card:active { transform: scale(0.99); }
.m-card.open { box-shadow: 0 0 0 1.5px var(--color-rose-soft), var(--elev-2); }
.m-card:focus-visible { outline: 2px solid var(--color-rose); outline-offset: 2px; }
.m-ico {
  flex: 0 0 auto; width: 42px; height: 42px; border-radius: 50%; display: grid; place-items: center;
  font-size: 20px; color: var(--color-rose-text); background: var(--color-rose-soft); box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04);
}
.m-ico.dim { filter: grayscale(0.4); opacity: 0.7; }
.m-body { flex: 1; min-width: 0; }
.m-top { display: flex; align-items: center; gap: 8px; }
.m-title { font-weight: 600; color: var(--color-ink); }
.m-dot { width: 8px; height: 8px; border-radius: 999px; background: var(--color-rose); box-shadow: 0 0 0 3px var(--color-rose-soft); }
.m-time { font-size: 12px; color: var(--color-ink-3); font-family: var(--font-mono); margin: 2px 0 4px; }
.m-content { font-size: 13px; color: var(--color-ink-2); line-height: 1.6; white-space: pre-wrap; }
.m-content.clamp { display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }

/* 选取模式：复选框 + 选中态 + 底部批量条 */
.m-check {
  flex: 0 0 auto; width: 22px; height: 22px; margin-top: 10px; border-radius: 50%;
  border: 1.5px solid var(--color-border); background: var(--color-surface);
  display: grid; place-items: center; color: var(--color-on-primary);
  transition: all var(--dur-micro) var(--ease-love);
}
.m-check.on { background: var(--color-rose); border-color: var(--color-rose); }
.m-card.selected { box-shadow: 0 0 0 1.5px var(--color-rose), var(--elev-2); }
.sel-bar {
  position: sticky; bottom: 12px; z-index: 20; margin-top: 18px;
  display: flex; align-items: center; gap: 10px;
  padding: 10px 14px; border-radius: 14px;
  background: var(--color-surface); border: 1px solid var(--color-border);
  box-shadow: var(--shadow-float);
}
.sel-count { flex: 1; font-size: 13px; color: var(--color-ink-2); }
.sel-btn {
  border: 1px solid var(--color-border); background: var(--color-surface-2);
  color: var(--color-ink); font-size: 13px; font-weight: 600;
  padding: 7px 14px; border-radius: 999px; cursor: pointer;
  transition: all var(--dur-micro) var(--ease-love);
}
.sel-btn:hover:not(:disabled) { border-color: var(--color-rose); color: var(--color-rose-text); }
.sel-btn:disabled { opacity: 0.45; cursor: not-allowed; }
.sel-del { color: var(--color-rose-text); }
.sel-cancel { background: none; }

/* 反应：胶囊 + 表情选择器，与留言板视觉一致 */
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
html:not(.reduce-motion) .reaction-opt:hover { transform: scale(1.18); background: var(--color-surface-2); }
.reaction-opt.active { background: var(--color-rose-soft); }

@media (max-width: 767px) {
  .stats { grid-template-columns: 1fr; }
}
</style>
