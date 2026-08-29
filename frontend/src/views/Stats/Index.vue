<template>
  <div class="stats">
    <!-- 年度切换 + 主题标题 -->
    <header class="yr-head">
      <button class="yr-nav" aria-label="上一年" @click="shiftYear(-1)">‹</button>
      <div class="yr-title-wrap">
        <h1 class="yr-title">我们这一年</h1>
        <div class="yr-sub">{{ report?.year ?? currentYear }} · 属于我们的数字回忆</div>
      </div>
      <button class="yr-nav" aria-label="下一年" :disabled="!report || report.year >= currentYear" @click="shiftYear(1)">›</button>
    </header>

    <!-- 骨架 / 空 -->
    <template v-if="!report">
      <IndSkeleton variant="grid" :rows="4" :columns="2" />
    </template>

    <template v-else>
      <!-- 总览大数字：恋爱天数 -->
      <section class="hero block">
        <AuroraBackdrop class="hero-aurora" />
        <div class="hero-num"><GradientText tag="span"><NumberRoll :value="report.loveDays" /></GradientText><span class="hero-unit">天</span></div>
        <div class="hero-txt">这一年，我们继续爱着彼此 · 共 {{ report.anniversaryTotal }} 个纪念日</div>
        <button class="poster-btn" @click="poster?.open()">✨ 生成我们的海报</button>
      </section>

      <!-- 数字卡片：内容产出 -->
      <section class="block">
        <h2 class="sec-title">我们的痕迹</h2>
        <div class="cards">
          <div class="card"><div class="num"><NumberRoll :value="report.diaryCount" /></div><div class="lbl">篇日记 · 平均心情 <NumberRoll :value="report.avgMood" :decimals="1" /> 分</div></div>
          <div class="card"><div class="num"><NumberRoll :value="report.imageCount" /></div><div class="lbl">张照片定格瞬间</div></div>
          <div class="card"><div class="num"><NumberRoll :value="report.wishDone" />/<NumberRoll :value="report.wishCreated" /></div><div class="lbl">愿望达成</div></div>
          <div class="card"><div class="num"><NumberRoll :value="report.quizRounds" /></div><div class="lbl">轮默契问答 · 默契率 <NumberRoll :value="report.matchRate" />%</div></div>
          <div class="card"><div class="num"><NumberRoll :value="report.boardCount" /></div><div class="lbl">条留言悄悄话</div></div>
          <div class="card"><div class="num"><NumberRoll :value="report.footprintCount" /></div><div class="lbl">个小确幸足迹</div></div>
          <div class="card"><div class="num"><NumberRoll :value="report.dateCompleted" />/<NumberRoll :value="report.dateCount" /></div><div class="lbl">次约会成行</div></div>
          <div class="card"><div class="num"><NumberRoll :value="report.todoDone" /></div><div class="lbl">件待办完成</div></div>
          <div class="card"><div class="num"><NumberRoll :value="report.conflictResolved" />/<NumberRoll :value="report.conflictCount" /></div><div class="lbl">次矛盾已和解</div></div>
        </div>
      </section>

      <!-- 记账总览 -->
      <section class="block">
        <h2 class="sec-title">一起记账</h2>
        <div class="cards">
          <div class="card"><div class="num inc">+{{ fmt(report.income) }}</div><div class="lbl">收入</div></div>
          <div class="card"><div class="num exp">-{{ fmt(report.expense) }}</div><div class="lbl">支出</div></div>
          <div class="card"><div class="num bal">{{ fmt(report.income - report.expense) }}</div><div class="lbl">结余</div></div>
        </div>
        <div class="chart-card"><div class="chart-title">月度收支</div><ChartWrap :option="financeOption" height="260px" /></div>
        <div v-if="report.topSpend.length" class="chart-card"><div class="chart-title">支出去向 TOP</div><ChartWrap :option="spendOption" height="260px" /></div>
      </section>

      <!-- 心情与矛盾趋势 -->
      <section class="block">
        <h2 class="sec-title">情绪曲线</h2>
        <div class="chart-card"><div class="chart-title">月度平均心情（1-10）</div><ChartWrap :option="moodOption" height="240px" /></div>
        <div class="chart-card"><div class="chart-title">月度矛盾次数</div><ChartWrap :option="conflictOption" height="240px" /></div>
      </section>

      <!-- 纪念日回顾 -->
      <section v-if="report.anniversaries.length" class="block">
        <h2 class="sec-title">这一年我们纪念过</h2>
        <div class="ann-list">
          <div v-for="a in report.anniversaries" :key="a.name + a.targetDate" class="ann-item">
            <span class="ann-dot">💝</span>
            <span class="ann-name">{{ a.name }}</span>
            <span class="ann-date">{{ fmtDate(a.targetDate) }}</span>
          </div>
        </div>
      </section>

      <IndEmpty v-else title="这一年还没有纪念日" desc="去「纪念日」页添加一个吧～" />
    </template>

    <!-- 年度报告分享海报 -->
    <CoupleSummaryPoster ref="poster" :data="posterData" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import ChartWrap from '@/components/ChartWrap.vue';
