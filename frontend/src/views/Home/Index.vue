<template>
  <IndSkeleton v-if="loading" variant="hero" />
  <PullRefresh v-else @refresh="onRefresh">
    <div class="home" ref="container">
    <!-- 圆整节点庆祝横幅 -->
    <transition name="cele">
      <div v-if="celebrate" class="cele-banner">
        <PartyPopper :size="18" class="cele-ico" />
        <span class="cele-txt">{{ celebrate }}</span>
        <button class="cele-close" aria-label="关闭" @click="celebrate = ''"><X :size="16" /></button>
      </div>
    </transition>

    <!-- 问候 hero -->
    <section class="hero block">
      <AuroraBackdrop class="hero-aurora" />
      <div class="hero-blob" />
      <FloatingHearts class="hero-hearts" />
      <GradientText class="hero-greet" tag="h1">{{ greet }}，{{ nickName }}</GradientText>
      <template v-if="loveInfo.hasLoveStart">
        <div class="hero-days"><LoveCount :value="loveInfo.totalDays" /> <span>天</span></div>
        <div class="hero-sub">你们已经相恋 {{ loveInfo.totalDays }} 天 · 精确 {{ loveInfo.totalHours }} 小时</div>
        <div class="hero-lovedate">
          <Heart :size="14" />
          <span>{{ fmtDate(loveInfo.loveStartTime) }}</span>
          <button class="hero-edit" type="button" @click="openLoveEditor">修改</button>
        </div>
      </template>
      <template v-else>
        <div class="hero-set">
          <div class="hero-set-tip">还没有记录你们的相恋纪念日</div>
          <button v-if="!showLoveEditor" class="hero-set-cta" type="button" @click="openLoveEditor">＋ 设置相恋纪念日</button>
        </div>
      </template>
      <!-- 相恋纪念日编辑表单：v-if / v-else 两分支共用，仅渲染一次，消除重复 -->
      <div v-if="showLoveEditor" class="hero-set-form">
        <input type="date" v-model="loveStartInput" class="love-input" :max="todayStr" />
        <NButton size="small" type="primary" :loading="savingLove" v-press-bounce @click="saveLoveStart">保存</NButton>
        <NButton size="small" quaternary @click="showLoveEditor = false">取消</NButton>
      </div>
    </section>

    <!-- 恋爱里程碑 -->
    <MilestoneStrip
      v-if="loveInfo.hasLoveStart"
      :total-days="loveInfo.totalDays"
      :love-start-time="loveInfo.loveStartTime"
    />

    <!-- 每日一句 -->
    <section class="block" v-if="quote.content">
      <IndSectionTitle label="每日一句" :led="true" />
      <IndCard class="quote-card">
        <button class="quote-shuffle" :class="{ beat: quoteBeat }" @click="shuffleQuote" title="换一句情话" aria-label="换一句情话">
          <Heart :size="15" />
        </button>
        <span class="quote-mark">“</span>
        <p class="quote-text">{{ quote.content }}</p>
        <span class="quote-author" v-if="quote.author">—— {{ quote.author }}</span>
        <span class="quote-hint" v-else>—— <Heart :size="12" class="q-heart" /> 换一句</span>
      </IndCard>
    </section>

    <!-- 今日与你（聚合卡） -->
    <section class="block">
      <IndSectionTitle label="今日与你" :led="true" />
      <div class="today-grid">
        <button class="today-card" type="button" :class="{ ok: nearest.length && nearest[0].daysLeft <= 7 }" aria-label="查看最近纪念日" @click="go('anniversary')">
          <span class="tc-ico"><component :is="icAnniversary" :size="22" /></span>
          <div class="tc-label">最近纪念日</div>
          <div v-if="nearest.length" class="tc-val" :class="{ 'tc-big': nearest[0].daysLeft <= 7 }">
            <template v-if="nearest[0].daysLeft === 0">就是今天！{{ nearest[0].name }}</template>
            <template v-else>还有 {{ nearest[0].daysLeft }} 天 · {{ nearest[0].name }}</template>
          </div>
          <div v-else class="tc-val">未设置</div>
        </button>
        <button class="today-card" type="button" :class="{ ok: unread > 0 }" aria-label="查看未读消息" @click="go('message')">
          <span class="tc-ico"><component :is="icMessage" :size="22" /></span>
          <div class="tc-label">未读消息</div>
          <div class="tc-val">{{ unread > 0 ? unread + ' 条' : '暂无' }}</div>
        </button>
      </div>
    </section>

    <!-- 回忆轮播 -->
    <section class="block" v-if="albums.length">
      <IndSectionTitle label="回忆胶片" :led="true" />
      <div class="film">
        <button v-for="a in albums" :key="a.id" class="film-cell" type="button" :aria-label="'查看相册 ' + a.albumName" @click="go('album')">
          <img v-if="a.cover" :src="a.cover" :alt="a.albumName" loading="lazy" @error="onAlbumCoverError(a)" />
          <div v-else class="film-ph">{{ a.albumName.slice(0, 1) }}</div>
          <div class="film-cap">{{ a.albumName }} · {{ a.imageCount }}张</div>
        </button>
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
          <div class="next" v-if="a.nextOccurrence">下次 {{ fmtMD(a.nextOccurrence) }}<span v-if="a.lunarDate" class="hm-lunar">{{ a.lunarDate }}</span></div>
          <div class="next expired" v-else>已过去</div>
        </IndCard>
      </div>
      <IndEmpty v-else title="还没有纪念日" desc="在「设置 / 时间轴」里记下一个重要的日子吧" />
    </section>

    <!-- 趋势数据带：双图并列，缩短纵向、制造节奏层次 -->
    <section class="block">
      <IndSectionTitle label="数据趋势" :led="true" />
      <div class="trend-grid">
        <IndCard class="trend-cell">
          <div class="viz-title">心情趋势 · 近 30 天</div>
          <div class="screen">
            <ChartWrap :option="moodOption" />
          </div>
        </IndCard>
        <IndCard class="trend-cell">
          <div class="viz-title">矛盾趋势 · 近 6 月</div>
          <div class="screen">
            <ChartWrap :option="conflictOption" />
          </div>
        </IndCard>
      </div>
    </section>

    <!-- 关键指标 -->
    <section class="block stat-row">
      <button class="stat-link" type="button" aria-label="查看愿望完成率" @click="go('wish')">
        <IndStatCard label="愿望完成率" :value="dashboard.wishCompleteRate + '%'" />
      </button>
      <button class="stat-link" type="button" aria-label="查看共同余额" @click="go('account')">
        <IndStatCard label="共同余额" :value="'¥' + (dashboard.accountSummary?.balance ?? 0).toFixed(2)" />
      </button>
      <button class="stat-link" type="button" aria-label="查看连续互动" @click="go('diary')">
        <IndStatCard label="连续互动" :value="dashboard.activeStreakDays + ' 天'" />
      </button>
    </section>

    <!-- 数据可视化大屏：愿望完成率仪表盘 + 共同收支环形图 -->
    <section class="block">
      <IndSectionTitle label="关系数据 · 一目了然" :led="true" />
      <div class="viz-grid">
        <IndCard as="button" type="button" class="viz-card" aria-label="查看愿望完成率" @click="go('wish')">
          <div class="viz-title">愿望完成率</div>
          <ChartWrap :option="wishGaugeOption" height="210px" />
        </IndCard>
        <IndCard as="button" type="button" class="viz-card" aria-label="查看共同收支" @click="go('account')">
          <div class="viz-title">共同收支</div>
          <ChartWrap :option="accountDonutOption" height="210px" />
        </IndCard>
      </div>
      <div class="viz-hint">点击卡片可前往对应模块 · 数据随你们的使用实时更新</div>
    </section>

    <!-- 最近动态 feed -->
    <section class="block" v-if="feed.length">
      <IndSectionTitle label="最近动态" :led="true" />
      <IndCard>
        <ul class="feed">
          <li v-for="f in feed" :key="f.id" class="feed-item">
            <span class="feed-ico"><component :is="feedIcon(f.type)" :size="18" /></span>
            <div class="feed-body">
              <div class="feed-title">{{ f.title }}</div>
              <div class="feed-time">{{ f.date.slice(0, 10) }}</div>
            </div>
          </li>
        </ul>
      </IndCard>
    </section>
  </div>
  </PullRefresh>
