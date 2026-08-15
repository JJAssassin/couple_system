<template><div ref="el" :style="{ width: '100%', height }"></div></template>
<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue';
// 按需引入：只打包实际用到的图表与组件，砍掉全量 echarts 的 ~60% 体积（消除 >900kB 构建告警）
import { use, init, type EChartsType } from 'echarts/core';
import { PieChart, LineChart, BarChart } from 'echarts/charts';
import { GridComponent, TooltipComponent, LegendComponent } from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import type { EChartsOption } from 'echarts';
import { useSettingStore } from '@/store/settingStore';

// 运行期只用到 line/bar/pie 三类图 + 网格/提示/图例 + Canvas 渲染器（来自 Home/Account 的真实 option）
use([PieChart, LineChart, BarChart, GridComponent, TooltipComponent, LegendComponent, CanvasRenderer]);

// 选项类型放宽，避免 ECharts 严格的字面量类型在各页面反复报错；内部统一 cast。
const props = withDefaults(defineProps<{ option: any; height?: string }>(), { height: '240px' });
const el = ref<HTMLElement>();
let chart: EChartsType | null = null;

const setting = useSettingStore();

// 浪漫柔光统一调色板（与设计 Token 一致）
const PALETTE = ['#ff6f7d', '#D88593', '#E8EEF2', '#7A6462', '#F4A9B8', '#9DB4C0'];

function themeColors() {
  return setting.dark
    ? { text: '#cabdc1', ink: '#f3ecee', surface: '#2a2429', border: 'rgba(255,255,255,0.09)' }
    : { text: '#4B5563', ink: '#1F2937', surface: '#ffffff', border: 'rgba(122,100,98,0.14)' };
}

function baseOption(): any {
  const c = themeColors();
  return {
    color: PALETTE,
    textStyle: { color: c.text, fontFamily: 'PingFang SC, Microsoft YaHei, Noto Sans SC, sans-serif' },
    legend: { textStyle: { color: c.text }, icon: 'roundRect', itemWidth: 12, itemHeight: 12 },
    tooltip: {
      backgroundColor: c.surface,
      borderColor: c.border,
      borderWidth: 1,
      textStyle: { color: c.ink, fontFamily: 'PingFang SC, Microsoft YaHei, Noto Sans SC, sans-serif' },
      extraCssText: 'box-shadow:0 4px 12px rgba(31,41,55,.06),0 18px 44px -12px rgba(122,100,98,.2);border-radius:10px;padding:8px 12px;',
    },
    animationDuration: setting.reduceMotion ? 0 : 800,
    animationEasing: 'cubicOut',
  };
}

// 给坐标轴设默认：关闭网格线（除非调用方显式指定），柔和坐标文字
function axisDefaults(axis: any): any {
  if (!axis) return axis;
  const c = themeColors();
  const arr = Array.isArray(axis) ? axis : [axis];
  arr.forEach((a: any) => {
    if (a.splitLine === undefined) a.splitLine = { show: false };
    if (a.axisLine === undefined) a.axisLine = { lineStyle: { color: c.border } };
    if (a.axisLabel === undefined) a.axisLabel = { color: c.text };
  });
  return Array.isArray(axis) ? arr : arr[0];
}

// 柱状图默认圆角（除非调用方已自定义 itemStyle）
function seriesDefaults(series: any): any {
  if (!series) return series;
  const arr = Array.isArray(series) ? series : [series];
  arr.forEach((s: any) => {
    if (s.type === 'bar' && !s.itemStyle?.borderRadius) {
      s.itemStyle = { ...(s.itemStyle || {}), borderRadius: [6, 6, 0, 0] };
    }
  });
  return Array.isArray(series) ? arr : arr[0];
}

function render() {
  if (!el.value) return;
  if (!chart) chart = init(el.value);

  const userOpt = (props.option || {}) as EChartsOption;
  const merged: any = {
    ...baseOption(),
    ...userOpt,
    color: (userOpt as any).color || PALETTE,
    xAxis: axisDefaults((userOpt as any).xAxis),
    yAxis: axisDefaults((userOpt as any).yAxis),
    series: seriesDefaults((userOpt as any).series),
  };
  chart.setOption(merged, true);
  chart.resize();
}

onMounted(() => {
  render();
  window.addEventListener('resize', onResize);
});
watch(() => props.option, render, { deep: true });
// 暗色切换时重绘（重新取色）
watch(() => setting.dark, render);
onUnmounted(() => {
  window.removeEventListener('resize', onResize);
  chart?.dispose();
  chart = null;
});

function onResize() {
  chart?.resize();
}
</script>
