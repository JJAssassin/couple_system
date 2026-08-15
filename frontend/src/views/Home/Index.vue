<template>
  <IndSkeleton v-if="loading" variant="hero" />
  <div v-else class="home" ref="container">
    <!-- 问候 hero -->
    <section class="hero block">
      <AuroraBackdrop class="hero-aurora" />
      <FloatingHearts class="hero-hearts" />
      <GradientText class="hero-greet" tag="div">{{ greet }}，{{ nickName }}</GradientText>
      <template v-if="loveInfo.hasLoveStart">
        <div class="hero-days"><LoveCount :value="loveInfo.totalDays" /> <span>天</span></div>
        <div class="hero-sub">你们已经相恋 {{ loveInfo.totalDays }} 天 · 精确 {{ loveInfo.totalHours }} 小时</div>
        <div class="hero-lovedate">
          <Heart :size="14" :stroke-width="2" />
          <span>{{ fmtDate(loveInfo.loveStartTime) }}</span>
          <span class="hero-edit" @click="openLoveEditor">修改</span>
        </div>
        <div v-if="showLoveEditor" class="hero-set-form">
          <input type="date" v-model="loveStartInput" class="love-input" :max="todayStr" />
          <NButton size="small" type="primary" :loading="savingLove" @click="saveLoveStart">保存</NButton>
          <NButton size="small" quaternary @click="showLoveEditor = false">取消</NButton>
        </div>
      </template>
      <template v-else>
        <div class="hero-set">
          <div class="hero-set-tip">还没有记录你们的相恋纪念日</div>
          <div v-if="!showLoveEditor" class="hero-set-cta" @click="openLoveEditor">＋ 设置相恋纪念日</div>
          <div v-else class="hero-set-form">
            <input type="date" v-model="loveStartInput" class="love-input" :max="todayStr" />
            <NButton size="small" type="primary" :loading="savingLove" @click="saveLoveStart">保存</NButton>
            <NButton size="small" quaternary @click="showLoveEditor = false">取消</NButton>
          </div>
        </div>
      </template>
    </section>

    <!-- 每日一句 -->
    <section class="block" v-if="quote.content">
      <IndSectionTitle label="每日一句" :led="true" />
      <IndCard class="quote-card">
        <span class="quote-mark">“</span>
        <p class="quote-text">{{ quote.content }}</p>
        <span class="quote-author" v-if="quote.author">—— {{ quote.author }}</span>
      </IndCard>
    </section>

    <!-- 今日与你（聚合卡） -->
    <section class="block">
      <IndSectionTitle label="今日与你" :led="true" />
      <div class="today-grid">
        <div class="today-card" :class="{ ok: nearest.length }" @click="go('anniversary')">
          <span class="tc-ico"><component :is="icAnniversary" :size="22" :stroke-width="1.8" /></span>
          <div class="tc-label">最近纪念日</div>
          <div class="tc-val">{{ nearest.length ? nearest[0].name + ' · ' + nearest[0].daysLeft + '天' : '未设置' }}</div>
        </div>
        <div class="today-card" :class="{ ok: unread > 0 }" @click="go('message')">
          <span class="tc-ico"><component :is="icMessage" :size="22" :stroke-width="1.8" /></span>
          <div class="tc-label">未读消息</div>
          <div class="tc-val">{{ unread > 0 ? unread + ' 条' : '暂无' }}</div>
        </div>
      </div>
    </section>

    <!-- 回忆轮播 -->
    <section class="block" v-if="albums.length">
      <IndSectionTitle label="回忆胶片" :led="true" />
      <div class="film">
        <div v-for="a in albums" :key="a.id" class="film-cell" @click="go('album')">
          <img v-if="a.cover" :src="a.cover" :alt="a.albumName" />
          <div v-else class="film-ph">{{ a.albumName.slice(0, 1) }}</div>
          <div class="film-cap">{{ a.albumName }} · {{ a.imageCount }}张</div>
        </div>
      </div>
    </section>

    <!-- 就近纪念日 -->
    <section class="block">
      <IndSectionTitle label="就近纪念日" :led="true" />
      <div v-if="nearest.length" class="cards">
        <IndCard v-for="a in nearest" :key="a.id" class="mini">
          <div class="name">
            {{ a.name }}
            <span v-if="a.isYearly" class="yr-badge">每年</span>
          </div>
          <div class="days">还有 <b>{{ a.daysLeft }}</b> 天</div>
          <div class="next" v-if="a.nextOccurrence">下次 {{ fmtMD(a.nextOccurrence) }}</div>
          <div class="next expired" v-else>已过去</div>
        </IndCard>
      </div>
      <IndEmpty v-else title="还没有纪念日" desc="在「设置 / 时间轴」里记下一个重要的日子吧" />
    </section>

    <!-- 统计看板 -->
    <section class="block">
      <IndSectionTitle label="心情趋势 · 近 30 天" :led="true" />
      <IndCard>
        <div class="screen">
          <ChartWrap :option="moodOption" />
        </div>
      </IndCard>
    </section>

    <section class="block">
      <IndSectionTitle label="矛盾趋势 · 近 6 月" :led="true" />
      <IndCard>
        <div class="screen">
          <ChartWrap :option="conflictOption" />
        </div>
      </IndCard>
    </section>

    <!-- 关键指标 -->
    <section class="block stat-row">
      <div class="stat-link" @click="go('wish')">
        <IndStatCard label="愿望完成率" :value="dashboard.wishCompleteRate + '%'" />
      </div>
      <div class="stat-link" @click="go('account')">
        <IndStatCard label="共同余额" :value="'¥' + dashboard.accountSummary.balance.toFixed(2)" />
      </div>
      <div class="stat-link" @click="go('diary')">
        <IndStatCard label="连续互动" :value="dashboard.activeStreakDays + ' 天'" />
      </div>
    </section>

    <!-- 最近动态 feed -->
    <section class="block" v-if="feed.length">
      <IndSectionTitle label="最近动态" :led="true" />
      <IndCard>
        <ul class="feed">
          <li v-for="f in feed" :key="f.id" class="feed-item">
            <span class="feed-ico"><component :is="feedIcon(f.type)" :size="18" :stroke-width="1.8" /></span>
            <div class="feed-body">
              <div class="feed-title">{{ f.title }}</div>
              <div class="feed-time">{{ f.date.slice(0, 10) }}</div>
            </div>
          </li>
        </ul>
      </IndCard>
    </section>
  </div>