</template>
<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue';
import { useRouter } from 'vue-router';
import { NButton } from 'naive-ui';
import {
  Heart, Mail, BookOpen, Star, CalendarHeart, CloudFog, Image, PartyPopper, X,
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
import MilestoneStrip from '@/components/Common/MilestoneStrip.vue';
import { useStaggerEnter } from '@/composables/useAnimation';
import { useAuthStore } from '@/store/authStore';
import { useNotifyStore } from '@/store/notifyStore';
import { useSettingStore } from '@/store/settingStore';
import { useRealtime } from '@/composables/useRealtime';
import * as coupleApi from '@/api/couple';
import { getDailyQuote } from '@/api/quote';
import PullRefresh from '@/components/Common/PullRefresh.vue';
import { hapticForAction } from '@/composables/useHaptic';

const { onSync } = useRealtime();
const loading = ref(true);
const refreshing = ref(false);
const loveInfo = ref<LoveInfo>({ hasLoveStart: false, totalDays: 0, totalHours: 0, totalMinutes: 0, loveStartTime: '' });
const dashboard = ref<DashboardData>({ moodTrend: [], conflictTrend: [], wishCompleteRate: 0, accountSummary: { income: 0, expend: 0, balance: 0 }, activeStreakDays: 0 });
const nearest = ref<AnniversaryDto[]>([]);
const container = ref<HTMLElement>();
const router = useRouter();
const auth = useAuthStore();
const notify = useNotifyStore();
const setting = useSettingStore();

const nickName = computed(() => auth.profile?.nickName || '亲爱的');
const unread = ref(0);

// 圆整节点彩蛋：在一起天数逢 100 的倍数或整周年（365 的倍数）时，全屏心动 + 顶部横幅庆祝（每天仅展示一次）
const celebrate = ref('');
function milestoneLabel(d: number): string {
  if (d % 365 === 0) return `在一起 ${d} 天 · 整 ${d / 365} 周年快乐`;
  if (d % 100 === 0) return `在一起 ${d} 天 · 小小里程碑`;
  return '';
}
function checkMilestone() {
  if (!loveInfo.value.hasLoveStart) return;
  const d = loveInfo.value.totalDays;
  const label = milestoneLabel(d);
  if (!label) return;
  const today = toDateStr();
  const key = `cl_cele_${d}_${today}`;
  if (localStorage.getItem(key)) return; // 当天已庆祝过
  localStorage.setItem(key, '1');
  celebrate.value = label;
  if (!setting.reduceMotion) {
    window.dispatchEvent(new CustomEvent('cl-heartburst', { detail: { x: window.innerWidth / 2, y: window.innerHeight * 0.35 } }));
  }
}
const showLoveEditor = ref(false);
const loveStartInput = ref('');
const savingLove = ref(false);
const todayStr = toDateStr(); // YYYY-MM-DD（本地），防止选到未来日期

/** 本地时区稳定的 YYYY-MM-DD，避免 toLocaleDateString 依赖区域数据/时区漂移 */
function toDateStr(d: Date = new Date()): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

const icAnniversary = CalendarHeart;
const icMessage = Mail;
const FEED_ICONS: Record<string, any> = {
  Diary: BookOpen, Wish: Star, Anniversary: CalendarHeart, Conflict: CloudFog, Album: Image,
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

const quoteBeat = ref(false);
// 本地治愈系短句库：点击 ♥ 随机换一句，丰富首页情趣（不依赖后端）
const extraQuotes = [
  '世界很大，但和你在一起的地方就是家。',
  '想把每天的晚安，都变成见到你的早安。',
  '你的小脾气，也是我喜欢的样子。',
  '慢慢来，我们的故事还很长很长。',
  '被人放在心上，是这世上最温柔的事。',
  '今天也想和你一起虚度时光。',
  '你是我所有计划里，最重要的一项。',
  '喜欢你，是我做过最不后悔的决定。',
  '哪怕什么也不做，只要是你身边就很好。',
  '我们的日子，是把平凡过成糖。',
  '想把一年四季，都和你一起走过。',
  '你一笑，我的世界就亮了。',
  '在一起越久，越觉得当初选对了人。',
  '余生很长，请多指教呀。',
  '你是我的意外之喜，也是命中注定。',
  '无论晴雨，有你在就是好天气。',
];
let quoteTimer: number | null = null;
function shuffleQuote() {
  if (extraQuotes.length === 0) return;
  hapticForAction('tap');
  let q = quote.value.content;
  let guard = 0;
  while (q === quote.value.content && guard++ < 12) {
    q = extraQuotes[Math.floor(Math.random() * extraQuotes.length)];
  }
  quote.value = { content: q, author: '' };
  quoteBeat.value = false;
  requestAnimationFrame(() => { quoteBeat.value = true; });
  // 保存句柄并在下次进入/卸载时清理，避免切换页面后产生悬空调用
  if (quoteTimer !== null) clearTimeout(quoteTimer);
  quoteTimer = window.setTimeout(() => { quoteBeat.value = false; }, 650);
}

/** 相册封面加载失败：清除 cover 回退到首字母占位，避免裂图 */
function onAlbumCoverError(a: AlbumDto) {
  a.cover = '';
}

const hour = new Date().getHours();
const greet = computed(() => (hour < 6 ? '凌晨好' : hour < 12 ? '早安' : hour < 14 ? '午安' : hour < 18 ? '下午好' : '晚安'));

function go(name: string) {
  router.push('/' + name);
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
  series: [{ type: 'line', smooth: true, data: dashboard.value.moodTrend.map((p) => p.value), areaStyle: { opacity: 0.15 }, itemStyle: { color: 'var(--color-rose)' }, lineStyle: { color: 'var(--color-rose)', width: 2 } }],
  grid: { left: 30, right: 16, top: 16, bottom: 24 },
}));

