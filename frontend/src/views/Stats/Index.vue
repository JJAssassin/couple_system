<template>
  <div class="stats" ref="container">
    <!-- 品牌条 -->
    <div class="brand">
      <IpIcon name="module_stats" :size="28" class="brand-icon" alt="年度数据" />
      <h1 class="ind-label">STATS · 年度数据</h1>
      <span class="brand-status"><IndLed color="green" :size="9" /> 已同步</span>
    </div>

    <!-- 年度切换 + 主题标题 -->
    <header class="yr-head">
      <button class="yr-nav uvi-jelly" aria-label="上一年" @click="shiftYear(-1)">‹</button>
      <div class="yr-title-wrap">
        <h1 class="yr-title">我们这一年</h1>
        <div class="yr-sub">{{ report?.year ?? currentYear }} · 属于我们的数字回忆</div>
      </div>
      <button class="yr-nav uvi-jelly" aria-label="下一年" :disabled="!report || report.year >= currentYear" @click="shiftYear(1)">›</button>
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
        <GlowButton style="width:auto;display:inline-block;margin-top:16px" @click="poster?.open()">✨ 生成我们的海报</GlowButton>
      </section>

      <!-- 数字卡片：内容产出 -->
      <section class="block">
        <IndSectionTitle label="我们的痕迹" :led="true" />
        <div class="cards">
          <div class="card uvi-card3d"><div class="num"><NumberRoll :value="report.diaryCount" /></div><div class="lbl">篇日记 · 平均心情 <NumberRoll :value="report.avgMood" :decimals="1" /> 分</div></div>
          <div class="card uvi-card3d"><div class="num"><NumberRoll :value="report.imageCount" /></div><div class="lbl">张照片定格瞬间</div></div>
          <div class="card uvi-card3d"><div class="num"><NumberRoll :value="report.wishDone" />/<NumberRoll :value="report.wishCreated" /></div><div class="lbl">愿望达成</div></div>
          <div class="card uvi-card3d"><div class="num"><NumberRoll :value="report.quizRounds" /></div><div class="lbl">轮默契问答 · 默契率 <NumberRoll :value="report.matchRate" />%</div></div>
          <div class="card uvi-card3d"><div class="num"><NumberRoll :value="report.boardCount" /></div><div class="lbl">条留言悄悄话</div></div>
          <div class="card uvi-card3d"><div class="num"><NumberRoll :value="report.footprintCount" /></div><div class="lbl">个小确幸足迹</div></div>
          <div class="card uvi-card3d"><div class="num"><NumberRoll :value="report.dateCompleted" />/<NumberRoll :value="report.dateCount" /></div><div class="lbl">次约会成行</div></div>
          <div class="card uvi-card3d"><div class="num"><NumberRoll :value="report.todoDone" /></div><div class="lbl">件待办完成</div></div>
          <div class="card uvi-card3d"><div class="num"><NumberRoll :value="report.conflictResolved" />/<NumberRoll :value="report.conflictCount" /></div><div class="lbl">次矛盾已和解</div></div>
        </div>
      </section>

      <!-- 年度默契仪表盘 -->
      <section class="block">
        <IndSectionTitle label="年度默契" :led="true" />
        <div class="gauge-grid">
          <div class="chart-card uvi-glass-pop gauge-card">
            <div class="chart-title">默契率</div>
            <ChartWrap :option="matchGaugeOption" height="200px" />
            <div class="gauge-sub">{{ report?.quizRounds ?? 0 }} 轮默契问答 · 越接近 100% 越懂彼此</div>
          </div>
          <div class="chart-card uvi-glass-pop gauge-card">
            <div class="chart-title">平均心情</div>
            <ChartWrap :option="moodGaugeOption" height="200px" />
            <div class="gauge-sub">满分 10 分 · 这一年我们的小情绪</div>
          </div>
        </div>
      </section>

      <!-- 记账总览 -->
      <section class="block">
        <IndSectionTitle label="一起记账" :led="true" />
        <div class="cards">
          <div class="card uvi-card3d"><div class="num inc">+{{ fmt(report.income) }}</div><div class="lbl">收入</div></div>
          <div class="card uvi-card3d"><div class="num exp">-{{ fmt(report.expense) }}</div><div class="lbl">支出</div></div>
          <div class="card uvi-card3d"><div class="num bal">{{ fmt(report.income - report.expense) }}</div><div class="lbl">结余</div></div>
        </div>
        <div class="chart-card uvi-glass-pop"><div class="chart-title">月度收支</div><ChartWrap :option="financeOption" height="260px" /></div>
        <div v-if="report.topSpend.length" class="chart-card"><div class="chart-title">支出去向 TOP</div><ChartWrap :option="spendOption" height="260px" /></div>
      </section>

      <!-- 心情与矛盾趋势 -->
      <section class="block">
        <IndSectionTitle label="情绪曲线" :led="true" />
        <div class="chart-card uvi-glass-pop"><div class="chart-title">月度平均心情（1-10）</div><ChartWrap :option="moodOption" height="240px" /></div>
        <div class="chart-card uvi-glass-pop"><div class="chart-title">月度矛盾次数</div><ChartWrap :option="conflictOption" height="240px" /></div>
      </section>

      <!-- 年度心情日历热力图 -->
      <section v-if="moodCalendar" class="block">
        <IndSectionTitle label="年度心情日历" :led="true" />
        <div class="heatmap-card uvi-glass-pop">
          <div class="heatmap-scroll">
            <div class="hm-month-row">
              <span class="hm-corner"></span>
              <div class="hm-months">
                <span v-for="m in monthCols" :key="m.col" class="hm-month" :style="{ gridColumnStart: m.col + 1 }">{{ m.label }}</span>
              </div>
            </div>
            <div class="hm-body">
              <div class="hm-weekdays">
                <span>日</span><span>一</span><span>二</span><span>三</span><span>四</span><span>五</span><span>六</span>
              </div>
              <div class="hm-grid">
                <template v-for="(col, ci) in heatmap" :key="ci">
                  <span
                    v-for="(cell, ri) in col" :key="ri"
                    class="hcell"
                    :class="{ empty: !cell.inYear || cell.score == null }"
                    :style="cell.score != null && cell.inYear ? { background: moodColor(cell.score) } : {}"
                    :title="cell.inYear ? `${cell.date} · ${cell.score != null ? '心情 ' + cell.score : '无记录'}${cell.tag ? ' ' + cell.tag : ''}` : ''"
                  ></span>
                </template>
              </div>
            </div>
          </div>
          <div class="hm-legend">
            <span class="hm-legend-txt">心情越低</span>
            <span class="hcell empty"></span>
            <span class="hcell" :style="{ background: moodColor(2) }"></span>
            <span class="hcell" :style="{ background: moodColor(5) }"></span>
            <span class="hcell" :style="{ background: moodColor(8) }"></span>
            <span class="hcell" :style="{ background: moodColor(10) }"></span>
            <span class="hm-legend-txt">越高</span>
            <span class="hm-count">· 共记录 {{ moodCalendar?.days?.length ?? 0 }} 天心情</span>
          </div>
        </div>
      </section>

      <!-- 关系里程碑时间轴 -->
      <section v-if="sortedAnn.length" class="block">
        <IndSectionTitle label="关系里程碑" :led="true" />
        <div class="milestone-tl">
          <div v-for="a in sortedAnn" :key="a.name + a.targetDate" class="ms-item">
            <span class="ms-dot">💝</span>
            <div class="ms-body">
              <div class="ms-name">{{ a.name }}</div>
              <div class="ms-date">{{ fmtDate(a.targetDate) }}</div>
            </div>
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
import type { EChartsOption } from 'echarts';
import AuroraBackdrop from '@/components/Common/AuroraBackdrop.vue';
import GradientText from '@/components/Common/GradientText.vue';
import CoupleSummaryPoster from '@/components/Common/CoupleSummaryPoster.vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndSectionTitle from '@/components/industrial/IndSectionTitle.vue';
import IndLed from '@/components/industrial/IndLed.vue';
import IpIcon from '@/components/Common/IpIcon.vue';
import GlowButton from '@/components/Common/GlowButton.vue';
import { NumberRoll } from '@/interactions';
import { fetchYearReport, fetchMoodCalendar, type YearReport, type MoodCalendar } from '@/api/stats';
import { listAlbum, listImages } from '@/api/album';
import { listFootprints } from '@/api/footprint';
import type { ImageDto, FootprintDto } from '@/types';
import type { PosterData } from '@/types/poster';
import { useStaggerEnter } from '@/composables/useAnimation';
import { useRealtime, AGGREGATE_SYNC_MODULES } from '@/composables/useRealtime';