</template>
<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import { NButton } from 'naive-ui';
import {
  Heart, Mail, BookOpen, Star, CalendarHeart, CloudFog, Image,
} from 'lucide-vue-next';
import api from '@/utils/request';
import type { ApiResult, LoveInfo, DashboardData, AnniversaryDto, TimelineItemDto, AlbumDto, DailyQuoteDto } from '@/types';
import type { EChartsOption } from 'echarts';
import LoveCount from '@/components/Common/LoveCount.vue';
import FloatingHearts from '@/components/Common/FloatingHearts.vue';
import AuroraBackdrop from '@/components/Common/AuroraBackdrop.vue';
import GradientText from '@/components/Common/GradientText.vue';
import ChartWrap from '@/components/ChartWrap.vue';
import IndCard from '@/components/industrial/IndCard.vue';
import IndStatCard from '@/components/industrial/IndStatCard.vue';
import IndSectionTitle from '@/components/industrial/IndSectionTitle.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import { useStaggerEnter } from '@/composables/useAnimation';
import { useAuthStore } from '@/store/authStore';
import { useNotifyStore } from '@/store/notifyStore';
import { useRealtime } from '@/composables/useRealtime';
import * as coupleApi from '@/api/couple';
import { getDailyQuote } from '@/api/quote';

const { onSync } = useRealtime();
const loading = ref(true);
const loveInfo = ref<LoveInfo>({ hasLoveStart: false, totalDays: 0, totalHours: 0, totalMinutes: 0, loveStartTime: '' });
const dashboard = ref<DashboardData>({ moodTrend: [], conflictTrend: [], wishCompleteRate: 0, accountSummary: { income: 0, expend: 0, balance: 0 }, activeStreakDays: 0 });
const nearest = ref<AnniversaryDto[]>([]);
const container = ref<HTMLElement>();
const router = useRouter();
const auth = useAuthStore();
const notify = useNotifyStore();

