<template>
  <Teleport to="body">
    <transition name="poster-fade">
      <div v-if="visible" class="mp-mask" @click.self="close">
        <div ref="dialogEl" class="mp-dialog" v-bind="dialogAttrs" @click.stop>
          <div class="mp-head">
            <span class="mp-title">纪念海报</span>
            <button class="mp-close" type="button" aria-label="关闭" @click="close">×</button>
          </div>
          <div class="mp-body">
            <div class="mp-actions">
              <n-button type="primary" :loading="exporting" v-press-bounce @click="handleDownload">下载图片</n-button>
              <n-button v-if="canShare" :disabled="exporting" v-press-bounce @click="handleShare">分享</n-button>
              <n-button :disabled="exporting" v-press-bounce @click="close">关闭</n-button>
            </div>

            <!-- 预览舞台 -->
            <div class="mp-stage">
              <div ref="posterRef" class="mp-poster">
                <!-- 漂浮小心（装饰，不影响导出主画面） -->
                <span class="mp-heart h1">♥</span>
                <span class="mp-heart h2">♥</span>
                <span class="mp-heart h3">♥</span>
                <span class="mp-heart h4">♥</span>

                <!-- 顶栏 -->
                <div class="mp-top">
                  <span class="mp-brand">OUR MILESTONE · 纪念时刻</span>
                  <span class="mp-date">{{ date }}</span>
                </div>

                <!-- 主体 -->
                <div class="mp-main">
                  <p class="mp-label">{{ label || '我们的纪念日' }}</p>
                  <div class="mp-num-row">
                    <span class="mp-num">{{ days ?? 0 }}</span>
                    <span class="mp-unit">天</span>
                  </div>
                  <div class="mp-rule">
                    <span class="mp-line" />
                    <span class="mp-heart-big">♥</span>
                    <span class="mp-line" />
                  </div>
                  <p class="mp-sign">送给 {{ name }} 与 TA · 每一刻都值得纪念</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useMessage } from 'naive-ui';
import { NButton } from 'naive-ui';
import html2canvas from 'html2canvas';
import { useDialogA11y } from '@/composables/useDialogA11y';

/**
 * MilestonePoster —— 整百天 / 整周年纪念海报（P2-14）
 * 圆整节点庆祝横幅的延伸：把「在一起 N 天」做成可保存分享的节日海报。
 * 与 QuotePoster 同款 750px 离屏克隆导出链路。
 */
const props = defineProps<{
  days?: number;
  label?: string;
  name?: string;
  date?: string;
}>();

const days = computed(() => props.days ?? 0);
const label = computed(() => props.label || '我们的纪念日');
const name = computed(() => props.name || '我');
const date = computed(() => props.date || new Date().toLocaleDateString('zh-CN'));

const message = useMessage();
const posterRef = ref<HTMLElement | null>(null);
const exporting = ref(false);
const canShare = typeof navigator !== 'undefined' && !!navigator.share;

const visible = ref(false);
const dialogEl = ref<HTMLElement>();
function open() { visible.value = true; }
function close() { visible.value = false; }
const { dialogAttrs } = useDialogA11y({
  isOpen: visible,
  close,
  dialogRef: dialogEl,
  ariaLabel: '纪念海报',
  initialFocus: '.mp-actions button',
});
defineExpose({ open, close });

/** 等系统字体（含 Noto Serif SC 按需切片）加载完再截图，避免衬线降级 */
async function fontsReady(): Promise<void> {
  try { await (document as any).fonts?.ready; } catch { /* 忽略 */ }
}

async function renderPoster(): Promise<HTMLCanvasElement | null> {
  const src = posterRef.value;
  if (!src) return null;
  await fontsReady();
  const off = document.createElement('div');
  off.style.cssText = 'position:fixed;left:-9999px;top:0;width:750px;height:auto;z-index:-1;overflow:visible;';
  document.body.appendChild(off);
  const clone = src.cloneNode(true) as HTMLElement;
  clone.style.zoom = '1';
  clone.style.position = 'relative';
  off.appendChild(clone);
  try {
    const h = Math.max(clone.scrollHeight, 1080);
    off.style.height = `${h}px`;
    const canvas = await html2canvas(clone, {
      width: 750,
      height: h,
      scale: 2,
      useCORS: true,
      allowTaint: false,
      backgroundColor: '#ff6f7d',
      logging: false,
    });
    return canvas;
  } finally {
    document.body.removeChild(off);
  }
}

async function handleDownload() {
  exporting.value = true;
  try {
    const canvas = await renderPoster();
    if (!canvas) return;
    canvas.toBlob((blob) => {
      if (!blob) return;
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `纪念${days.value}天-${date.value}.png`;
      a.click();
      setTimeout(() => URL.revokeObjectURL(url), 5000);
    }, 'image/png');
    message.success('海报已导出');
  } catch (err: any) {
    message.error(`导出失败: ${err?.message || err}`);
  } finally {
    exporting.value = false;
  }
}

