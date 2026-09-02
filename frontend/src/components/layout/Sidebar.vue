<template>
  <aside class="sidebar" :class="{ collapsed }">
    <div class="brand">
      <span class="brand-mark"><Heart :size="18" :stroke-width="1.8" /></span>
      <GradientText v-if="!collapsed" tag="span" class="brand-name">我们的小世界</GradientText>
      <button v-if="!collapsed" class="bell" v-ripple @click="togglePanel" title="消息" aria-label="消息">
        <Mail :size="18" :stroke-width="1.8" />
        <span v-if="unread > 0" class="badge">{{ unread > 99 ? '99+' : unread }}</span>
      </button>
    </div>

    <div v-if="!collapsed && partner.status?.isBound && partner.status.partner" class="partner-chip">
      <HeartHandshake :size="14" :stroke-width="1.8" />
      <span>已绑定 {{ partner.status.partner.nickName }}</span>
    </div>

    <transition name="panel-pop">
      <div v-if="panelOpen" class="panel">
        <div class="panel-head">
          <button class="ph-title" @click="goMessage">消息通知</button>
          <div class="ph-right">
            <button v-if="unread > 0" class="ph-all" @click="markAllRead">全部已读</button>
            <button class="close" @click="panelOpen = false" aria-label="关闭消息面板">×</button>
          </div>
        </div>
        <div v-if="loading" class="skeleton">加载中…</div>
        <div v-else-if="!messages.length" class="empty">暂时没有消息</div>
        <ul v-else ref="msgList" class="msg-list">
          <li
            v-for="m in messages"
            :key="m.id"
            :class="[{ unread: !m.isRead }, { dismissed: dismissId === m.id }]"
            @click="markRead(m)"
            @touchstart.passive="onSwipeStart(m, $event)"
            @touchend="onSwipeEnd(m, $event)"
          >
            <div class="mt">{{ m.title }}</div>
            <div class="mc">{{ m.content }}</div>
            <div class="md">{{ fmt(m.createTime) }}</div>
            <span class="swipe-hint">← 滑动标记已读</span>
          </li>
        </ul>
      </div>
    </transition>

    <nav>
      <router-link
        v-for="item in items"
        :key="item.to"
        :to="item.to"
        class="nav-item"
        v-ripple
        :aria-label="item.label"
        @click="hapticForAction('tap'); onClickNav"
      >
        <span class="ico"><component :is="item.icon" :size="20" :stroke-width="1.8" /></span>
        <span v-if="!collapsed" class="lbl">{{ item.label }}</span>
        <span v-if="badgeMap[item.to]" class="nbadge" :class="badgeMap[item.to]!.type">
          <template v-if="badgeMap[item.to]!.type === 'count'">{{ badgeMap[item.to]!.value }}</template>
        </span>
      </router-link>
    </nav>
  </aside>
</template>
<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, nextTick } from 'vue';
import { gsap } from 'gsap';
import {
  Mail, HeartHandshake, Heart,
  Home, History, BookOpen, Star, ListChecks, MessageCircle, Sparkles, Image, CloudFog,
  Wallet, Coffee, Footprints, CalendarHeart, BarChart3, Smile, Settings,
} from 'lucide-vue-next';
import { useRouter } from 'vue-router';
import api from '@/utils/request';
import type { ApiResult, SystemMessageDto, AnniversaryDto } from '@/types';
import { useRealtime } from '@/composables/useRealtime';
import { useSettingStore } from '@/store/settingStore';
import { useNotifyStore } from '@/store/notifyStore';
import { usePartnerStore } from '@/store/partnerStore';
import { hapticForAction } from '@/composables/useHaptic';
import GradientText from '@/components/Common/GradientText.vue';
import * as msgApi from '@/api/message';

defineProps<{ collapsed?: boolean }>();
const emit = defineEmits<{ (e: 'navigated'): void }>();

const router = useRouter();
const partner = usePartnerStore();
const { partnerOnline, onSync } = useRealtime();