const nickName = computed(() => auth.profile?.nickName || '亲爱的');
const unread = ref(0);
const showLoveEditor = ref(false);
const loveStartInput = ref('');
const savingLove = ref(false);
const todayStr = new Date().toLocaleDateString('en-CA'); // YYYY-MM-DD（本地），防止选到未来日期

const icAnniversary = CalendarHeart;
const icMessage = Mail;
const FEED_ICONS: Record<string, any> = {
  Diary: BookOpen, Wish: Star, Anniversary: CalendarHeart, Conflict: CloudFog, Letter: Mail, Album: Image,
};

function openLoveEditor() {
  loveStartInput.value = loveInfo.value.loveStartTime ? loveInfo.value.loveStartTime.slice(0, 10) : '';
  showLoveEditor.value = true;
}
function fmtDate(s?: string | null) {
  if (!s) return '—';
  const d = new Date(s);
  return `${d.getFullYear()}年${d.getMonth() + 1}月${d.getDate()}日`;
}
async function reloadLoveInfo() {
  try {
    const { data } = await api.get('/home/loveinfo');
    loveInfo.value = (data as ApiResult<LoveInfo>).data;
  } catch { /* 忽略 */ }
}
async function saveLoveStart() {
  if (!loveStartInput.value) return;
  savingLove.value = true;
  try {
    await coupleApi.setLoveStart(loveStartInput.value);
    await reloadLoveInfo();
    showLoveEditor.value = false;
    notify.success(loveInfo.value.hasLoveStart ? '恋爱纪念日已更新，时长重新计算' : '相恋纪念日已记录，双方首页同步生效');
  } catch { /* 拦截器已提示 */ }
  finally { savingLove.value = false; }
}
const albums = ref<AlbumDto[]>([]);
const feed = ref<TimelineItemDto[]>([]);
const quote = ref<DailyQuoteDto>({ content: '' });

const hour = new Date().getHours();
const greet = computed(() => (hour < 6 ? '凌晨好' : hour < 12 ? '早安' : hour < 14 ? '午安' : hour < 18 ? '下午好' : '晚安'));

function go(name: string) {
  router.push({ name });
}
function feedIcon(type: string) {
  return FEED_ICONS[type] || Star;
}
function fmtMD(s?: string | null) {
  if (!s) return '—';
  const d = new Date(s);
  return `${d.getMonth() + 1}/${d.getDate()}`;
}

useStaggerEnter(container, '.block', { stagger: 0.1, y: 16 });

const moodOption = computed<EChartsOption>(() => ({
  xAxis: { type: 'category', data: dashboard.value.moodTrend.map((p) => p.label) },
  yAxis: { type: 'value', max: 10 },
  series: [{ type: 'line', smooth: true, data: dashboard.value.moodTrend.map((p) => p.value), areaStyle: { opacity: 0.15 }, itemStyle: { color: '#ff6f7d' }, lineStyle: { color: '#ff6f7d', width: 2 } }],
  grid: { left: 30, right: 16, top: 16, bottom: 24 },
}));

const conflictOption = computed<EChartsOption>(() => ({
  xAxis: { type: 'category', data: dashboard.value.conflictTrend.map((p) => p.label) },
  yAxis: { type: 'value' },
  series: [{ type: 'bar', data: dashboard.value.conflictTrend.map((p) => p.value), itemStyle: { color: '#9CA3AF', borderRadius: [6, 6, 0, 0] } }],
  grid: { left: 30, right: 16, top: 16, bottom: 24 },
}));