const conflictOption = computed<EChartsOption>(() => ({
  xAxis: { type: 'category', data: dashboard.value.conflictTrend.map((p) => p.label) },
  yAxis: { type: 'value' },
  series: [{ type: 'bar', data: dashboard.value.conflictTrend.map((p) => p.value), itemStyle: { color: '#9CA3AF', borderRadius: [6, 6, 0, 0] } }],
  grid: { left: 30, right: 16, top: 16, bottom: 24 },
}));

// 愿望完成率仪表盘（玫瑰渐变）
const wishGaugeOption = computed<EChartsOption>(() => ({
  series: [{
    type: 'gauge', startAngle: 210, endAngle: -30, min: 0, max: 100,
    radius: '98%', center: ['50%', '56%'],
    progress: { show: true, width: 16, roundCap: true, itemStyle: { color: 'var(--color-rose)' } },
    axisLine: { lineStyle: { width: 16, color: [[1, 'rgba(255,111,125,0.14)']] } },
    pointer: { show: false }, axisTick: { show: false }, splitLine: { show: false }, axisLabel: { show: false },
    anchor: { show: false },
    detail: {
      valueAnimation: true, formatter: '{value}%', color: 'var(--color-rose)', fontSize: 30, fontWeight: 800,
      offsetCenter: [0, '2%'],
    },
    title: { show: true, offsetCenter: [0, '32%'], color: 'var(--color-ink-3)', fontSize: 12 },
    data: [{ value: Math.round(dashboard.value.wishCompleteRate), name: '已达成 / 总愿望' }],
  }],
}));

