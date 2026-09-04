<template>
  <Teleport to="body">
    <transition name="poster-fade">
      <div v-if="visible" class="qp-mask" @click.self="close">
        <div ref="dialogEl" class="qp-dialog" v-bind="dialogAttrs" @click.stop>
          <div class="qp-head">
            <span class="qp-title">金句海报</span>
            <button class="qp-close" type="button" aria-label="关闭" @click="close">×</button>
          </div>
          <div class="qp-body">
            <div class="qp-actions">
              <n-button type="primary" :loading="exporting" v-press-bounce @click="handleDownload">下载图片</n-button>
              <n-button v-if="canShare" :disabled="exporting" v-press-bounce @click="handleShare">分享</n-button>
              <n-button :disabled="exporting" v-press-bounce @click="close">关闭</n-button>
            </div>

            <!-- 预览舞台：zoom 0.5 显示 750px 海报 -->
            <div class="qp-stage">
              <div ref="posterRef" class="qp-poster">
                <!-- 顶栏：品牌 + 日期 -->
                <div class="qp-top">
                  <span class="qp-brand">LOVE LETTER · 每日一句</span>
                  <span class="qp-date">{{ date }}</span>
                </div>

                <div class="qp-rule">
                  <span class="qp-heart">♥</span>
                  <span class="qp-line" />
                </div>

                <!-- 引号 + 正文 -->
                <div class="qp-quote-mark">“</div>
                <p class="qp-quote">{{ quote }}</p>
                <p v-if="author" class="qp-author">—— {{ author }}</p>

                <div class="qp-rule qp-rule-b">
                  <span class="qp-line" />
                  <span class="qp-heart">♥</span>
                </div>

                <!-- 落款 -->
                <p class="qp-sign">
                  {{ name }}<template v-if="partner"> 与 {{ partner }}</template>
                  <template v-if="days != null"> · 相恋第 {{ days }} 天</template>
                </p>
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
 * QuotePoster —— 每日一句金句海报（P1-11）
 * 与 CoupleSummaryPoster 同款交互/导出链路：Teleport 弹窗 + ref.open() + 离屏克隆 750px 导出。
 * 情感衬线（Noto Serif SC）在这里是绝对主角：一句情话、一条日期、两个名字。
 */
const props = defineProps<{
  quote?: string;
  author?: string;
  name?: string;
  partner?: string;
  days?: number;
  date?: string;
}>();

const quote = computed(() => props.quote || '');
const author = computed(() => props.author || '');
const name = computed(() => props.name || '我');
const partner = computed(() => props.partner || '');
const days = computed(() => props.days);
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
  ariaLabel: '每日一句金句海报',
  initialFocus: '.qp-actions button',
});
defineExpose({ open, close });

/** 等系统字体（含 Noto Serif SC 按需切片）加载完再截图，避免衬线降级成宋体 */
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
    const h = Math.max(clone.scrollHeight, 900);
    off.style.height = `${h}px`;
    const canvas = await html2canvas(clone, {
      width: 750,
      height: h,
      scale: 2,
      useCORS: true,
      allowTaint: false,
      backgroundColor: '#f9f6f4',
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
      a.download = `每日一句-${date.value}.png`;
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
    const file = new File([blob], `每日一句-${date.value}.png`, { type: 'image/png' });
    await navigator.share({ files: [file], title: '每日一句', text: quote.value });
  } catch { /* 用户取消分享 */ }
  finally { exporting.value = false; }
}
</script>

<style scoped>
.qp-mask {
  position: fixed; inset: 0; z-index: 1000;
  background: rgba(40, 32, 30, 0.55);
  display: flex; align-items: center; justify-content: center; padding: calc(16px + env(safe-area-inset-top)) 16px calc(16px + env(safe-area-inset-bottom));
}
.qp-dialog {
  width: min(430px, 94vw); max-height: 92vh;
  background: #fbf7f2; border-radius: 18px;
  box-shadow: 0 30px 80px -20px rgba(0, 0, 0, 0.5);
  display: flex; flex-direction: column; overflow: hidden;
}
.qp-head {
  display: flex; align-items: center; justify-content: space-between;
  padding: 12px 16px; border-bottom: 1px solid #efe6db; flex-shrink: 0;
}
.qp-title { font-size: 15px; font-weight: 700; color: #4a3e39; }
.qp-close {
  width: 30px; height: 30px; border-radius: 50%; border: none;
  background: #f0e7dc; color: #6b5d54; font-size: 20px; line-height: 1; cursor: pointer;
}
.qp-close:hover { background: #e7dccd; }
.qp-body {
  padding: 16px; overflow-y: auto;
  display: flex; flex-direction: column; align-items: center;
}
.poster-fade-enter-active, .poster-fade-leave-active { transition: opacity 0.22s ease; }
.poster-fade-enter-from, .poster-fade-leave-to { opacity: 0; }

.qp-actions { display: flex; gap: 10px; margin-bottom: 16px; flex-wrap: wrap; justify-content: center; }

.qp-stage {
  width: 100%; max-width: 375px; overflow: hidden;
  border-radius: 18px; box-shadow: 0 24px 60px -16px rgba(0, 0, 0, 0.35);
  background: #f9f6f4;
}
.qp-stage .qp-poster { zoom: 0.5; }

/* —— 海报本体（750px，导出 1:1） —— */
.qp-poster {
  width: 750px; box-sizing: border-box;
  padding: 64px 64px 56px;
  background-color: #f9f6f4;
  background-image:
    radial-gradient(at 14% 0%, rgba(255, 111, 125, 0.14) 0px, transparent 46%),
    radial-gradient(at 88% 100%, rgba(216, 133, 147, 0.16) 0px, transparent 48%),
    radial-gradient(rgba(122, 100, 98, 0.08) 1px, transparent 1px);
  background-size: auto, auto, 18px 18px;
  color: #2f2926;
  text-align: center;
  user-select: none;
}
.qp-top {
  display: flex; justify-content: space-between; align-items: baseline;
  font-family: ui-monospace, "JetBrains Mono", monospace;
  font-size: 21px; letter-spacing: 0.14em; color: #7a6462;
}
.qp-date { font-size: 21px; letter-spacing: 0.08em; color: #a89ba0; }
.qp-rule { display: flex; align-items: center; gap: 16px; margin: 30px 0 10px; }
.qp-rule-b { margin: 6px 0 24px; }
.qp-line { flex: 1; height: 1px; background: linear-gradient(90deg, transparent, rgba(216, 133, 147, 0.55), transparent); }
.qp-heart { font-size: 22px; color: #ff6f7d; line-height: 1; }
.qp-quote-mark {
  font-family: "Noto Serif SC", "Songti SC", "STZhongsong", "SimSun", serif;
  font-size: 150px; line-height: 0.6; color: rgba(255, 111, 125, 0.5); margin-top: 40px;
}
.qp-quote {
  font-family: "Noto Serif SC", "Songti SC", "STZhongsong", "SimSun", serif;
  font-size: 42px; font-weight: 500; line-height: 1.9; letter-spacing: 0.03em;
  color: #2f2926; margin: 44px auto 0; max-width: 600px;
}
.qp-author {
  font-family: "Noto Serif SC", "Songti SC", "STZhongsong", "SimSun", serif;
  font-size: 24px; color: #8c827a; margin-top: 24px; text-align: right;
}
.qp-sign {
  font-family: ui-monospace, "JetBrains Mono", monospace;
  font-size: 22px; letter-spacing: 0.06em; color: #7a6462; margin: 0;
}
</style>