const currentYear = new Date().getFullYear();
const report = ref<YearReport | null>(null);
const selectedYear = ref(currentYear);
const poster = ref<InstanceType<typeof CoupleSummaryPoster> | null>(null);
const posterPhotos = ref<ImageDto[]>([]);
const posterFootprints = ref<FootprintDto[]>([]);
// 年度心情日历：按天记录的心情分（1-10）用于热力图
const moodCalendar = ref<MoodCalendar | null>(null);
// 错峰入场容器（柔光 2.0 · 动效编排）
const container = ref<HTMLElement>();
useStaggerEnter(container, '.block', { stagger: 0.1, y: 16 });

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

// 关系里程碑时间轴：把当年纪念日按日期升序排列，渲染为竖向时间线
const sortedAnn = computed(() =>
  (report.value?.anniversaries ?? []).slice().sort((a, b) => a.targetDate.localeCompare(b.targetDate)),
);

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
  loadMood(selectedYear.value);
}
async function loadPosterAssets() {
  try {
    const [albumRes, fpRes] = await Promise.all([
      listAlbum({ page: 1, pageSize: 20 }),
      listFootprints(),
    ]);
    const albums = albumRes.data.data.items ?? [];
    // 收集封面照片 + 各相册前几张照片（albumRes.data = ApiResult，.data = payload）
    const covers = albums.map((a) => a.cover).filter(Boolean) as string[];
    const extra: string[] = [];
    for (const a of albums.slice(0, 3)) {
      try {
        const imgRes = await listImages(a.id);
        const imgs = imgRes.data.data ?? [];
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

async function loadMood(year: number) {
  try {
    moodCalendar.value = await fetchMoodCalendar(year);
  } catch {
    /* 静默：热力图降级为空白 */
  }
}

// 年度数据由多模块聚合而成；伴侣在当年新增日记/照片/愿望/记账/矛盾等内容时实时刷新。
// 历史年份数据已封版，刷新无副作用。reload() 直接赋值，避免实时刷新时骨架闪烁。
const { onSync } = useRealtime();
function reload() {
  fetchYearReport(selectedYear.value).then((r) => { report.value = r; }).catch(() => {});
}
AGGREGATE_SYNC_MODULES.forEach((m) => onSync(m, reload));

// ---- ECharts options（ChartWrap 提供主题/调色板） ----
const financeOption = computed<EChartsOption>(() => {
  const m = report.value?.monthlyFinance ?? [];
  return {
    tooltip: { trigger: 'axis', valueFormatter: (v: unknown) => fmt(Number(v)) },
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
const spendOption = computed<EChartsOption>(() => ({
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
const moodOption = computed<EChartsOption>(() => ({
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
const conflictOption = computed<EChartsOption>(() => ({
  tooltip: { trigger: 'axis' },
  grid: { left: 8, right: 8, top: 16, bottom: 4, containLabel: true },
  xAxis: { type: 'category', data: (report.value?.conflictTrend ?? []).map((x) => x.label) },
  yAxis: { type: 'value', minInterval: 1 },
  series: [{
    type: 'bar', data: (report.value?.conflictTrend ?? []).map((x) => x.value),
    itemStyle: { color: 'var(--color-semantic-anniv)', borderRadius: [6, 6, 0, 0] },
  }],
}));

// ---- 年度默契仪表盘（GaugeChart，ChartWrap 已注册）----
const matchGaugeOption = computed<EChartsOption>(() => ({
  series: [{
    type: 'gauge',
    startAngle: 220, endAngle: -40, min: 0, max: 100,
    radius: '94%', center: ['50%', '62%'],
    progress: { show: true, width: 12, roundCap: true, itemStyle: { color: 'var(--color-rose-vivid)' } },
    axisLine: { lineStyle: { width: 12, color: [[1, 'var(--color-surface-2)']] } },
    pointer: { show: false }, axisTick: { show: false }, splitLine: { show: false },
    axisLabel: { show: false }, anchor: { show: false }, title: { show: false },
    detail: {
      valueAnimation: true, formatter: '{value}%',
      fontSize: 32, fontWeight: 800, color: 'var(--color-rose-text)', offsetCenter: [0, '8%'],
    },
    data: [{ value: report.value?.matchRate ?? 0 }],
  }],
}));
const moodGaugeOption = computed<EChartsOption>(() => ({
  series: [{
    type: 'gauge',
    startAngle: 220, endAngle: -40, min: 0, max: 10,
    radius: '94%', center: ['50%', '62%'],
    progress: { show: true, width: 12, roundCap: true, itemStyle: { color: 'var(--color-rose)' } },
    axisLine: { lineStyle: { width: 12, color: [[1, 'var(--color-surface-2)']] } },
    pointer: { show: false }, axisTick: { show: false }, splitLine: { show: false },
    axisLabel: { show: false }, anchor: { show: false }, title: { show: false },
    detail: {
      valueAnimation: true, formatter: '{value}',
      fontSize: 32, fontWeight: 800, color: 'var(--color-rose-text)', offsetCenter: [0, '8%'],
    },
    data: [{ value: report.value?.avgMood ?? 0 }],
  }],
}));

// ---- 年度心情日历热力图（自研 CSS 网格，无 echarts 依赖）----
const pad2 = (n: number) => String(n).padStart(2, '0');
const heatmap = computed(() => {
  const year = report.value?.year ?? selectedYear.value;
  const map = new Map<string, { score?: number; tag?: string }>();
  (moodCalendar.value?.days ?? []).forEach((d) => map.set(d.date, { score: d.moodScore, tag: d.moodTag }));
  const start = new Date(year, 0, 1);
  const gridStart = new Date(year, 0, 1 - start.getDay()); // 回退到当周周日，使列对齐
  const weeks: { date: string; score?: number; tag?: string; inYear: boolean }[][] = [];
  const cur = new Date(gridStart);
  for (let w = 0; w < 53; w++) {
    const col: { date: string; score?: number; tag?: string; inYear: boolean }[] = [];
    for (let d = 0; d < 7; d++) {
      const y = cur.getFullYear();
      const inYear = y === year;
      const dateStr = `${y}-${pad2(cur.getMonth() + 1)}-${pad2(cur.getDate())}`;
      const md = inYear ? map.get(dateStr) : undefined;
      col.push({ date: dateStr, score: md?.score, tag: md?.tag, inYear });
      cur.setDate(cur.getDate() + 1);
    }
    weeks.push(col);
  }
  return weeks;
});
const monthCols = computed(() => {
  const labels: { col: number; label: string }[] = [];
  let last = -1;
  heatmap.value.forEach((col, i) => {
    const firstIn = col.find((c) => c.inYear);
    if (!firstIn) return;
    const m = new Date(firstIn.date).getMonth();
    if (m !== last) { labels.push({ col: i, label: `${m + 1}月` }); last = m; }
  });
  return labels;
});
// 心情分 1-10 → 浅玫瑰到鲜活玫瑰的 RGB 插值；空值由 .empty 类接手主题色
function moodColor(score?: number): string {
  if (score == null) return 'var(--color-surface-2)';
  const t = Math.max(0, Math.min(1, score / 10));
  const a = [255, 233, 236], b = [255, 94, 114];
  const c = a.map((v, i) => Math.round(v + (b[i] - v) * t));
  return `rgb(${c[0]}, ${c[1]}, ${c[2]})`;
}
</script>

<style scoped>
.stats { max-width: 720px; margin: 0 auto; padding: 4px 0 24px; }

/* 品牌条 */
.brand {
  display: flex; align-items: center; gap: 16px; padding: 12px 16px; margin-bottom: 12px;
  background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg);
  box-shadow: var(--shadow-card);
}
.brand-status {
  margin-left: auto; display: inline-flex; align-items: center; gap: 6px;
  font-size: 12px; font-weight: 500; color: var(--color-ink-2);
  padding: 4px 12px; border-radius: 999px;
  background: var(--color-surface-2); border: 1px solid var(--color-border);
}
.brand-icon { margin-right: 2px; flex: 0 0 auto; }
.ind-label { font-family: var(--font-mono); font-weight: 500; letter-spacing: 0.1em; font-size: 13px; color: var(--color-ink); margin: 0; }

.yr-head { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
.yr-nav {
  width: 38px; height: 38px; border-radius: 50%; border: 1px solid var(--color-border);
  background: var(--color-surface); color: var(--color-ink-2); font-size: 20px; cursor: pointer;
  transition: transform var(--dur-pop) var(--ease-love);
}
.yr-nav:hover:not(:disabled) { color: var(--color-rose-text); border-color: var(--color-rose); }
.yr-nav:disabled { opacity: 0.3; cursor: default; }
.yr-title-wrap { text-align: center; }
.yr-title { margin: 0; font-size: 22px; font-weight: 800; color: var(--color-ink); }
.yr-sub { font-size: 12px; color: var(--color-ink-3); margin-top: 2px; }

.block { margin: 24px 0; }

.hero {
  position: relative; overflow: hidden; text-align: center; padding: 32px 16px;
  background: linear-gradient(135deg, var(--color-rose-soft), var(--color-surface));
  border: 1px solid var(--color-border); border-radius: var(--radius-lg);
}
.hero-aurora { opacity: 0.5; }
.hero-num { position: relative; font-size: 52px; line-height: 1; font-variant-numeric: tabular-nums; font-feature-settings: "tnum" 1; letter-spacing: -0.03em; animation: heartbeat 2.6s var(--ease-love) infinite; }
/* 心跳脉冲：仅 scale（无位移），不创建 containing block；reduce-motion 下由全局规则收敛为瞬时 */
@keyframes heartbeat {
  0%, 100% { transform: scale(1); }
  8% { transform: scale(1.045); }
  16% { transform: scale(1); }
  24% { transform: scale(1.045); }
  32% { transform: scale(1); }
}
.hero-unit { font-size: 20px; margin-left: 6px; color: var(--color-ink-2); }
.hero-txt { position: relative; margin-top: 10px; font-size: 13px; color: var(--color-ink-2); }
/* 主行动按钮改用 GlowButton 组件（流动渐变描边 + 高光扫过，见模板） */

.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 12px; }
.card {
  background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-md);
  padding: 16px; box-shadow: var(--shadow-card);
  display: flex; flex-direction: column; gap: 4px;
  transition: transform var(--dur-pop) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love), border-color var(--dur-pop) var(--ease-love);
}
.card:hover { box-shadow: 0 4px 12px rgba(31, 41, 55, 0.06), 0 18px 44px -12px rgba(122, 100, 98, 0.22); border-color: var(--color-rose-soft); }
.num { font-size: 24px; font-weight: 800; color: var(--color-ink); }
.num.inc { color: #16a34a; }
.num.exp { color: #dc2626; }
.num.bal { color: var(--color-rose-text); }
.lbl { font-size: 12px; color: var(--color-ink-3); }

.chart-card {
  margin-top: 16px; background: var(--color-surface); border: 1px solid var(--color-border);
  border-radius: var(--radius-md); padding: 16px;
}
.chart-title { font-size: 13px; font-weight: 600; color: var(--color-ink-2); margin-bottom: 8px; }

.ann-list { display: flex; flex-direction: column; gap: 8px; }
.ann-item {
  display: flex; align-items: center; gap: 10px; background: var(--color-surface);
  border: 1px solid var(--color-border); border-radius: var(--radius-md); padding: 12px 16px;
}
.ann-name { font-size: 14px; font-weight: 600; color: var(--color-ink); }
.ann-date { margin-left: auto; font-size: 12px; color: var(--color-ink-3); }

/* 关系里程碑竖向时间轴 */
.milestone-tl { position: relative; margin-left: 6px; padding-left: 24px; }
.milestone-tl::before { content: ''; position: absolute; left: 6px; top: 6px; bottom: 6px; width: 2px; background: linear-gradient(var(--color-rose-soft), var(--color-border)); border-radius: 2px; }
.ms-item { position: relative; display: flex; align-items: center; gap: 10px; background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-md); padding: 11px 16px; margin-bottom: 10px; transition: transform var(--dur-pop) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love); }
html:not(.reduce-motion) .ms-item:hover { transform: translateY(-2px); box-shadow: var(--elev-3); }
.ms-dot { position: absolute; left: -22px; top: 50%; transform: translateY(-50%); width: 14px; height: 14px; border-radius: 50%; background: var(--color-surface); border: 2px solid var(--color-rose); display: grid; place-items: center; font-size: 7px; }
.ms-body { display: flex; flex-direction: column; gap: 1px; }
.ms-name { font-size: 14px; font-weight: 600; color: var(--color-ink); }
.ms-date { font-size: 12px; color: var(--color-ink-3); font-family: var(--font-mono); }

.skeleton { height: 96px; background: linear-gradient(90deg, var(--color-ink-soft) 25%, var(--color-surface-2) 50%, var(--color-ink-soft) 75%); background-size: 200% 100%; animation: sk 1.4s infinite; }
@keyframes sk { from { background-position: 200% 0; } to { background-position: -200% 0; } }
:global(.reduce-motion) .skeleton { animation: none; }

/* 年度默契双仪表 */
.gauge-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.gauge-card { display: flex; flex-direction: column; align-items: center; }
.gauge-sub { font-size: 11px; color: var(--color-ink-3); margin-top: -6px; text-align: center; }

/* 年度心情日历热力图（GitHub 式 53×7 网格） */
.heatmap-card { padding: 16px; }
.heatmap-scroll { overflow-x: auto; padding-bottom: 4px; }
.hm-month-row { display: flex; margin-bottom: 4px; }
.hm-corner { flex: 0 0 18px; width: 18px; margin-right: 6px; }
.hm-months { display: grid; grid-template-columns: repeat(53, 11px); gap: 3px; }
.hm-month { font-size: 10px; color: var(--color-ink-3); white-space: nowrap; }
.hm-body { display: flex; gap: 6px; }
.hm-weekdays { display: grid; grid-template-rows: repeat(7, 11px); gap: 3px; width: 18px; flex: 0 0 18px; }
.hm-weekdays span { font-size: 9px; color: var(--color-ink-3); line-height: 11px; text-align: right; padding-right: 2px; }
.hm-grid { display: grid; grid-template-rows: repeat(7, 11px); grid-auto-flow: column; grid-auto-columns: 11px; gap: 3px; }
.hcell { width: 11px; height: 11px; border-radius: 3px; background: var(--color-surface-2); transition: transform var(--dur-pop) var(--ease-love); }
html:not(.reduce-motion) .hcell:hover { transform: scale(1.35); }
.hcell.empty { background: var(--color-surface-2); }
.hm-legend { display: flex; align-items: center; gap: 4px; margin-top: 10px; font-size: 11px; color: var(--color-ink-3); }
.hm-legend .hcell { width: 11px; height: 11px; }
.hm-legend-txt { margin: 0 2px; }
.hm-count { margin-left: auto; }

@media (max-width: 767px) {
  .gauge-grid { grid-template-columns: 1fr; }
}

@media (max-width: 767px) {
  .brand { padding: 10px 16px; margin-bottom: 10px; }
  .brand .ind-label { font-size: 12px; }
  .brand-status { padding: 3px 8px; font-size: 11px; }
  .yr-title { font-size: 19px; }
  .hero { padding: 24px 16px; }
  .hero-num { font-size: 40px; }
  .hero-unit { font-size: 17px; }
  .cards { grid-template-columns: repeat(auto-fill, minmax(130px, 1fr)); gap: 10px; }
  .block { margin: 16px 0; }
}
</style>