// 各区块独立拉取：任一接口失败只清空该区块，不连累整页（避免 Promise.all 单点失败导致首页全归零）
async function loadLoveInfo() {
  try { const { data } = await api.get('/home/loveinfo'); loveInfo.value = (data as ApiResult<LoveInfo>).data; } catch { /* 拦截器已提示 */ }
}
async function loadDashboard() {
  try { const { data } = await api.get('/home/dashboard'); dashboard.value = (data as ApiResult<DashboardData>).data; } catch { /* 拦截器已提示 */ }
}
async function loadNearest() {
  try { const { data } = await api.get('/home/nearestanniversary', { params: { take: 3 } }); nearest.value = (data as ApiResult<AnniversaryDto[]>).data; } catch { /* 拦截器已提示 */ }
}
async function loadUnread() {
  try { const { data } = await api.get('/message/unread/count'); unread.value = (data as ApiResult<number>).data ?? 0; } catch { /* 拦截器已提示 */ }
}
async function loadAlbums() {
  try { const { data } = await api.get('/album/list', { params: { page: 1, pageSize: 12 } }); const al = (data as ApiResult<{ items: AlbumDto[] }>).data; albums.value = al?.items ?? []; } catch { /* 拦截器已提示 */ }
}
async function loadFeed() {
  try { const { data } = await api.get('/timeline/list', { params: { page: 1, pageSize: 6 } }); feed.value = (data as ApiResult<TimelineItemDto[]>).data ?? []; } catch { /* 拦截器已提示 */ }
}
async function loadQuote() {
  try { const { data } = await api.get('/quote/today'); quote.value = (data as ApiResult<DailyQuoteDto>).data ?? { content: '' }; } catch { /* 拦截器已提示 */ }
}

onMounted(async () => {
  // 各子请求各自 try/catch，Promise.all 必然 resolve，单点失败不再整页归零
  await Promise.all([
    loadLoveInfo(), loadDashboard(), loadNearest(), loadUnread(),
    loadAlbums(), loadFeed(), loadQuote(),
  ]);
  loading.value = false;
  onSync('setting', reloadLoveInfo);
  onSync('message', loadUnread); // 服务端提醒实时推送：即时刷新未读角标，不再依赖被动轮询
});
</script>
<style scoped>
.home { max-width: 880px; margin: 0 auto; }
.hero { position: relative; text-align: center; padding: 28px 0 8px; overflow: hidden; }
.hero-hearts { z-index: 0; }
.hero > :not(.hero-hearts):not(.hero-aurora) { position: relative; z-index: 1; }
.hero-aurora { z-index: 0; }
.hero-greet {
  font-size: 22px; font-weight: 700; letter-spacing: 0.01em; margin-bottom: 2px;
  display: inline-block;
}
.hero-days {
  font-size: 52px; font-weight: 600; color: var(--color-rose);
  display: flex; align-items: baseline; justify-content: center; gap: 8px;
  margin-top: 6px;
  animation: heartbeat 2.6s var(--ease-love) infinite;
}
@keyframes heartbeat {
  0%, 100% { transform: scale(1); }
  14% { transform: scale(1.06); }
  28% { transform: scale(1); }
  42% { transform: scale(1.05); }
  70% { transform: scale(1); }
}
.reduce-motion .hero-days { animation: none; }
.hero-days span { font-size: 20px; color: var(--color-ink-3); }
.hero-sub { color: var(--color-ink-3); font-size: 13px; }
.hero-lovedate {
  display: inline-flex; align-items: center; gap: 7px; margin-top: 10px;
  font-size: 13px; color: var(--color-ink-2);
  padding: 5px 14px; border-radius: 999px;
  background: var(--color-rose-soft); border: 1px solid var(--color-border);
}
.hero-lovedate :deep(svg) { color: var(--color-rose); }
.hero-edit {
  color: var(--color-rose); cursor: pointer; font-size: 12px; font-weight: 600;
  border-bottom: 1px dashed var(--color-rose); padding-bottom: 1px;
}
.hero-edit:hover { opacity: 0.8; }
.hero-set { margin-top: 6px; }
.hero-set-tip { color: var(--color-ink-3); font-size: 14px; }
.hero-set-cta {
  display: inline-block; margin-top: 10px; padding: 8px 16px; border-radius: 999px; cursor: pointer;
  color: var(--color-rose); font-size: 14px; font-weight: 600;
  background: var(--color-rose-soft); border: 1px solid var(--color-border);
  transition: all var(--dur-micro) var(--ease-love);
}
.hero-set-cta:hover { background: var(--color-rose); color: #fff; border-color: var(--color-rose); }
.hero-set-form { display: flex; align-items: center; gap: 8px; margin-top: 10px; flex-wrap: wrap; justify-content: center; }
.love-input {
  padding: 7px 10px; border-radius: 10px; border: 1px solid var(--color-border);
  background: var(--color-surface); color: var(--color-ink); font-size: 13px;
}
.block { margin: 22px 0; }

/* 今日与你 */
.today-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 12px; }
.today-card {
  background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-md); padding: 16px;
  box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04), 0 10px 28px -10px rgba(122, 100, 98, 0.16);
  display: flex; flex-direction: column; gap: 4px; cursor: pointer; transition: transform var(--dur-pop) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love);
}
.today-card:hover { transform: translateY(-3px); }
.today-card.ok { animation: cardGlow 2.8s ease-in-out infinite; }
@keyframes cardGlow {
  0%, 100% { box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04), 0 10px 28px -10px rgba(122, 100, 98, 0.16); }
  50% { box-shadow: 0 0 0 2px rgba(255, 111, 125, 0.35), 0 10px 28px -10px rgba(122, 100, 98, 0.16); }
}
.reduce-motion .today-card.ok { animation: none; }
.tc-ico { color: var(--color-rose); display: inline-flex; }
.tc-label { font-size: 13px; color: var(--color-ink-2); }
.tc-val { font-weight: 600; font-size: 14px; color: var(--color-ink); }

