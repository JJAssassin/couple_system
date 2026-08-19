<template>
  <teleport to="body">
    <transition name="poster-fade">
      <div v-if="visible" class="poster-mask" @click.self="visible = false">
        <div class="poster-wrap">
          <canvas ref="cv" width="1080" height="1440" class="poster-canvas" />
          <div class="poster-actions">
            <button class="p-btn primary" @click="download">保存图片</button>
            <button v-if="canShare" class="p-btn" @click="share">分享</button>
            <button class="p-btn" @click="visible = false">关闭</button>
          </div>
          <p class="poster-tip">生成的是图片，可发到朋友圈 / 发给 TA 💝</p>
        </div>
      </div>
    </transition>
  </teleport>
</template>

<script setup lang="ts">
import { ref, watch, nextTick } from 'vue';
import type { YearReport } from '@/api/stats';

const props = defineProps<{ report: YearReport | null }>();
const visible = ref(false);
const cv = ref<HTMLCanvasElement>();
const canShare = typeof navigator !== 'undefined' && !!navigator.share;

const PALETTE = {
  bg0: '#fff5f6', bg1: '#ffe3e8', rose: '#ff6f7d', roseDeep: '#D88593',
  ink: '#5d3a3f', ink2: '#9a6b72', surface: 'rgba(255,255,255,0.78)',
  border: 'rgba(255,111,125,0.25)',
};

function open() {
  visible.value = true;
  nextTick(() => render());
}
defineExpose({ open });

watch(() => props.report, () => { if (visible.value) nextTick(() => render()); });

function fmtMoney(n: number): string {
  const v = Math.abs(Math.round(n * 100) / 100);
  return '¥' + v.toLocaleString('zh-CN');
}

function render() {
  const canvas = cv.value;
  const r = props.report;
  if (!canvas || !r) return;
  const ctx = canvas.getContext('2d')!;
  const W = canvas.width, H = canvas.height;
  ctx.clearRect(0, 0, W, H);

  // 背景渐变（柔和玫瑰）
  const bg = ctx.createLinearGradient(0, 0, W, H);
  bg.addColorStop(0, PALETTE.bg0);
  bg.addColorStop(1, PALETTE.bg1);
  ctx.fillStyle = bg;
  ctx.fillRect(0, 0, W, H);

  // 顶部装饰：飘散爱心
  ctx.save();
  ctx.globalAlpha = 0.16;
  ctx.font = '90px serif';
  const hearts = ['💗', '💞', '💕', '💖', '💘'];
  hearts.forEach((h, i) => {
    const x = 90 + i * 220, y = 120 + (i % 2) * 70;
    ctx.fillText(h, x, y);
  });
  ctx.restore();

  // 顶部小字 + 标题
  ctx.textAlign = 'center';
  ctx.fillStyle = PALETTE.ink2;
  ctx.font = '500 40px "PingFang SC","Microsoft YaHei",sans-serif';
  ctx.fillText('我们的小世界', W / 2, 300);
  ctx.fillStyle = PALETTE.ink;
  ctx.font = '800 96px "PingFang SC","Microsoft YaHei",sans-serif';
  ctx.fillText('我们的一年', W / 2, 420);
  ctx.fillStyle = PALETTE.rose;
  ctx.font = '700 56px "PingFang SC","Microsoft YaHei",sans-serif';
  ctx.fillText('· ' + r.year + ' ·', W / 2, 506);

  // 恋爱天数大数字
  ctx.fillStyle = PALETTE.rose;
  ctx.font = '800 300px "PingFang SC","Microsoft YaHei",sans-serif';
  ctx.fillText(String(r.loveDays), W / 2, 780);
  ctx.fillStyle = PALETTE.ink2;
  ctx.font = '500 48px "PingFang SC","Microsoft YaHei",sans-serif';
  ctx.fillText('天，我们继续爱着彼此', W / 2, 870);

  // 数据卡（2 列 x 3 行）
  const rows: [string, string][] = [
    ['📷 照片', String(r.imageCount)],
    ['📖 日记', String(r.diaryCount)],
    ['💫 愿望达成', r.wishDone + '/' + r.wishCreated],
    ['💞 默契率', r.matchRate + '%'],
    ['🗓️ 纪念日', String(r.anniversaries.length)],
    ['👣 足迹', String(r.footprintCount)],
  ];
  const cardW = 430, cardH = 128, gap = 40;
  const startY = 960;
  ctx.textAlign = 'left';
  rows.forEach(([label, val], i) => {
    const col = i % 2, row = Math.floor(i / 2);
    const x = (W - cardW * 2 - gap) / 2 + col * (cardW + gap);
    const y = startY + row * (cardH + gap);
    ctx.save();
    ctx.fillStyle = PALETTE.surface;
    ctx.beginPath();
    ctx.roundRect(x, y, cardW, cardH, 24);
    ctx.fill();
    ctx.strokeStyle = PALETTE.border;
    ctx.lineWidth = 2;
    ctx.stroke();
    ctx.fillStyle = PALETTE.ink2;
    ctx.font = '400 40px "PingFang SC","Microsoft YaHei",sans-serif';
    ctx.fillText(label, x + 36, y + 62);
    ctx.fillStyle = PALETTE.ink;
    ctx.font = '800 56px "PingFang SC","Microsoft YaHei",sans-serif';
    ctx.fillText(val, x + 36, y + 108);
    ctx.restore();
  });

  // 底部：结余（如有）+ 域名水印
  const finLine = `一起记账：收入 ${fmtMoney(r.income)} · 支出 ${fmtMoney(r.expense)}`;
  ctx.textAlign = 'center';
  ctx.fillStyle = PALETTE.ink2;
  ctx.font = '400 40px "PingFang SC","Microsoft YaHei",sans-serif';
  ctx.fillText(finLine, W / 2, startY + 3 * (cardH + gap) + 20);
  ctx.fillStyle = PALETTE.roseDeep;
  ctx.font = '500 38px "PingFang SC","Microsoft YaHei",sans-serif';
  ctx.fillText('7182629.xyz · 我们的专属小世界', W / 2, H - 90);
}

