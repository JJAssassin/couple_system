<template>
  <div class="mood-calendar" ref="container">
    <!-- 品牌条 -->
    <div class="brand">
      <h1 class="ind-label">MOOD CALENDAR · 心情日历</h1>
      <span class="brand-status"><IndLed color="green" :size="9" /> 已同步</span>
    </div>

    <!-- 头部：年份切换 -->
    <header class="mc-head">
      <button class="mc-nav uvi-jelly" aria-label="上一年" @click="shiftYear(-1)">‹</button>
      <div class="mc-title-wrap">
        <div class="mc-title">{{ currentYear }} 年</div>
        <div class="mc-sub">每一天都值得被记住</div>
      </div>
      <button class="mc-nav uvi-jelly" aria-label="下一年" :disabled="loading || currentYear >= maxYear" @click="shiftYear(1)">›</button>
    </header>

    <!-- 加载态 -->
    <template v-if="loading">
      <IndSkeleton variant="grid" :rows="6" :columns="7" />
    </template>

    <template v-else>
      <!-- 热力图 -->
      <section class="mc-section">
        <IndSectionTitle label="心情热力图" :led="true" />
        <div class="mc-grid">
          <!-- 星期标签 -->
          <div class="mc-weekday-label" v-for="w in weekdays" :key="w">{{ w }}</div>

          <!-- 日期格子 -->
          <div
            v-for="(cell, idx) in cells"
            :key="idx"
            class="mc-cell"
            :class="{
              empty: !cell.day,
              'has-mood': cell.day?.moodScore != null,
            }"
            :style="cell.day?.moodScore != null ? { background: moodColor(cell.day.moodScore) } : {}"
            :tabindex="cell.day ? 0 : -1"
            :role="cell.day ? 'button' : undefined"
            :aria-label="cell.day ? (cell.day.moodScore != null ? cell.day.date + ' 心情 ' + cell.day.moodScore + ' 分' : cell.day.date + ' 无记录') + '，点击前往当天日记' : ''"
            @mouseenter="hover = cell.day"
            @mouseleave="hover = null"
            @click="cell.day && goDiary(cell.day.date)"
            @keydown.enter.prevent="cell.day && goDiary(cell.day.date)"
            @keydown.space.prevent="cell.day && goDiary(cell.day.date)"
          >
            <span v-if="cell.day" class="mc-cell-text">{{ cell.day.date.split('-')[2] }}</span>
          </div>
        </div>

        <!-- 悬停提示 -->
        <div v-if="hover" class="mc-tooltip">
          <div class="mc-tooltip-date">{{ hover.date }}</div>
          <div v-if="hover.moodScore != null" class="mc-tooltip-mood">
            <IpIcon :name="moodFace(hover.moodScore)" :size="20" class="mc-tooltip-face" :alt="`心情 ${hover.moodScore} 分`" />
            心情：{{ hover.moodScore }} 分
            <span v-if="hover.moodTag" class="mc-tooltip-tag">#{{ hover.moodTag }}</span>
          </div>
          <div v-else class="mc-tooltip-empty">无记录</div>
        </div>
      </section>

      <!-- 图例 -->
      <section class="mc-section mc-legend">
        <IndSectionTitle label="心情图例" :led="true" />
        <div class="mc-legend-inner">
          <span class="mc-legend-label">心情：</span>
          <span class="mc-legend-item uvi-shine" v-for="s in [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]" :key="s"
            :style="{ background: moodColor(s) }">
            {{ s }}
          </span>
          <span class="mc-legend-empty">· 空白 = 无记录</span>
        </div>
        <div class="mc-legend-faces">
          <IpIcon v-for="f in moodFaces" :key="f.name" :name="f.name" :size="22" :alt="f.label" />
          <span class="mc-legend-faces-note">糟糕 → 幸福</span>
        </div>
      </section>

      <!-- 统计摘要 -->
      <section v-if="stats" class="mc-section mc-stats-sec">
        <IndSectionTitle label="今年心情小结" :led="true" />
        <div class="mc-stats">
          <IndStatCard label="记录天数" :value="stats.recordedDays" />
          <IndStatCard label="平均心情" :value="stats.avgMood" />
          <IndStatCard label="最高心情" :value="stats.maxMood" />
          <IndStatCard label="最低心情" :value="stats.minMood" />
        </div>
      </section>
      <IndEmpty v-else-if="calendar" class="mc-section" title="今年还没记录过心情"
        desc="在日记里给每天打个分，这里就会画出你们的色彩～" />
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndSectionTitle from '@/components/industrial/IndSectionTitle.vue';
import IndLed from '@/components/industrial/IndLed.vue';
import IndStatCard from '@/components/industrial/IndStatCard.vue';
import IpIcon from '@/components/Common/IpIcon.vue';
import { useRouter } from 'vue-router';
import { fetchMoodCalendar, type MoodDay } from '@/api/stats';