async function handleShare() {
  exporting.value = true;
  try {
    const canvas = await renderPoster();
    if (!canvas || !navigator.share) return;
    const blob = await new Promise<Blob | null>((res) => canvas.toBlob(res, 'image/png'));
    if (!blob) return;
    const file = new File([blob], `纪念${days.value}天-${date.value}.png`, { type: 'image/png' });
    await navigator.share({ files: [file], title: '纪念海报', text: `${label.value} · 相恋 ${days.value} 天` });
  } catch { /* 用户取消分享 */ }
  finally { exporting.value = false; }
}
</script>

<style scoped>
.mp-mask {
  position: fixed; inset: 0; z-index: 1000;
  background: rgba(40, 32, 30, 0.55);
  display: flex; align-items: center; justify-content: center; padding: calc(16px + env(safe-area-inset-top)) 16px calc(16px + env(safe-area-inset-bottom));
}
.mp-dialog {
  width: min(430px, 94vw); max-height: 92vh;
  background: #fbf7f2; border-radius: 18px;
  box-shadow: 0 30px 80px -20px rgba(0, 0, 0, 0.5);
  display: flex; flex-direction: column; overflow: hidden;
}
.mp-head {
  display: flex; align-items: center; justify-content: space-between;
  padding: 12px 16px; border-bottom: 1px solid #efe6db; flex-shrink: 0;
}
.mp-title { font-size: 15px; font-weight: 700; color: #4a3e39; }
.mp-close {
  width: 30px; height: 30px; border-radius: 50%; border: none;
  background: #f0e7dc; color: #6b5d54; font-size: 20px; line-height: 1; cursor: pointer;
}
.mp-close:hover { background: #e7dccd; }
.mp-body {
  padding: 16px; overflow-y: auto;
  display: flex; flex-direction: column; align-items: center;
}
.poster-fade-enter-active, .poster-fade-leave-active { transition: opacity 0.22s ease; }
.poster-fade-enter-from, .poster-fade-leave-to { opacity: 0; }

.mp-actions { display: flex; gap: 10px; margin-bottom: 16px; flex-wrap: wrap; justify-content: center; }

.mp-stage {
  width: 100%; max-width: 375px; overflow: hidden;
  border-radius: 18px; box-shadow: 0 24px 60px -16px rgba(0, 0, 0, 0.35);
}
.mp-stage .mp-poster { zoom: 0.5; }

/* —— 海报本体（750px 玫瑰庆典风） —— */
.mp-poster {
  width: 750px; box-sizing: border-box; position: relative;
  padding: 64px 60px 56px;
  background:
    radial-gradient(90% 60% at 20% 0%, rgba(255, 255, 255, 0.28), transparent 55%),
    radial-gradient(70% 50% at 85% 100%, rgba(255, 136, 147, 0.55), transparent 60%),
    linear-gradient(160deg, #ff6f7d 0%, #ff5e72 58%, #ff8893 100%);
  color: #2b1416; text-align: center; user-select: none;
  overflow: hidden;
}
.mp-heart {
  position: absolute; color: rgba(255, 255, 255, 0.5); font-size: 46px; line-height: 1; pointer-events: none;
}
.h1 { top: 42px; left: 56px; transform: rotate(-14deg); }
.h2 { top: 120px; right: 60px; font-size: 34px; transform: rotate(10deg); }
.h3 { bottom: 120px; left: 64px; font-size: 34px; transform: rotate(12deg); }
.h4 { bottom: 44px; right: 70px; transform: rotate(-8deg); }
.mp-top {
  display: flex; justify-content: space-between; align-items: baseline;
  font-family: ui-monospace, "JetBrains Mono", monospace;
  font-size: 21px; letter-spacing: 0.14em; color: rgba(43, 20, 22, 0.7);
}
.mp-date { font-size: 21px; letter-spacing: 0.08em; color: rgba(43, 20, 22, 0.55); }
.mp-main { margin-top: 90px; }
.mp-label {
  font-family: "Noto Serif SC", "Songti SC", "STZhongsong", "SimSun", serif;
  font-size: 30px; font-weight: 700; letter-spacing: 0.1em; color: rgba(43, 20, 22, 0.85);
}
.mp-num-row { display: flex; align-items: baseline; justify-content: center; gap: 18px; margin-top: 30px; }
.mp-num {
  font-family: "Noto Serif SC", "Songti SC", "STZhongsong", "SimSun", serif;
  font-size: 150px; font-weight: 800; line-height: 1; color: #2b1416; letter-spacing: -0.02em;
}
.mp-unit { font-size: 42px; font-weight: 700; color: #2b1416; }
.mp-rule { display: flex; align-items: center; gap: 20px; margin: 40px 0 34px; }
.mp-line { flex: 1; height: 1px; background: linear-gradient(90deg, transparent, rgba(43, 20, 22, 0.45), transparent); }
.mp-heart-big { font-size: 34px; color: #2b1416; line-height: 1; }
.mp-sign {
  font-family: ui-monospace, "JetBrains Mono", monospace;
  font-size: 22px; letter-spacing: 0.06em; color: rgba(43, 20, 22, 0.75);
}
</style>