// 共同收支环形图（收入 vs 支出）
const accountDonutOption = computed<EChartsOption>(() => {
  const a = dashboard.value.accountSummary ?? { income: 0, expend: 0, balance: 0 };
  const empty = (a.income <= 0 && a.expend <= 0);
  return {
    tooltip: { trigger: 'item', valueFormatter: (v: unknown) => '¥' + Math.round(Number(v)).toLocaleString('zh-CN') },
    legend: { bottom: 0, icon: 'circle', itemWidth: 8, itemHeight: 8, textStyle: { color: 'var(--color-ink-3)', fontSize: 12 } },
    series: [{
      type: 'pie', radius: ['46%', '72%'], center: ['50%', '44%'],
      label: { show: !empty, formatter: '{b}\n{d}%', color: 'var(--color-ink)', fontSize: 12 },
      labelLine: { show: !empty, length: 8, length2: 8 },
      itemStyle: { borderColor: 'var(--color-surface)', borderWidth: 2, borderRadius: 6 },
      data: empty
        ? [{ name: '暂无记账', value: 1, itemStyle: { color: 'rgba(122,100,98,0.18)' } }]
        : [
            { name: '收入', value: a.income, itemStyle: { color: '#16a34a' } },
            { name: '支出', value: a.expend, itemStyle: { color: '#dc2626' } },
          ],
    }],
  };
});