async function download() {
  const canvas = cv.value;
  if (!canvas) return;
  canvas.toBlob((blob) => {
    if (!blob) return;
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `我们的一年-${props.report?.year ?? ''}.png`;
    a.click();
    setTimeout(() => URL.revokeObjectURL(url), 5000);
  }, 'image/png');
}

async function share() {
  const canvas = cv.value;
  if (!canvas || !navigator.share) return;
  const blob = await new Promise<Blob | null>((res) => canvas.toBlob(res, 'image/png'));
  if (!blob) return;
  const file = new File([blob], `我们的一年-${props.report?.year ?? ''}.png`, { type: 'image/png' });
  try {
    await navigator.share({
      files: [file],
      title: '我们的一年',
      text: `${props.report?.year} · 我们相恋 ${props.report?.loveDays} 天 💞`,
    });
  } catch {
    /* 用户取消分享 */
  }
}
</script>

<style scoped>
.poster-mask {
  position: fixed; inset: 0; z-index: 1400;
  background: rgba(60, 30, 35, 0.55);
  display: flex; align-items: center; justify-content: center;
  padding: 16px;
}
.poster-wrap { display: flex; flex-direction: column; align-items: center; max-height: 92vh; overflow: auto; }
.poster-canvas { width: min(92vw, 430px); border-radius: 18px; box-shadow: 0 24px 60px -16px rgba(0, 0, 0, 0.45); }
.poster-actions { display: flex; gap: 10px; margin-top: 14px; }
.p-btn {
  padding: 10px 22px; border-radius: 999px; border: 1px solid var(--color-border);
  background: var(--color-surface); color: var(--color-ink-2); font-size: 14px; cursor: pointer;
}
.p-btn.primary { background: var(--color-rose); border-color: var(--color-rose); color: #fff; font-weight: 600; }
.poster-tip { margin-top: 10px; font-size: 12px; color: rgba(255, 255, 255, 0.85); }
.poster-fade-enter-active, .poster-fade-leave-active { transition: opacity 0.25s var(--ease-love); }
.poster-fade-enter-from, .poster-fade-leave-to { opacity: 0; }
:global(.reduce-motion) .poster-fade-enter-active,
:global(.reduce-motion) .poster-fade-leave-active { transition: none; }
</style>
