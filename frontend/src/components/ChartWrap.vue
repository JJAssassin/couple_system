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

// 给坐标轴设默认：极简无框风——隐藏生硬坐标轴实线与刻度，仅留超轻虚线网格
function axisDefaults(axis: any): any {
  if (!axis) return axis;
  const c = themeColors();
  const gridColor = setting.dark ? 'rgba(255,255,255,0.06)' : 'rgba(0,0,0,0.04)';
  const arr = Array.isArray(axis) ? axis : [axis];
  arr.forEach((a: any) => {
    if (a.splitLine === undefined) a.splitLine = { show: true, lineStyle: { color: gridColor, type: 'dashed' } };
    if (a.axisLine === undefined) a.axisLine = { show: false };
    if (a.axisTick === undefined) a.axisTick = { show: false };
    if (a.axisLabel === undefined) a.axisLabel = { color: c.text, fontSize: 11 };
    else if (a.axisLabel && a.axisLabel.color === undefined) a.axisLabel.color = c.text;
  });
  return Array.isArray(axis) ? arr : arr[0];
}

// hex → rgba（兼容 3/6 位），用于渐变与发光阴影
function hexToRgba(hex: string, alpha: number): string {
  let h = hex.replace('#', '');
  if (h.length === 3) h = h.split('').map((c) => c + c).join('');
  const r = parseInt(h.slice(0, 2), 16);
  const g = parseInt(h.slice(2, 4), 16);
  const b = parseInt(h.slice(4, 6), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

// 柱状图默认圆角；折线图默认发光曲线 + 渐变区域填充（除非调用方已自定义）
function seriesDefaults(series: any): any {
  if (!series) return series;
  const arr = Array.isArray(series) ? series : [series];
  arr.forEach((s: any, i: number) => {
    if (s.type === 'bar' && !s.itemStyle?.borderRadius) {
      s.itemStyle = { ...(s.itemStyle || {}), borderRadius: [6, 6, 0, 0] };
    }
    if (s.type === 'line') {
      if (s.smooth === undefined) s.smooth = 0.4;
      if (s.showSymbol === undefined) s.showSymbol = false;
      const colorStr =
        (typeof s.color === 'string' && s.color.startsWith('#')) ? s.color :
        (s.itemStyle && typeof s.itemStyle.color === 'string' && s.itemStyle.color.startsWith('#')) ? s.itemStyle.color :
        PALETTE[i % PALETTE.length];
      const lineColor = colorStr.startsWith('#') ? colorStr : `#${colorStr}`;
      if (!s.lineStyle) s.lineStyle = {};
      if (s.lineStyle.width === undefined) s.lineStyle.width = 3.5;
      if (s.lineStyle.color === undefined) s.lineStyle.color = lineColor;
      if (s.lineStyle.shadowColor === undefined) s.lineStyle.shadowColor = hexToRgba(lineColor, 0.4);
      if (s.lineStyle.shadowBlur === undefined) s.lineStyle.shadowBlur = 10;
      if (s.lineStyle.shadowOffsetY === undefined) s.lineStyle.shadowOffsetY = 4;
      if (!s.areaStyle) {
        s.areaStyle = {
          color: {
            type: 'linear', x: 0, y: 0, x2: 0, y2: 1,
            colorStops: [
              { offset: 0, color: hexToRgba(lineColor, 0.28) },
              { offset: 1, color: hexToRgba(lineColor, 0) },
            ],
          },
        };
      }
    }
  });
  return Array.isArray(series) ? arr : arr[0];
}

/**
 * 递归解析 ECharts option 中的 CSS 变量字符串（如 "var(--color-ink-3)"）。
 * 画布无法解析 CSS 变量 → 暗色模式下坐标轴/图例/标签/仪表副标题会失效（回退为默认黑）。
 * 集中在此解析，使所有图表的 var() 都能跟随 html.dark 正确取色（配合 watch(setting.dark) 重绘）。
 */
function resolveCssVars(obj: any): any {
  if (obj == null || typeof obj !== 'object') {
    if (typeof obj === 'string') {
      const m = obj.match(/^var\((--[\w-]+)\)$/);
      if (m) {
        const v = computed.value.getPropertyValue(m[1]).trim();
        return v || obj;
      }
    }
    return obj;
  }
  if (Array.isArray(obj)) return obj.map(resolveCssVars);
  const out: Record<string, any> = {};
  for (const k of Object.keys(obj)) out[k] = resolveCssVars(obj[k]);
  return out;
}

// 解析用的计算样式快照（每次渲染刷新，确保暗色切换后取最新变量）
const computed = { value: getComputedStyle(document.documentElement) };

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
  computed.value = getComputedStyle(document.documentElement);
  chart.setOption(resolveCssVars(merged), true);
  chart.resize();
}

let resizeTimer: number | null = null;
let ro: ResizeObserver | null = null;

onMounted(() => {
  render();
  window.addEventListener('resize', onResize);
  // 容器尺寸变化（如侧栏折叠）window.resize 捕获不到，用 ResizeObserver 兜底
  if (el.value && 'ResizeObserver' in window) {
    ro = new ResizeObserver(() => onResize());
    ro.observe(el.value);
  }
});
watch(() => props.option, render, { deep: true });
// 暗色切换时重绘（重新取色）
watch(() => setting.dark, render);
onUnmounted(() => {
  if (resizeTimer !== null) clearTimeout(resizeTimer);
  ro?.disconnect();
  ro = null;
  window.removeEventListener('resize', onResize);
  chart?.dispose();
  chart = null;
});

// 防抖 resize：4 个图表实例同时 resize 时避免重复重算风暴
function onResize() {
  if (resizeTimer !== null) clearTimeout(resizeTimer);
  resizeTimer = window.setTimeout(() => { chart?.resize(); }, 120);
}
</script>
