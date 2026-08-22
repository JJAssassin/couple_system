<template>
  <div class="mood-calendar">
    <!-- 头部：年份切换 + 标题 -->
    <header class="mc-head">
      <button class="mc-nav" aria-label="上一年" @click="shiftYear(-1)">‹</button>
      <div class="mc-title-wrap">
        <h1 class="mc-title">心情日历</h1>
        <div class="mc-sub">{{ currentYear }} 年 · 每一天都值得被记住</div>
      </div>
      <button class="mc-nav" aria-label="下一年" :disabled="loading || currentYear >= maxYear" @click="shiftYear(1)">›</button>
    </header>

    <!-- 加载态 -->
    <template v-if="loading">
      <IndSkeleton variant="grid" :rows="6" :columns="7" />
    </template>

    <template v-else>
      <!-- 热力图 -->
      <section class="mc-section">
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
            @mouseenter="hover = cell.day"
            @mouseleave="hover = null"
          >
            <span v-if="cell.day" class="mc-cell-text">{{ cell.day.date.split('-')[2] }}</span>
          </div>
        </div>

        <!-- 悬停提示 -->
        <div v-if="hover" class="mc-tooltip">
          <div class="mc-tooltip-date">{{ hover.date }}</div>
          <div v-if="hover.moodScore != null" class="mc-tooltip-mood">
            心情：{{ hover.moodScore }} 分
            <span v-if="hover.moodTag" class="mc-tooltip-tag">#{{ hover.moodTag }}</span>
          </div>
          <div v-else class="mc-tooltip-empty">无记录</div>
        </div>
      </section>

      <!-- 图例 -->
      <section class="mc-section mc-legend">
        <span class="mc-legend-label">心情：</span>
        <span class="mc-legend-item" v-for="s in [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]" :key="s"
          :style="{ background: moodColor(s) }">
          {{ s }}
        </span>
        <span class="mc-legend-empty">· 空白 = 无记录</span>
      </section>

      <!-- 统计摘要 -->
      <section v-if="stats" class="mc-section mc-stats">
        <div class="mc-stat-card">
          <div class="mc-stat-num">{{ stats.recordedDays }}</div>
          <div class="mc-stat-label">记录天数</div>
        </div>
        <div class="mc-stat-card">
          <div class="mc-stat-num">{{ stats.avgMood }}</div>
          <div class="mc-stat-label">全年平均心情</div>
        </div>
        <div class="mc-stat-card">
          <div class="mc-stat-num">{{ stats.maxMood }}</div>
          <div class="mc-stat-label">最高心情</div>
        </div>
        <div class="mc-stat-card">
          <div class="mc-stat-num">{{ stats.minMood }}</div>
          <div class="mc-stat-label">最低心情</div>
        </div>
      </section>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import { fetchMoodCalendar, type MoodDay } from '@/api/stats';

const currentYear = ref(new Date().getFullYear());
const maxYear = currentYear.value;
const loading = ref(false);
const calendar = ref<{ year: number; days: MoodDay[] } | null>(null);
const hover = ref<MoodDay | null>(null);

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
.mc-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 24px; }
.mc-title { font-size: 22px; font-weight: 700; color: var(--ind-text); }
.mc-sub { font-size: 13px; color: var(--ind-text-secondary); margin-top: 4px; }
.mc-nav {
  width: 36px; height: 36px; border-radius: 50%; border: 1px solid var(--ind-border);
  background: var(--ind-card); color: var(--ind-text); font-size: 20px; cursor: pointer;
  display: flex; align-items: center; justify-content: center;
}
.mc-nav:disabled { opacity: 0.4; cursor: not-allowed; }

.mc-section { margin-top: 24px; }
.mc-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 6px;
}
.mc-weekday-label {
  text-align: center; font-size: 12px; color: var(--ind-text-secondary);
  padding-bottom: 4px;
}
.mc-cell {
  aspect-ratio: 1 / 1;
  border-radius: 4px;
  background: var(--ind-bg-secondary);
  display: flex; align-items: center; justify-content: center;
  font-size: 10px; color: var(--ind-text-secondary);
  cursor: pointer;
  transition: transform 0.15s ease;
}
.mc-cell:not(.empty):hover { transform: scale(1.15); z-index: 2; }
.mc-cell.has-mood { color: #fff; text-shadow: 0 1px 2px rgba(0,0,0,0.3); }
.mc-cell-text { pointer-events: none; }

/* Tooltip */
.mc-tooltip {
  margin-top: 12px; padding: 10px 14px; border-radius: 8px;
  background: var(--ind-card); border: 1px solid var(--ind-border);
  box-shadow: var(--ind-shadow-sm); font-size: 13px;
}
.mc-tooltip-date { color: var(--ind-text-secondary); margin-bottom: 4px; }
.mc-tooltip-mood { font-weight: 600; color: var(--ind-text); }
.mc-tooltip-tag { margin-left: 6px; color: var(--ind-primary); font-weight: 400; }
.mc-tooltip-empty { color: var(--ind-text-secondary); }

/* 图例 */
.mc-legend { display: flex; align-items: center; gap: 4px; flex-wrap: wrap; }
.mc-legend-label { font-size: 13px; color: var(--ind-text-secondary); margin-right: 4px; }
.mc-legend-item {
  width: 18px; height: 18px; border-radius: 3px; color: #fff;
  display: inline-flex; align-items: center; justify-content: center;
  font-size: 10px; font-weight: 600; text-shadow: 0 1px 1px rgba(0,0,0,0.3);
}
.mc-legend-empty { font-size: 12px; color: var(--ind-text-secondary); margin-left: 6px; }

/* 统计 */
.mc-stats { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; }
.mc-stat-card {
  padding: 16px; border-radius: 12px; text-align: center;
  background: var(--ind-card); border: 1px solid var(--ind-border);
}
.mc-stat-num { font-size: 24px; font-weight: 700; color: var(--ind-primary); }
.mc-stat-label { font-size: 12px; color: var(--ind-text-secondary); margin-top: 4px; }
</style>