// 各区块独立拉取：任一接口失败只清空该区块，不连累整页（避免 Promise.all 单点失败导致首页全归零）
async function loadLoveInfo() {
  try { const { data } = await api.get('/home/loveinfo'); loveInfo.value = (data as ApiResult<LoveInfo>).data; } catch { /* 拦截器已提示 */ }
}
async function loadDashboard() {
  try {
    const { data } = await api.get('/home/dashboard');
    const d = (data as ApiResult<DashboardData>).data;
    // 归一化：以默认值打底，后端数据覆盖；兜底 accountSummary 为 null，防止 .balance 抛 TypeError 白屏
    const base: DashboardData = {
      moodTrend: [], conflictTrend: [], wishCompleteRate: 0,
      accountSummary: { income: 0, expend: 0, balance: 0 }, activeStreakDays: 0,
    };
    dashboard.value = d ? { ...base, ...d } : base;
    if (!dashboard.value.accountSummary) {
      dashboard.value.accountSummary = { income: 0, expend: 0, balance: 0 };
    }
  } catch { /* 拦截器已提示 */ }
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

/** 下拉刷新：重载首页所有数据。done 由 PullRefresh 传入，必须调用以收起指示器 */
async function onRefresh(done?: () => void) {
  refreshing.value = true;
  try {
    await Promise.all([
      loadLoveInfo(), loadDashboard(), loadNearest(), loadUnread(),
      loadAlbums(), loadFeed(), loadQuote(),
    ]);
    checkMilestone();
  } finally {
    refreshing.value = false;
    done?.();
  }
}

onMounted(async () => {
  // 各子请求各自 try/catch，Promise.all 必然 resolve，单点失败不再整页归零
  await Promise.all([
    loadLoveInfo(), loadDashboard(), loadNearest(), loadUnread(),
    loadAlbums(), loadFeed(), loadQuote(),
  ]);
  loading.value = false;
  checkMilestone(); // 数据就绪后判断是否需要展示圆整节点彩蛋
  onSync('setting', reloadLoveInfo);
  onSync('message', loadUnread); // 服务端提醒实时推送：即时刷新未读角标，不再依赖被动轮询
});

onUnmounted(() => {
  if (quoteTimer !== null) clearTimeout(quoteTimer);
});
</script>
<style scoped>
.home { max-width: 880px; margin: 0 auto; }
.hero { position: relative; text-align: center; padding: 28px 0 8px; overflow: hidden; border-radius: var(--radius-lg, 24px); }
.hero-hearts { z-index: 0; }
.hero > :not(.hero-hearts):not(.hero-aurora):not(.hero-blob) { position: relative; z-index: 1; }
.hero-aurora { z-index: 0; }
.hero-blob {
  position: absolute; left: -10%; right: -10%; top: -50px; height: 240px; z-index: 0;
  background:
    radial-gradient(60% 100% at 28% 0%, color-mix(in srgb, var(--color-rose) 16%, transparent), transparent 70%),
    radial-gradient(50% 100% at 78% 12%, color-mix(in srgb, var(--color-cocoa) 12%, transparent), transparent 70%);
  filter: blur(10px); opacity: 0.9; pointer-events: none;
}
.reduce-motion .hero-blob { filter: none; }

/* 圆整节点庆祝横幅 */
.cele-banner {
  display: flex; align-items: center; gap: 8px; justify-content: center;
  margin: 0 auto 18px; max-width: 520px; padding: 10px 14px; border-radius: 999px;
  color: var(--color-on-primary); font-weight: 600; font-size: 14px;
  background: linear-gradient(135deg, var(--color-rose), var(--color-rose-deep));
  box-shadow: 0 8px 24px -8px rgba(255, 111, 125, 0.5);
  position: relative;
}
.cele-ico { display: inline-flex; flex: 0 0 auto; }
.cele-txt { flex: 1; text-align: center; }
.cele-close {
  flex: 0 0 auto; border: none; background: rgba(255, 255, 255, 0.22); color: var(--color-on-primary);
  width: 22px; height: 22px; border-radius: 999px; cursor: pointer;
  display: inline-flex; align-items: center; justify-content: center; padding: 0;
  transition: background var(--dur-micro) var(--ease-love);
}
.cele-close:hover { background: rgba(255, 255, 255, 0.36); }
.cele-enter-active, .cele-leave-active { transition: all var(--dur-pop) var(--ease-love); }
.cele-enter-from, .cele-leave-to { opacity: 0; transform: translateY(-10px) scale(0.96); }
.hero-greet {
  font-size: 22px; font-weight: 700; letter-spacing: 0.01em; margin-bottom: 2px;
  display: inline-block;
}
.hero-days {
  font-size: 52px; font-weight: 900;
  display: flex; align-items: baseline; justify-content: center; gap: 8px;
  margin-top: 6px;
  /* 渐变大字：品牌色渐变 + 等宽数字（"天" 后缀由 .hero-days span 单独着色） */
  background: linear-gradient(135deg, var(--color-rose) 0%, var(--color-rose-deep) 100%);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
  color: transparent;
  font-variant-numeric: tabular-nums;
  font-feature-settings: "tnum" 1;
  letter-spacing: -0.03em;
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
.hero-lovedate :deep(svg) { color: var(--color-rose-text); }
.hero-edit {
  color: var(--color-rose-text); cursor: pointer; font-size: 12px; font-weight: 600;
  border-bottom: 1px dashed var(--color-rose); padding-bottom: 1px;
}
.hero-edit:hover { opacity: 0.8; }
.hero-set { margin-top: 6px; }
.hero-set-tip { color: var(--color-ink-3); font-size: 14px; }
.hero-set-cta {
  display: inline-block; margin-top: 10px; padding: 8px 16px; border-radius: 999px; cursor: pointer;
  color: var(--color-rose-text); font-size: 14px; font-weight: 600;
  background: var(--color-rose-soft); border: 1px solid var(--color-border);
  transition: all var(--dur-micro) var(--ease-love);
}
.hero-set-cta:hover { background: var(--color-rose); color: var(--color-on-primary); border-color: var(--color-rose); }
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
  box-shadow: var(--shadow-card);
  display: flex; flex-direction: column; gap: 4px; cursor: pointer; transition: transform var(--dur-pop) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love);
}
.today-card:hover { transform: translateY(-3px); }
.today-card.ok { animation: cardGlow 2.8s ease-in-out infinite; }
@keyframes cardGlow {
  0%, 100% { box-shadow: var(--shadow-card); }
  50% { box-shadow: 0 0 0 2px rgba(255, 111, 125, 0.35), var(--shadow-card); }
}
.reduce-motion .today-card.ok { animation: none; }
.tc-ico { color: var(--color-rose-text); display: inline-flex; }
.tc-label { font-size: 13px; color: var(--color-ink-2); }
.tc-val { font-weight: 600; font-size: 14px; color: var(--color-ink); }
/* 临近纪念日（≤7 天）：大字玫瑰色强调 */
.tc-val.tc-big { font-size: 19px; font-weight: 800; color: var(--color-rose-text); line-height: 1.35; }