import AuroraBackdrop from '@/components/Common/AuroraBackdrop.vue';
import GradientText from '@/components/Common/GradientText.vue';
import CoupleSummaryPoster from '@/components/Common/CoupleSummaryPoster.vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import { NumberRoll } from '@/interactions';
import { fetchYearReport, type YearReport } from '@/api/stats';
import { listAlbum, listImages } from '@/api/album';
import { listFootprints } from '@/api/footprint';
import type { ImageDto, FootprintDto } from '@/types';
import type { PosterData } from '@/types/poster';

const currentYear = new Date().getFullYear();
const report = ref<YearReport | null>(null);
const selectedYear = ref(currentYear);
const poster = ref<InstanceType<typeof CoupleSummaryPoster> | null>(null);
const posterPhotos = ref<ImageDto[]>([]);
const posterFootprints = ref<FootprintDto[]>([]);

// 把后端 YearReport + 相册/足迹列表，映射成手账海报所需的 PosterData。
// agreements / goals / coverCaption 后端暂无字段，留空则对应板块自动隐藏。
const posterData = computed<PosterData>(() => {
  const r = report.value;
  if (!r) return {};
  const first = posterPhotos.value[0];
  const coverPhoto = first ? first.url || first.imagePath : undefined;
  const metrics = [
    { icon: '📖', label: '篇日记', value: r.diaryCount, unit: '篇' },
    { icon: '📷', label: '张照片', value: r.imageCount, unit: '张' },
    { icon: '💌', label: '愿望达成', value: `${r.wishDone}/${r.wishCreated}` },
    { icon: '🧩', label: '默契率', value: r.matchRate, unit: '%' },
    { icon: '💬', label: '悄悄话', value: r.boardCount, unit: '条' },
    { icon: '📍', label: '足迹', value: r.footprintCount, unit: '个' },
    { icon: '🎯', label: '约会成行', value: `${r.dateCompleted}/${r.dateCount}` },
    { icon: '✅', label: '待办完成', value: r.todoDone, unit: '件' },
    { icon: '🤝', label: '矛盾和解', value: `${r.conflictResolved}/${r.conflictCount}` },
    { icon: '💝', label: '纪念日', value: r.anniversaryTotal, unit: '个' },
  ];
  return {
    title: `${r.year} · 我们的年`,
    togetherDays: r.loveDays,
    dateRange: `${r.year}.01.01 – ${r.year}.12.31`,
    coverPhoto,
    coverCaption: '我们这一年最爱的瞬间',
    metrics,
    momentPhotos: posterPhotos.value
      .map((p) => p.url || p.imagePath)
      .filter(Boolean) as string[],
    footprints: posterFootprints.value
      .slice(0, 6)
      .map((f) => ({ city: f.title, emoji: f.emoji })),
  };
});