const unread = ref(0);
const nearSoon = ref(0); // 7 天内临近的纪念日数量
const messages = ref<SystemMessageDto[]>([]);
const panelOpen = ref(false);
const loading = ref(false);
const msgList = ref<HTMLElement>();
const dismissId = ref<number | null>(null);
const setting = useSettingStore();
const notify = useNotifyStore();
let timer: number | undefined;

const items = [
  { to: '/home', label: '首页', icon: Home },
  { to: '/timeline', label: '时间轴', icon: History },
  { to: '/diary', label: '日记', icon: BookOpen },
  { to: '/wish', label: '愿望', icon: Star },
  { to: '/todo', label: '待办', icon: ListChecks },
  { to: '/board', label: '留言板', icon: MessageCircle },
  { to: '/message', label: '消息', icon: Mail },
  { to: '/quiz', label: '默契问答', icon: Sparkles },
  { to: '/album', label: '相册', icon: Image },
  { to: '/conflict', label: '矛盾', icon: CloudFog },
  { to: '/account', label: '记账', icon: Wallet },
  { to: '/dateplan', label: '约会', icon: Coffee },
  { to: '/footprint', label: '足迹', icon: Footprints },
  { to: '/anniversary', label: '纪念日', icon: CalendarHeart },
  { to: '/stats', label: '我们的一年', icon: BarChart3 },
  { to: '/mood-calendar', label: '心情日历', icon: Smile },
  { to: '/setting', label: '设置', icon: Settings },
];

