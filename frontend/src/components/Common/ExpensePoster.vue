<template>
  <div class="poster-preview" ref="posterRef">
    <div class="poster-card">
      <div class="poster-header">
        <div class="poster-logo">我们的小世界</div>
        <div class="poster-date">{{ dateText }}</div>
      </div>

      <div class="poster-section">
        <div class="poster-label">本月总支出</div>
        <div class="poster-amount">¥{{ totalExpense.toFixed(2) }}</div>
      </div>

      <div class="poster-section" v-if="categories.length">
        <div class="poster-label">消费分类占比</div>
        <div class="poster-cats">
          <div v-for="(c, i) in topCategories" :key="c.category" class="poster-cat-row">
            <span class="cat-dot" :style="{ background: palette[i % palette.length] }"></span>
            <span class="cat-name">{{ c.category || '未分类' }}</span>
            <span class="cat-amt">¥{{ c.amount.toFixed(2) }}</span>
            <span class="cat-pct">{{ c.percent.toFixed(1) }}%</span>
          </div>
        </div>
      </div>

      <div class="poster-footer">
        <div class="poster-quote">{{ quote }}</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';

const props = defineProps<{
  totalExpense: number;
  categories: Array<{ category: string; amount: number; percent: number }>;
  dateText?: string;
  quote?: string;
}>();

const posterRef = ref<HTMLElement>();
const palette = ['#ff6f7d', '#ff9f6e', '#ffc46b', '#7ec8a4', '#6ba7d6', '#b48ad9', '#e584b4', '#8ec6c5', '#c9b26b', '#9aa5b1'];

const dateText = computed(() => props.dateText ?? new Date().toLocaleDateString('zh-CN', { year: 'numeric', month: 'long', day: 'numeric' }));
const quote = computed(() => props.quote ?? '好好相爱，认真生活 💞');
const topCategories = computed(() => props.categories.slice(0, 6));

function generate(): Promise<Blob> {
  // 动态导入 html2canvas，避免打包体积膨胀
  return import('html2canvas').then((m) => {
    if (!posterRef.value) throw new Error('poster element not found');
    return m.default(posterRef.value, {
      backgroundColor: '#fff5f6',
      scale: 2,
      useCORS: true,
      logging: false,
    });
  }).then((canvas) => {
    return new Promise<Blob>((resolve, reject) => {
      canvas.toBlob((b: Blob | null) => b ? resolve(b) : reject(new Error('toBlob failed')), 'image/png');
    });
  });
}

function download() {
  generate().then((blob) => {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `couple-expense-${new Date().toISOString().slice(0, 10)}.png`;
    a.click();
    URL.revokeObjectURL(url);
  });
}

defineExpose({ generate, download });
</script>

<style scoped>
.poster-preview { }
.poster-card {
  width: 320px;
  background: #fff5f6;
  border-radius: 20px;
  padding: 28px 24px;
  color: #2a2429;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
  box-shadow: 0 20px 60px -12px rgba(122, 100, 98, 0.25);
}
.poster-header { display: flex; justify-content: space-between; align-items: baseline; margin-bottom: 24px; }
.poster-logo { font-size: 18px; font-weight: 700; letter-spacing: 0.02em; }
.poster-date { font-size: 12px; color: #968a8f; }
.poster-section { margin-bottom: 20px; }
.poster-label { font-size: 12px; color: #968a8f; text-transform: uppercase; letter-spacing: 0.06em; margin-bottom: 6px; }
.poster-amount { font-size: 36px; font-weight: 700; color: #2a2429; letter-spacing: -0.02em; }
.poster-cats { display: flex; flex-direction: column; gap: 10px; }
.poster-cat-row { display: flex; align-items: center; gap: 10px; font-size: 14px; }
.cat-dot { width: 10px; height: 10px; border-radius: 50%; flex-shrink: 0; }
.cat-name { flex: 1; color: #2a2429; }
.cat-amt { font-weight: 600; color: #2a2429; }
.cat-pct { width: 48px; text-align: right; color: #968a8f; font-variant-numeric: tabular-nums; }
.poster-footer { margin-top: 24px; padding-top: 16px; border-top: 1px dashed rgba(122,100,98,0.18); }
.poster-quote { font-size: 13px; color: #968a8f; text-align: center; line-height: 1.6; }
</style>