function fmt(n: number): string {
  const v = Math.abs(Math.round(n * 100) / 100);
  return '¥' + v.toLocaleString('zh-CN');
}
function fmtDate(s: string): string {
  const d = new Date(s);
  return `${d.getFullYear()}.${String(d.getMonth() + 1).padStart(2, '0')}.${String(d.getDate()).padStart(2, '0')}`;
}
function shiftYear(delta: number) {
  const y = (report.value?.year ?? selectedYear.value) + delta;
  if (y < 2000 || y > currentYear) return;
  selectedYear.value = y;
  load();
}
async function load() {
  report.value = null;
  try {
    report.value = await fetchYearReport(selectedYear.value);
  } catch {
    /* 拦截器已 toast */
  }
}
async function loadPosterAssets() {
  try {
    const [albumRes, fpRes] = await Promise.all([
      listAlbum({ page: 1, pageSize: 20 }),
      listFootprints(),
    ]);
    const albums = (albumRes.data as { data: { items: { id: number; cover?: string }[] } }).data.items ?? [];
    // 收集封面照片 + 各相册前几张照片
    const covers = albums.map((a) => a.cover).filter(Boolean) as string[];
    const extra: string[] = [];
    for (const a of albums.slice(0, 3)) {
      try {
        const { data: imgRes } = await listImages(a.id);
        const imgs = (imgRes.data as { data: ImageDto[] }).data ?? [];
        extra.push(...imgs.slice(0, 4).map((i) => i.url || i.imagePath).filter(Boolean));
      } catch { /* ignore */ }
    }
    const urls = Array.from(new Set([...extra, ...covers])).slice(0, 9);
    posterPhotos.value = urls.map((url, idx) => ({ id: idx + 1, albumId: 0, imagePath: url, url, createUserId: 0, createTime: '' } as ImageDto));
    posterFootprints.value = fpRes ?? [];
  } catch {
    /* 静默忽略：海报降级为占位数据 */
  }
}
onMounted(() => { load(); loadPosterAssets(); });

// ---- ECharts options（ChartWrap 提供主题/调色板） ----
const financeOption = computed(() => {
  const m = report.value?.monthlyFinance ?? [];
  return {
    tooltip: { trigger: 'axis' },
    legend: { data: ['收入', '支出'] },
    grid: { left: 8, right: 8, top: 34, bottom: 4, containLabel: true },
    xAxis: { type: 'category', data: m.map((x) => x.month.slice(5)) },
    yAxis: { type: 'value' },
    series: [
      { name: '收入', type: 'bar', data: m.map((x) => x.income), itemStyle: { color: 'var(--color-income)', borderRadius: [6, 6, 0, 0] } },
      { name: '支出', type: 'bar', data: m.map((x) => x.expense), itemStyle: { color: 'var(--color-expend)', borderRadius: [6, 6, 0, 0] } },
    ],
  };
});
const spendOption = computed(() => ({
  tooltip: { trigger: 'item', valueFormatter: (v: unknown) => fmt(Number(v)) },
  series: [{
    type: 'pie',
    radius: ['42%', '68%'],
    center: ['50%', '52%'],
    itemStyle: { borderRadius: 6, borderColor: 'transparent', borderWidth: 2 },
    label: { formatter: '{b}\n{d}%' },
    data: (report.value?.topSpend ?? []).map((s) => ({ name: s.category, value: s.amount })),
  }],
}));
const moodOption = computed(() => ({
  tooltip: { trigger: 'axis' },
  grid: { left: 8, right: 8, top: 16, bottom: 4, containLabel: true },
  xAxis: { type: 'category', boundaryGap: false, data: (report.value?.moodTrend ?? []).map((x) => x.label) },
  yAxis: { type: 'value', min: 0, max: 10 },
  series: [{
    type: 'line', smooth: true, symbol: 'circle', symbolSize: 7,
    data: (report.value?.moodTrend ?? []).map((x) => x.value),
    lineStyle: { width: 3, color: 'var(--color-rose)' }, itemStyle: { color: 'var(--color-rose)' },
    areaStyle: { color: { type: 'linear', x: 0, y: 0, x2: 0, y2: 1, colorStops: [{ offset: 0, color: 'rgba(255,111,125,.28)' }, { offset: 1, color: 'rgba(255,111,125,0)' }] } },
  }],
}));
const conflictOption = computed(() => ({
  tooltip: { trigger: 'axis' },
  grid: { left: 8, right: 8, top: 16, bottom: 4, containLabel: true },
  xAxis: { type: 'category', data: (report.value?.conflictTrend ?? []).map((x) => x.label) },
  yAxis: { type: 'value', minInterval: 1 },
  series: [{
    type: 'bar', data: (report.value?.conflictTrend ?? []).map((x) => x.value),
    itemStyle: { color: 'var(--color-semantic-anniv)', borderRadius: [6, 6, 0, 0] },
  }],
}));
</script>

