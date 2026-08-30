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
        <NButton size="small" type="primary" :disabled="unread === 0" v-press-bounce @click="markAllRead">全部已读</NButton>
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
              class="m-card love-card unread"
              role="button"
              tabindex="0"
              :aria-label="`展开消息：${m.title}`"
              :class="{ open: expanded.has(m.id) }"
              @click="open(m)"
              @keydown.enter="open(m)"
              @keydown.space.prevent="open(m)"
            >
              <span class="m-ico"><component :is="iconFor(m.messageType)" :size="20" /></span>
              <div class="m-body">
                <div class="m-top">
                  <span class="m-title">{{ m.title }}</span>
                  <span class="m-dot" />
                </div>
                <div class="m-time">{{ fmt(m.createTime) }}</div>
                <div class="m-content" :class="{ clamp: !expanded.has(m.id) }">{{ m.content || '（无正文）' }}</div>
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
              class="m-card love-card"
              role="button"
              tabindex="0"
              :aria-label="`展开消息：${m.title}`"
              :class="{ open: expanded.has(m.id) }"
              @click="open(m)"
              @keydown.enter="open(m)"
              @keydown.space.prevent="open(m)"
            >
              <span class="m-ico dim"><component :is="iconFor(m.messageType)" :size="20" /></span>
              <div class="m-body">
                <div class="m-top">
                  <span class="m-title">{{ m.title }}</span>
                </div>
                <div class="m-time">{{ fmt(m.createTime) }}</div>
                <div class="m-content" :class="{ clamp: !expanded.has(m.id) }">{{ m.content || '（无正文）' }}</div>
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
      </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick, type Component } from 'vue';
import { NButton } from 'naive-ui';
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
import { Mail, Gem, CheckCircle2, Heart, Star, Image as ImageIcon, PenLine } from 'lucide-vue-next';

const setting = useSettingStore();

const expanded = ref<Set<number>>(new Set());
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
});
onUnmounted(() => {
  if (timer) window.clearInterval(timer);
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
.m-card:hover { transform: translateY(-2px); box-shadow: var(--elev-2); }
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

@media (max-width: 767px) {
  .stats { grid-template-columns: 1fr; }
}
</style>