const router = useRouter();

// 点击某天 → 直达当天日记（带 date 参数，Diary 页会预填日期并打开写日记，便于补记/回看）
function goDiary(date?: string) {
  if (!date) return;
  router.push({ path: '/diary', query: { date } });
}

const currentYear = ref(new Date().getFullYear());
const maxYear = currentYear.value;
const loading = ref(false);
const calendar = ref<{ year: number; days: MoodDay[] } | null>(null);
const hover = ref<MoodDay | null>(null);
const container = ref<HTMLElement>();

const weekdays = ['日', '一', '二', '三', '四', '五', '六'];

// 构建 365/366 个格子，按周分组（周日始）
const cells = computed(() => {
  if (!calendar.value) return [];
  const year = calendar.value.year;
  const isLeap = (year % 4 === 0 && year % 100 !== 0) || year % 400 === 0;
  const daysInYear = isLeap ? 366 : 365;
  const map = new Map<string, MoodDay>();
  for (const d of calendar.value.days) map.set(d.date, d);

  const cells: { day: MoodDay | null }[] = [];
  // 1 月 1 日是星期几（0=周日）
  const firstDay = new Date(year, 0, 1).getDay();
  for (let i = 0; i < firstDay; i++) cells.push({ day: null });
  for (let m = 1; m <= 12; m++) {
    const daysInMonth = new Date(year, m, 0).getDate();
    for (let d = 1; d <= daysInMonth; d++) {
      const dateStr = `${year}-${String(m).padStart(2, '0')}-${String(d).padStart(2, '0')}`;
      cells.push({ day: map.get(dateStr) || { date: dateStr } });
    }
  }
  return cells;
});

const stats = computed(() => {
  if (!calendar.value || calendar.value.days.length === 0) return null;
  const scores = calendar.value.days.filter(d => d.moodScore != null).map(d => d.moodScore as number);
  if (scores.length === 0) return { recordedDays: 0, avgMood: 0, maxMood: 0, minMood: 0 };
  return {
    recordedDays: scores.length,
    avgMood: Math.round(scores.reduce((a, b) => a + b, 0) / scores.length * 10) / 10,
    maxMood: Math.max(...scores),
    minMood: Math.min(...scores),
  };
});

function moodColor(score: number): string {
  // 1=红, 5=黄, 10=绿
  const ratio = (score - 1) / 9; // 0..1
  const r = Math.round(255 * (1 - ratio));
  const g = Math.round(200 * ratio + 55);
  const b = Math.round(80 * (1 - ratio) + 50);
  return `rgb(${r},${g},${b})`;
}

// 1–10 连续分映射为 5 档心情贴纸（mood_terrible → mood_great）
const moodFaceMap: Record<number, string> = {
  1: 'mood_terrible', 2: 'mood_terrible',
  3: 'mood_sad', 4: 'mood_sad',
  5: 'mood_neutral', 6: 'mood_neutral',
  7: 'mood_good', 8: 'mood_good',
  9: 'mood_great', 10: 'mood_great',
};
function moodFace(score: number | null | undefined): string {
  if (score == null) return 'mood_neutral';
  return moodFaceMap[score] ?? 'mood_neutral';
}

// 图例里直接展示的 5 张心情贴纸（与上方映射一一对应）
const moodFaces = [
  { name: 'mood_terrible', label: '糟糕' },
  { name: 'mood_sad', label: '难过' },
  { name: 'mood_neutral', label: '平静' },
  { name: 'mood_good', label: '不错' },
  { name: 'mood_great', label: '幸福' },
];

async function load() {
  loading.value = true;
  try {
    calendar.value = await fetchMoodCalendar(currentYear.value);
  } finally {
    loading.value = false;
  }
}

function shiftYear(delta: number) {
  const next = currentYear.value + delta;
  if (next < 2000 || next > maxYear) return;
  currentYear.value = next;
  load();
}

onMounted(load);
</script>

<style scoped>
.mood-calendar { max-width: 960px; margin: 0 auto; padding: 16px; }

