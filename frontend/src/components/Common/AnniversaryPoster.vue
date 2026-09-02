<template>
  <teleport to="body">
    <transition name="poster-fade">
      <div v-if="visible" ref="maskEl" v-bind="dialogAttrs" class="poster-mask" @click.self="visible = false">
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
import { useDialogA11y } from '@/composables/useDialogA11y';
import type { AnniversaryDto } from '@/types';
import { usePartnerStore } from '@/store/partnerStore';

const props = defineProps<{ anniversary: AnniversaryDto | null }>();
const visible = ref(false);
const cv = ref<HTMLCanvasElement>();
const canShare = typeof navigator !== 'undefined' && !!navigator.share;
const maskEl = ref<HTMLElement>();

// 无障碍：对话框语义 + 焦点陷阱 + Esc + 焦点归还
const { dialogAttrs } = useDialogA11y({
  isOpen: visible,
  close: () => {
    visible.value = false;
  },
  dialogRef: maskEl,
  ariaLabel: '纪念日海报预览',
  initialFocus: '.p-btn.primary',
});
const partner = usePartnerStore();

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

watch(() => props.anniversary, () => { if (visible.value) nextTick(() => render()); });

function daysUntil(target: string): number {
  const now = new Date();
  const t = new Date(target);
  const diff = Math.ceil((t.getTime() - now.getTime()) / 86400000);
  return diff;
}

function render() {
  const canvas = cv.value;
  const a = props.anniversary;
  if (!canvas || !a) return;
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
  ctx.font = '800 84px "PingFang SC","Microsoft YaHei",sans-serif';
  ctx.fillText(a.name, W / 2, 420);
  ctx.fillStyle = PALETTE.rose;
  ctx.font = '700 56px "PingFang SC","Microsoft YaHei",sans-serif';
  ctx.fillText('· 纪念日 ·', W / 2, 506);

  // 倒计时大数字
  const days = daysUntil(a.nextOccurrence || a.targetDate);
  ctx.fillStyle = PALETTE.rose;
  ctx.font = '800 260px "PingFang SC","Microsoft YaHei",sans-serif';
  ctx.fillText(String(Math.max(0, days)), W / 2, 760);
  ctx.fillStyle = PALETTE.ink2;
  ctx.font = '500 48px "PingFang SC","Microsoft YaHei",sans-serif';
  ctx.fillText('天后，又是我们相爱的一天', W / 2, 850);

  // 日期信息卡
  const cardY = 940;
  const cardW = 860, cardH = 160, cardX = (W - cardW) / 2;
  ctx.save();
  ctx.fillStyle = PALETTE.surface;
  ctx.beginPath();
  ctx.roundRect(cardX, cardY, cardW, cardH, 24);
  ctx.fill();
  ctx.strokeStyle = PALETTE.border;
  ctx.lineWidth = 2;
  ctx.stroke();

  ctx.textAlign = 'left';
  ctx.fillStyle = PALETTE.ink2;
  ctx.font = '400 40px "PingFang SC","Microsoft YaHei",sans-serif';
  ctx.fillText('目标日', cardX + 40, cardY + 70);
  ctx.fillStyle = PALETTE.ink;
  ctx.font = '800 52px "PingFang SC","Microsoft YaHei",sans-serif';
  ctx.fillText(fmtDate(a.targetDate), cardX + 40, cardY + 130);

  ctx.textAlign = 'right';
  ctx.fillStyle = PALETTE.ink2;
  ctx.font = '400 40px "PingFang SC","Microsoft YaHei",sans-serif';
  ctx.fillText('类型', cardX + cardW - 40, cardY + 70);
  ctx.fillStyle = PALETTE.ink;
  ctx.font = '800 52px "PingFang SC","Microsoft YaHei",sans-serif';
  ctx.fillText(a.isYearly ? '每年' : '一次性', cardX + cardW - 40, cardY + 130);
  ctx.restore();

  // 底部水印
  ctx.textAlign = 'center';
  ctx.fillStyle = PALETTE.roseDeep;
  ctx.font = '500 38px "PingFang SC","Microsoft YaHei",sans-serif';
  ctx.fillText('7182629.xyz · 我们的专属小世界', W / 2, H - 90);
}

function fmtDate(s: string): string {
  const d = new Date(s);
  return `${d.getFullYear()}.${String(d.getMonth() + 1).padStart(2, '0')}.${String(d.getDate()).padStart(2, '0')}`;
}

async function download() {
  const canvas = cv.value;
  if (!canvas) return;
  canvas.toBlob((blob) => {
    if (!blob) return;
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${props.anniversary?.name ?? '纪念日'}.png`;
    a.click();
    setTimeout(() => URL.revokeObjectURL(url), 5000);
  }, 'image/png');
}

async function share() {
  const canvas = cv.value;
  if (!canvas || !navigator.share) return;
  const blob = await new Promise<Blob | null>((res) => canvas.toBlob(res, 'image/png'));
  if (!blob) return;
  const file = new File([blob], `${props.anniversary?.name ?? '纪念日'}.png`, { type: 'image/png' });
  try {
    await navigator.share({
      files: [file],
      title: props.anniversary?.name ?? '纪念日',
      text: `距离 ${props.anniversary?.name} 还有 ${daysUntil(props.anniversary?.nextOccurrence || props.anniversary?.targetDate || '')} 天 💞`,
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
  padding: calc(16px + env(safe-area-inset-top)) 16px calc(16px + env(safe-area-inset-bottom));
}
.poster-wrap { display: flex; flex-direction: column; align-items: center; max-height: 92vh; overflow: auto; }
.poster-canvas { width: min(92vw, 430px); border-radius: 18px; box-shadow: 0 24px 60px -16px rgba(0, 0, 0, 0.45); }
.poster-actions { display: flex; gap: 10px; margin-top: 14px; }
.p-btn {
  padding: 10px 22px; border-radius: 999px; border: 1px solid var(--color-border);
  background: var(--color-surface); color: var(--color-ink-2); font-size: 14px; cursor: pointer;
}
.p-btn.primary { background: var(--color-rose); border-color: var(--color-rose); color: var(--color-on-primary); font-weight: 600; }
.poster-tip { margin-top: 10px; font-size: 12px; color: rgba(255, 255, 255, 0.85); }
.poster-fade-enter-active, .poster-fade-leave-active { transition: opacity 0.25s var(--ease-love); }
.poster-fade-enter-from, .poster-fade-leave-to { opacity: 0; }
:global(.reduce-motion) .poster-fade-enter-active,
:global(.reduce-motion) .poster-fade-leave-active { transition: none; }
</style>