function fmt(s: string) {
  const d = new Date(s);
  return `${d.getMonth() + 1}/${d.getDate()} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
}
async function refreshUnread() {
  try {
    const { data } = await api.get('/message/unread/count');
    unread.value = (data as ApiResult<number>).data;
  } catch { /* 忽略 */ }
}
// 临近纪念日（7 天内）数量，用于「纪念日」导航项红点提醒
async function refreshNear() {
  try {
    const { data } = await api.get('/home/nearestanniversary', { params: { take: 5 } });
    const list = (data as ApiResult<AnniversaryDto[]>).data ?? [];
    nearSoon.value = list.filter((a) => (a.daysLeft ?? 999) <= 7).length;
  } catch { /* 忽略 */ }
}
// 导航项角标：消息未读数（数字）、纪念日临近（小红点）
const badgeMap = computed<Record<string, { type: 'count' | 'dot'; value?: string } | null>>(() => ({
  '/message': unread.value > 0
    ? { type: 'count', value: unread.value > 99 ? '99+' : String(unread.value) }
    : null,
  '/anniversary': nearSoon.value > 0 ? { type: 'dot' } : null,
}));
async function togglePanel() {
  panelOpen.value = !panelOpen.value;
  if (panelOpen.value) {
    loading.value = true;
    try {
      const { data } = await api.get('/message/list', { params: { page: 1, pageSize: 20 } });
      messages.value = (data as ApiResult<{ items: SystemMessageDto[] }>).data.items;
      await nextTick();
      animateList();
    } finally {
      loading.value = false;
    }
  }
}
function animateList() {
  if (!msgList.value || setting.reduceMotion) return;
  const items = msgList.value.querySelectorAll('li');
  if (items.length) {
    gsap.fromTo(
      items,
      { opacity: 0, y: 8 },
      { opacity: 1, y: 0, duration: 0.3, ease: 'power2.out', stagger: 0.04 }
    );
  }
}
function onClickNav() {
  emit('navigated');
}
function goMessage() {
  panelOpen.value = false;
  router.push('/message');
}
async function markAllRead() {
  if (unread.value === 0) return;
  try {
    await msgApi.readAll();
    messages.value.forEach((m) => (m.isRead = true));
    await refreshUnread();
    notify.success('已全部标记为已读');
  } catch { /* 忽略 */ }
}
async function markRead(m: SystemMessageDto) {
  if (m.isRead) return;
  try {
    await msgApi.readMessage(m.id);
    m.isRead = true;
    await refreshUnread();
  } catch { /* 忽略 */ }
}

/* 触摸滑动标记已读 */
let sx = 0;
function onSwipeStart(_m: SystemMessageDto, e: TouchEvent) {
  sx = e.touches[0].clientX;
}
function onSwipeEnd(m: SystemMessageDto, e: TouchEvent) {
  const dx = e.changedTouches[0].clientX - sx;
  if (dx < -60 && !m.isRead) {
    dismissId.value = m.id;
    setTimeout(() => {
      dismissId.value = null;
      markRead(m);
    }, 220);
  }
}

onMounted(() => {
  refreshUnread();
  refreshNear();
  onSync('message', refreshUnread); // 服务端提醒实时推送：即时刷新未读角标，不再依赖被动轮询
  onSync('anniversary', refreshNear); // 纪念日增删/临近变化 → 即时刷新红点
  partner.load();
  timer = window.setInterval(refreshUnread, 60000);
});
onUnmounted(() => {
  if (timer) window.clearInterval(timer);
});
</script>
<style scoped>
.sidebar {
  position: relative; width: 240px; min-height: 100dvh;
  /* 底部预留安全区：iOS 独立模式下抽屉 bottom:0 贴屏幕最底，
     不补 env(safe-area-inset-bottom) 会让最后一项（设置）落在 Home Indicator 之下被遮挡 */
  background: var(--color-surface); padding: calc(20px + env(safe-area-inset-top)) 14px calc(20px + env(safe-area-inset-bottom));
  display: flex; flex-direction: column;
  border-right: 1px solid var(--color-border);
}
.sidebar.collapsed { width: 72px; padding: calc(20px + env(safe-area-inset-top)) 10px; align-items: center; }

.brand { display: flex; align-items: center; gap: 10px; padding: 6px 8px 18px; }
.brand-mark {
  display: inline-flex; align-items: center; justify-content: center;
  width: 34px; height: 34px; border-radius: 10px; flex: 0 0 auto;
  color: var(--color-on-primary); background: linear-gradient(135deg, var(--color-rose) 0%, var(--color-rose-vivid) 100%);
}
.brand-name { font-weight: 600; font-size: 14px; color: var(--color-ink); letter-spacing: 0.01em; }
.collapsed .brand { padding: 6px 0 18px; }

.partner-chip {
  display: flex; align-items: center; gap: 6px; margin: -8px 0 12px; padding: 7px 10px;
  border-radius: 10px; font-size: 12px; color: var(--color-rose-text);
  background: var(--color-rose-soft);
}

.bell {
  margin-left: auto; position: relative; border: none; background: none;
  cursor: pointer; color: var(--color-ink-2); display: flex; align-items: center;
  padding: 6px; border-radius: 10px; transition: all var(--dur-micro) var(--ease-love);
}
.bell:hover { color: var(--color-rose-text); background: var(--color-rose-soft); }
.badge {
  position: absolute; top: 2px; right: 4px; background: var(--color-rose); color: var(--color-on-primary);
  font-size: 10px; line-height: 1; padding: 2px 5px; border-radius: 10px; font-family: var(--font-mono);
  animation: badgePulse 1.8s var(--ease-love) infinite;
}
@keyframes badgePulse {
  0%, 100% { box-shadow: 0 0 0 0 rgba(255, 111, 125, 0.45); }
  50% { box-shadow: 0 0 0 5px rgba(255, 111, 125, 0); }
}
.reduce-motion .badge { animation: none; }

.panel {
  position: absolute; top: 64px; left: 8px; right: 8px; width: auto; max-height: 70vh; overflow: auto;
  background: var(--color-surface); border-radius: 14px; padding: 6px; z-index: 60;
  border: 1px solid var(--color-border);
  box-shadow: var(--shadow-float);
  transform-origin: top right;
}
.panel-pop-enter-active { transition: opacity 0.24s var(--ease-love), transform 0.24s var(--ease-love); }
.panel-pop-leave-active { transition: opacity 0.18s ease, transform 0.18s ease; }
.panel-pop-enter-from,
.panel-pop-leave-to { opacity: 0; transform: translateY(-8px) scale(0.96); }
.panel-head { display: flex; justify-content: space-between; align-items: center; padding: 10px 12px; border-bottom: 1px solid var(--color-border); }
.ph-title {
  border: none; background: none; cursor: pointer; padding: 0;
  font-family: var(--font-mono); font-size: 0.72rem; font-weight: 500; letter-spacing: 0.08em;
  text-transform: uppercase; color: var(--color-ink-2); transition: color var(--dur-micro);
}
.ph-title:hover { color: var(--color-rose-text); }
.ph-right { display: flex; align-items: center; gap: 8px; }
.ph-all {
  border: none; background: var(--color-rose-soft); color: var(--color-rose-text);
  font-size: 11px; padding: 3px 9px; border-radius: 999px; cursor: pointer; transition: all var(--dur-micro);
}
.ph-all:active { transform: scale(0.95); }
.close { border: none; background: none; font-size: 18px; cursor: pointer; color: var(--color-ink-3); }
.msg-list { list-style: none; margin: 6px 0 0; padding: 0; }
.msg-list li {
  position: relative; overflow: hidden;
  padding: 10px 12px; margin-bottom: 6px; border-radius: 10px; cursor: pointer;
  background: var(--color-surface-2);
  transition: opacity 0.22s ease, transform 0.22s var(--ease-love);
}
.msg-list li.unread { background: var(--color-rose-soft); }
.msg-list li.dismissed { opacity: 0; transform: translateX(-40px); }
.swipe-hint {
  position: absolute; right: 10px; top: 50%; transform: translateY(-50%);
  font-size: 10px; color: var(--color-ink-3); opacity: 0; pointer-events: none;
  transition: opacity var(--dur-micro);
}
html:not(.reduce-motion) .msg-list li.unread:active .swipe-hint { opacity: 0.8; }
.mt { font-weight: 500; color: var(--color-ink); }
.mc { color: var(--color-ink-2); font-size: 13px; margin-top: 2px; }
.md { color: var(--color-ink-3); font-size: 11px; margin-top: 4px; font-family: var(--font-mono); }

.nav-item {
  display: flex; align-items: center; gap: 11px; padding: 10px 13px; border-radius: 12px;
  color: var(--color-ink-2); text-decoration: none; font-size: 14px; position: relative;
  transition: all var(--dur-pop) var(--ease-love);
}
.collapsed .nav-item { justify-content: center; gap: 0; padding: 11px; width: 100%; }
.nav-item:hover { color: var(--color-ink); background: var(--color-surface-2); }
/* 激活态：玫瑰浅底 + 主色文字 + 左侧 3px 强调条 */
.nav-item.router-link-active {
  color: var(--color-rose-text);
  background: var(--color-rose-soft);
  font-weight: 600;
}
.nav-item.router-link-active::before {
  content: ''; position: absolute; left: 0; top: 22%; bottom: 22%; width: 3px;
  border-radius: 3px; background: var(--color-rose);
}
.ico { width: 20px; display: flex; justify-content: center; }
.ico :deep(svg) { color: currentColor; }
.nbadge {
  position: absolute; top: 7px; right: 9px; z-index: 2;
  display: inline-flex; align-items: center; justify-content: center;
}
.nbadge.count {
  min-width: 16px; height: 16px; padding: 0 4px; border-radius: 999px;
  background: var(--color-rose); color: var(--color-on-primary); font-size: 10px; line-height: 16px;
  font-family: var(--font-mono); box-shadow: 0 0 0 2px var(--color-surface);
  animation: badgePulse 1.8s var(--ease-love) infinite;
}
.nbadge.dot {
  width: 9px; height: 9px; border-radius: 9999px; background: var(--color-rose);
  box-shadow: 0 0 0 2px var(--color-surface); animation: badgePulse 1.8s var(--ease-love) infinite;
}
.collapsed .nbadge { top: 6px; right: 8px; }
.reduce-motion .nbadge { animation: none; }
</style>