/* 回忆胶片 */
.film { display: flex; gap: 12px; overflow-x: auto; padding-bottom: 8px; scroll-snap-type: x mandatory; }
.film-cell { flex: 0 0 150px; scroll-snap-align: start; cursor: pointer; }
.film-cell img, .film-ph {
  width: 150px; height: 110px; object-fit: cover; border-radius: var(--radius-md);
  background: var(--color-ink-soft); display: grid; place-items: center; font-size: 32px; color: var(--color-ink-3);
  box-shadow: var(--shadow-card);
  transition: transform var(--dur-pop) var(--ease-love);
}
.film-cell:hover img, .film-cell:hover .film-ph { transform: translateY(-3px); }
.film-cap { font-size: 12px; color: var(--color-ink-3); margin-top: 6px; text-align: center; }

.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 12px; }
.mini .name { font-weight: 500; display: flex; align-items: center; gap: 6px; }
.mini .days { color: var(--color-ink-3); font-size: 13px; margin-top: 4px; }
.mini .days b { color: var(--color-rose-text); }
.yr-badge {
  font-size: 10px; padding: 1px 7px; border-radius: 999px; font-weight: 600;
  color: var(--color-on-primary); background: var(--color-rose); letter-spacing: 0.04em; line-height: 1.6;
}
.mini .next { font-size: 11px; color: var(--color-ink-3); margin-top: 4px; font-family: var(--font-mono); }
.mini .next.expired { color: var(--color-ink-3); }
.hm-lunar { color: var(--color-rose-text); font-weight: 600; margin-left: 4px; }
.stat-row { display: grid; grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); gap: 12px; }
.stat-link { cursor: pointer; transition: transform var(--dur-pop) var(--ease-love); }
.stat-link:hover { transform: translateY(-3px); }