<style scoped>
.stats { max-width: 720px; margin: 0 auto; padding: 4px 0 24px; }
.yr-head { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.yr-nav {
  width: 38px; height: 38px; border-radius: 50%; border: 1px solid var(--color-border);
  background: var(--color-surface); color: var(--color-ink-2); font-size: 20px; cursor: pointer;
  transition: transform var(--dur-pop) var(--ease-love);
}
.yr-nav:hover:not(:disabled) { transform: translateY(-2px); color: var(--color-rose); border-color: var(--color-rose); }
.yr-nav:disabled { opacity: 0.3; cursor: default; }
.yr-title-wrap { text-align: center; }
.yr-title { margin: 0; font-size: 22px; font-weight: 800; color: var(--color-ink); }
.yr-sub { font-size: 12px; color: var(--color-ink-3); margin-top: 2px; }

.block { margin: 22px 0; }
.sec-title { font-size: 16px; font-weight: 700; color: var(--color-ink); margin: 0 0 12px; }

.hero {
  position: relative; overflow: hidden; text-align: center; padding: 34px 16px;
  background: linear-gradient(135deg, var(--color-rose-soft), var(--color-surface));
  border: 1px solid var(--color-border); border-radius: var(--radius-lg);
}
.hero-aurora { opacity: 0.5; }
.hero-num { position: relative; font-size: 52px; line-height: 1; font-variant-numeric: tabular-nums; font-feature-settings: "tnum" 1; letter-spacing: -0.03em; }
.hero-unit { font-size: 20px; margin-left: 6px; color: var(--color-ink-2); }
.hero-txt { position: relative; margin-top: 10px; font-size: 13px; color: var(--color-ink-2); }
.poster-btn {
  position: relative; margin-top: 16px; padding: 10px 26px; border-radius: 999px;
  border: 1px solid var(--color-rose); background: var(--color-surface);
  color: var(--color-rose); font-size: 14px; font-weight: 600; cursor: pointer;
  transition: transform var(--dur-pop) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love);
}
.poster-btn:hover { transform: translateY(-2px); box-shadow: 0 10px 24px -10px rgba(255, 111, 125, 0.5); }

.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 12px; }
.card {
  background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-md);
  padding: 16px; box-shadow: 0 1px 2px rgba(31,41,55,.04), 0 10px 28px -10px rgba(122,100,98,.16);
  display: flex; flex-direction: column; gap: 4px;
}
.num { font-size: 24px; font-weight: 800; color: var(--color-ink); }
.num.inc { color: #16a34a; }
.num.exp { color: #dc2626; }
.num.bal { color: var(--color-rose); }
.lbl { font-size: 12px; color: var(--color-ink-3); }

.chart-card {
  margin-top: 14px; background: var(--color-surface); border: 1px solid var(--color-border);
  border-radius: var(--radius-md); padding: 14px;
}
.chart-title { font-size: 13px; font-weight: 600; color: var(--color-ink-2); margin-bottom: 8px; }

.ann-list { display: flex; flex-direction: column; gap: 8px; }
.ann-item {
  display: flex; align-items: center; gap: 10px; background: var(--color-surface);
  border: 1px solid var(--color-border); border-radius: var(--radius-md); padding: 12px 14px;
}
.ann-name { font-size: 14px; font-weight: 600; color: var(--color-ink); }
.ann-date { margin-left: auto; font-size: 12px; color: var(--color-ink-3); }

.skeleton { height: 96px; background: linear-gradient(90deg, var(--color-ink-soft) 25%, var(--color-surface-2) 50%, var(--color-ink-soft) 75%); background-size: 200% 100%; animation: sk 1.4s infinite; }
@keyframes sk { from { background-position: 200% 0; } to { background-position: -200% 0; } }
:global(.reduce-motion) .skeleton { animation: none; }
</style>