/* 品牌条 */
.brand {
  display: flex; align-items: center; gap: 14px; padding: 12px 16px; margin-bottom: 16px;
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

.mc-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 8px; }
.mc-title { font-size: 22px; font-weight: 700; color: var(--color-ink); }
.mc-sub { font-size: 13px; color: var(--color-ink-3); margin-top: 4px; }
.mc-nav {
  width: 36px; height: 36px; border-radius: 50%; border: 1px solid var(--color-border);
  background: var(--color-surface); color: var(--color-ink); font-size: 20px; cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  transition: all var(--dur-micro) var(--ease-love);
}
.mc-nav:active { transform: scale(0.94); }
.mc-nav:disabled { opacity: 0.4; cursor: not-allowed; }

.mc-section { margin-top: 24px; animation: mcRise 0.5s var(--ease-love) both; }
@keyframes mcRise { from { opacity: 0; transform: translateY(14px); } to { opacity: 1; transform: none; } }
html.reduce-motion .mc-section { animation: none; }
.mc-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 6px;
}
.mc-weekday-label {
  text-align: center; font-size: 12px; color: var(--color-ink-3);
  padding-bottom: 4px;
}
.mc-cell {
  aspect-ratio: 1 / 1;
  border-radius: 4px;
  background: var(--color-surface-2);
  display: flex; align-items: center; justify-content: center;
  font-size: 10px; color: var(--color-ink-3);
  transition: transform 0.15s ease;
}
.mc-cell:not(.empty) { cursor: pointer; }
html:not(.reduce-motion) .mc-cell:not(.empty):hover { transform: scale(1.15); z-index: 2; }
.mc-cell:focus-visible { outline: 2px solid var(--color-rose); outline-offset: 1px; }
.mc-cell.has-mood { color: #fff; text-shadow: 0 1px 2px rgba(0,0,0,0.3); }
.mc-cell-text { pointer-events: none; }

/* Tooltip */
.mc-tooltip {
  margin-top: 12px; padding: 10px 14px; border-radius: 8px;
  background: var(--color-surface); border: 1px solid var(--color-border);
  box-shadow: var(--shadow-card); font-size: 13px;
}
.mc-tooltip-date { color: var(--color-ink-3); margin-bottom: 4px; }
.mc-tooltip-mood { font-weight: 600; color: var(--color-ink); display: flex; align-items: center; gap: 6px; }
.mc-tooltip-face { flex: 0 0 auto; border-radius: 5px; }
.mc-tooltip-tag { margin-left: 6px; color: var(--color-rose-text); font-weight: 400; }
.mc-tooltip-empty { color: var(--color-ink-3); }

/* 图例 */
.mc-legend-inner { display: flex; align-items: center; gap: 4px; flex-wrap: wrap; }
.mc-legend-label { font-size: 13px; color: var(--color-ink-3); margin-right: 4px; }
.mc-legend-item {
  width: 18px; height: 18px; border-radius: 3px; color: #fff;
  display: inline-flex; align-items: center; justify-content: center;
  font-size: 10px; font-weight: 600; text-shadow: 0 1px 1px rgba(0,0,0,0.3);
}
.mc-legend-empty { font-size: 12px; color: var(--color-ink-3); margin-left: 6px; }
.mc-legend-faces { display: flex; align-items: center; gap: 6px; margin-top: 10px; flex-wrap: wrap; }
.mc-legend-faces :deep(.ip-icon) { border-radius: 5px; }
.mc-legend-faces-note { font-size: 12px; color: var(--color-ink-3); margin-left: 6px; }

/* 统计 */
.mc-stats { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; }
.mc-stats :deep(.ind-stat) { transition: transform var(--dur-pop) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love); }
html:not(.reduce-motion) .mc-stats :deep(.ind-stat):hover { transform: translateY(-3px); box-shadow: 0 4px 12px rgba(31, 41, 55, 0.06), 0 18px 44px -12px rgba(122, 100, 98, 0.22); }

@media (max-width: 767px) {
  .brand { padding: 10px 14px; margin-bottom: 12px; }
  .brand .ind-label { font-size: 12px; }
  .brand-status { padding: 3px 9px; font-size: 11px; }
  .mc-title { font-size: 19px; }
  .mc-grid { gap: 3px; }
  .mc-cell { font-size: 9px; border-radius: 3px; }
  .mc-weekday-label { font-size: 10px; }
  .mc-stats { grid-template-columns: repeat(2, 1fr); gap: 10px; }
  .mc-section :deep(.ind-sec-title) .ind-label { font-size: 14px; }
}
</style>