/* 回忆胶片 */
.film { display: flex; gap: 12px; overflow-x: auto; padding-bottom: 8px; scroll-snap-type: x mandatory; }
.film-cell { flex: 0 0 150px; scroll-snap-align: start; cursor: pointer; }
.film-cell img, .film-ph {
  width: 150px; height: 110px; object-fit: cover; border-radius: var(--radius-md);
  background: var(--color-ink-soft); display: grid; place-items: center; font-size: 32px; color: var(--color-ink-3);
  box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04), 0 10px 28px -10px rgba(122, 100, 98, 0.16);
  transition: transform var(--dur-pop) var(--ease-love);
}
.film-cell:hover img, .film-cell:hover .film-ph { transform: translateY(-3px); }
.film-cap { font-size: 12px; color: var(--color-ink-3); margin-top: 6px; text-align: center; }

.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 12px; }
.mini .name { font-weight: 500; display: flex; align-items: center; gap: 6px; }
.mini .days { color: var(--color-ink-3); font-size: 13px; margin-top: 4px; }
.mini .days b { color: var(--color-rose); }
.yr-badge {
  font-size: 10px; padding: 1px 7px; border-radius: 999px; font-weight: 600;
  color: #fff; background: var(--color-rose); letter-spacing: 0.04em; line-height: 1.6;
}
.mini .next { font-size: 11px; color: var(--color-ink-3); margin-top: 4px; font-family: var(--font-mono); }
.mini .next.expired { color: var(--color-ink-3); }
.stat-row { display: grid; grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); gap: 12px; }
.stat-link { cursor: pointer; transition: transform var(--dur-pop) var(--ease-love); }
.stat-link:hover { transform: translateY(-3px); }

/* feed */
.feed { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 4px; }
.feed-item { display: flex; align-items: center; gap: 12px; padding: 10px 4px; border-bottom: 1px solid var(--color-border); }
.feed-item:last-child { border-bottom: none; }
.feed-ico { color: var(--color-rose); display: inline-flex; }
.feed-title { font-size: 14px; color: var(--color-ink); }
.feed-time { font-size: 12px; color: var(--color-ink-3); }

/* 图表容器 */
.screen { position: relative; border-radius: var(--radius-md); padding: 8px; }

/* 每日一句 */
.quote-card { padding: 20px 22px; position: relative; overflow: hidden; }
.quote-mark {
  position: absolute; top: -6px; left: 10px; font-size: 64px; line-height: 1;
  color: var(--color-rose); opacity: 0.18; font-family: Georgia, serif;
}
.quote-text {
  position: relative; font-size: 16px; line-height: 1.8; color: var(--color-ink);
  margin: 6px 0 0; padding-left: 8px; letter-spacing: 0.02em;
}
.quote-author {
  display: block; margin-top: 10px; text-align: right; font-size: 13px; color: var(--color-ink-3);
}
</style>