/* 数据可视化大屏 */
.viz-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 12px; }
.viz-card { padding: 14px 14px 8px; cursor: pointer; transition: transform var(--dur-pop) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love); }
.viz-card:hover { transform: translateY(-3px); box-shadow: 0 0 0 2px rgba(255, 111, 125, 0.3), 0 10px 28px -10px rgba(122, 100, 98, 0.18); }
.viz-title { font-size: 13px; font-weight: 600; color: var(--color-ink-2); margin-bottom: 2px; }
.viz-hint { margin-top: 10px; font-size: 12px; color: var(--color-ink-3); text-align: center; }

/* 趋势数据带：与「关系数据」区对齐，2 列并列、窄屏回落单列 */
.trend-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(260px, 1fr)); gap: 12px; }
.trend-cell { padding: 12px 12px 6px; }

/* feed */
.feed { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 4px; }
.feed-item { display: flex; align-items: center; gap: 12px; padding: 10px 4px; border-bottom: 1px solid var(--color-border); }
.feed-item:last-child { border-bottom: none; }
.feed-ico { color: var(--color-rose-text); display: inline-flex; }
.feed-title { font-size: 14px; color: var(--color-ink); }
.feed-time { font-size: 12px; color: var(--color-ink-3); }

/* 图表容器 */
.screen { position: relative; border-radius: var(--radius-md); padding: 8px; }

/* 每日一句 */
.quote-card { padding: 20px 22px; position: relative; overflow: hidden; }
.quote-mark {
  position: absolute; top: -6px; left: 10px; font-size: 64px; line-height: 1;
  color: var(--color-rose-text); opacity: 0.18; font-family: Georgia, serif;
}
.quote-text {
  position: relative; font-size: 16px; line-height: 1.8; color: var(--color-ink);
  margin: 6px 0 0; padding-left: 8px; padding-right: 40px; letter-spacing: 0.02em;
}
.quote-author {
  display: block; margin-top: 10px; text-align: right; font-size: 13px; color: var(--color-ink-3);
}
.quote-hint {
  display: block; margin-top: 10px; text-align: right; font-size: 12px; color: var(--color-ink-3);
}
.q-heart { vertical-align: middle; color: var(--color-rose-text); }
.quote-shuffle {
  position: absolute; top: 12px; right: 12px; z-index: 3;
  width: 30px; height: 30px; border-radius: 999px; border: 1px solid var(--color-border);
  background: var(--color-surface); color: var(--color-rose-text); cursor: pointer;
  display: inline-flex; align-items: center; justify-content: center;
  transition: all var(--dur-micro) var(--ease-love);
}
.quote-shuffle:hover { background: var(--color-rose-soft); border-color: var(--color-rose); transform: scale(1.08); }
.quote-shuffle.beat { animation: q-beat 0.6s var(--ease-love); }
@keyframes q-beat {
  0%, 100% { transform: scale(1); }
  30% { transform: scale(1.25); }
  60% { transform: scale(0.92); }
}
.reduce-motion .quote-shuffle.beat { animation: none; }

/* 无障碍：将原 div/span 点击区改为原生 button，统一重置 + 键盘焦点环 */
button.hero-edit, button.hero-set-cta, button.today-card, button.film-cell, button.stat-link, .viz-card.ind-card-shell {
  font: inherit; color: inherit; text-align: left; cursor: pointer;
}
button.hero-edit, button.hero-set-cta, button.film-cell, button.stat-link {
  background: none; border: none; padding: 0;
}
button.stat-link, button.today-card, button.film-cell { display: block; width: 100%; }
button.film-cell { border: none; }
button.today-card { border: 1px solid var(--color-border); }
button.hero-edit { border-bottom: 1px dashed var(--color-rose); }
/* 键盘可达性：焦点环仅对键盘用户可见 */
button.hero-edit:focus-visible, button.hero-set-cta:focus-visible, button.today-card:focus-visible,
button.film-cell:focus-visible, button.stat-link:focus-visible, .viz-card.ind-card-shell:focus-visible {
  outline: 2px solid var(--color-accent, var(--color-rose));
  outline-offset: 2px;
}
</style>
