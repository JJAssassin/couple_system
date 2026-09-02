<template>
  <Teleport to="body">
    <transition name="poster-fade">
      <div
        v-if="visible"
        class="ps-mask"
        @click.self="close"
      >
        <div class="ps-dialog" @click.stop>
          <div class="ps-head">
            <span class="ps-title">我们的年度海报</span>
            <button class="ps-close" type="button" aria-label="关闭" @click="close">×</button>
          </div>
          <div class="ps-body">
            <!-- 控制栏（不被截图，仅用于交互） -->
            <div class="action-bar">
              <n-button type="primary" :loading="exporting" @click="handleSave">保存海报到相册</n-button>
              <n-button v-if="canShare" :disabled="exporting" @click="handleShare">分享</n-button>
              <n-button :disabled="exporting" @click="handleDownload">下载图片</n-button>
            </div>

            <!-- 预览舞台：响应式宽 + zoom 缩放，避免 750px 在手机上溢出 -->
            <div class="poster-stage">
              <!-- 海报 DOM 实体（导出时克隆到离屏 1:1 容器，不受预览 zoom 影响） -->
              <div ref="posterRef" class="poster-container">
                <!-- 1. 顶部 Header -->
                <div class="header-section">
                  <div class="polaroid-photo rotate-neg">
                    <div class="washi-tape tape-pink"></div>
                    <img
                      v-if="data.coverPhoto"
                      :src="toAbs(data.coverPhoto)"
                      crossorigin="anonymous"
                      class="photo-img"
                      @error="onImgError"
                    />
                    <div v-else class="photo-fallback">💕</div>
                    <div class="caption">{{ data.coverCaption || '一起看过的日落' }}</div>
                  </div>

                  <div class="title-area">
                    <h1 class="main-title hand-font">{{ data.title || '我们的一年' }}</h1>
                    <p class="sub-title">有你在身边的每一天，都是最好的时光</p>
                    <div class="days-pill">
                      <span class="text-xs">我们在一起已经</span>
                      <div class="days-count">{{ data.togetherDays ?? 0 }}<span class="text-sm">天</span></div>
                      <span v-if="data.dateRange" class="text-mono">{{ data.dateRange }}</span>
                    </div>
                  </div>
                </div>

                <!-- 2. 年度数据看板 -->
                <div v-if="data.metrics && data.metrics.length" class="memo-card">
                  <div class="card-pin">📌</div>
                  <h3 class="section-title">我们的这一年</h3>
                  <div class="metric-list">
                    <div v-for="(m, i) in data.metrics" :key="i" class="metric-item">
                      <span class="metric-label">{{ m.icon }} {{ m.label }}</span>
                      <span class="metric-value">{{ m.value }}<small v-if="m.unit"> {{ m.unit }}</small></span>
                    </div>
                  </div>
                </div>

                <!-- 3. 九宫格心动瞬间 -->
                <div v-if="data.momentPhotos && data.momentPhotos.length" class="memo-card">
                  <h3 class="section-title">那些心动瞬间</h3>
                  <div class="grid-gallery">
                    <div v-for="(img, i) in data.momentPhotos.slice(0, 9)" :key="i" class="grid-img-wrap">
                      <img
                        :src="toAbs(img)"
                        crossorigin="anonymous"
                        @error="onImgError"
                      />
                    </div>
                  </div>
                </div>

                <!-- 4. 足迹 -->
                <div v-if="data.footprints && data.footprints.length" class="memo-card">
                  <h3 class="section-title">我们的足迹</h3>
                  <div class="footprint-items">
                    <div v-for="(f, i) in data.footprints.slice(0, 6)" :key="i" class="footprint-tag">
                      <img
                        v-if="f.thumb"
                        :src="toAbs(f.thumb)"
                        crossorigin="anonymous"
                        class="thumb"
                        @error="onImgError"
                      />
                      <span v-else class="thumb-fallback">{{ f.emoji || '📍' }}</span>
                      <span>{{ f.city }}</span>
                    </div>
                  </div>
                </div>

                <!-- 5. 约定与小目标（后端暂无字段，作为可选 prop；为空则不渲染） -->
                <div v-if="hasAgreementsOrGoals" class="two-col">
                  <div v-if="data.agreements && data.agreements.length" class="memo-card mini-card">
                    <h4 class="mini-title">我们的约定</h4>
                    <ul class="checklist">
                      <li v-for="(t, i) in data.agreements" :key="i">☑️ {{ t }}</li>
                    </ul>
                  </div>
                  <div v-if="data.goals && data.goals.length" class="memo-card mini-card">
                    <h4 class="mini-title">下一年目标</h4>
                    <ul class="checklist">
                      <li v-for="(t, i) in data.goals" :key="i">🔲 {{ t }}</li>
                    </ul>
                  </div>
                </div>

                <!-- 6. 底部手写落款 -->
                <div class="footer-section">
                  <p class="footer-p">感谢这一年的相遇相知相伴，期待未来的更多美好</p>
                  <div class="signature hand-font">I Love You</div>
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
import { useDialogA11y } from '@/composables/useDialogA11y';
import { useMessage } from 'naive-ui';
import html2canvas from 'html2canvas';
import { NButton } from 'naive-ui';
import type { PosterData } from '@/types/poster';
import { assetUrl } from '@/config/server';

// 数据接口（@/types/poster）：全部可选，缺失字段对应的板块自动隐藏。
// 注意：agreements / goals / coverCaption 等当前后端 YearReport 不提供，
// 由调用方以可选 prop 传入（情侣自定义内容）；其余字段可由 YearReport + 相册/足迹列表组合得到。

const props = defineProps<{ data: PosterData }>();
const data = computed(() => props.data ?? ({} as PosterData));

const message = useMessage();
const posterRef = ref<HTMLElement | null>(null);
const exporting = ref(false);
const canShare = typeof navigator !== 'undefined' && !!navigator.share;
const brokenImages = ref<Set<string>>(new Set());

// 弹窗显隐（与现有 YearPoster 一致的调用方式：父组件通过 ref.open() 打开）
const visible = ref(false);
function open() {
  visible.value = true;
}
function close() {
  visible.value = false;
}
const dialogEl = ref<HTMLElement>();

// 无障碍：对话框语义 + 焦点陷阱 + Esc + 焦点归还
const { dialogAttrs } = useDialogA11y({
  isOpen: visible,
  close,
  dialogRef: dialogEl,
  ariaLabel: '我们的年度海报',
  initialFocus: '.action-bar button',
});

defineExpose({ open, close });

const hasAgreementsOrGoals = computed(
  () => !!((data.value.agreements && data.value.agreements.length) || (data.value.goals && data.value.goals.length))
);

function toAbs(path?: string): string {
  return assetUrl(path);
}
function onImgError(e: Event) {
  const el = e.target as HTMLImageElement;
  if (el.src) brokenImages.value.add(el.src);
}

// 与现有 YearPoster 一致的离屏克隆导出：克隆到 left:-9999px 的 1:1 容器，
// 避免预览 zoom 把导出图裁切；图片预加载后再 html2canvas（useCORS 处理同源 /uploads）。
function waitImages(root: HTMLElement): Promise<void> {
  const imgs = Array.from(root.querySelectorAll('img'));
  return Promise.all(
    imgs.map(
      (img) =>
        new Promise<void>((resolve) => {
          if (img.complete) return resolve();
          img.addEventListener('load', () => resolve(), { once: true });
          img.addEventListener('error', () => resolve(), { once: true });
          // 兜底：某些情况下 error 不触发
          setTimeout(resolve, 4000);
        })
    )
  ).then(() => {});
}

async function renderPoster(): Promise<HTMLCanvasElement | null> {
  const src = posterRef.value;
  if (!src) return null;
  const off = document.createElement('div');
  off.style.position = 'fixed';
  off.style.left = '-9999px';
  off.style.top = '0';
  off.style.width = '750px';
  off.style.height = 'auto';
  off.style.zIndex = '-1';
  off.style.overflow = 'visible';
  document.body.appendChild(off);

  const clone = src.cloneNode(true) as HTMLElement;
  clone.style.zoom = '1';
  clone.style.position = 'relative';
  off.appendChild(clone);

  try {
    await waitImages(clone);
    const h = Math.max(clone.scrollHeight, 1600);
    off.style.height = `${h}px`;
    const canvas = await html2canvas(clone, {
      width: 750,
      height: h,
      scale: 2,
      useCORS: true,
      allowTaint: false,
      backgroundColor: '#F8F5EF',
      logging: false,
    });
    return canvas;
  } finally {
    document.body.removeChild(off);
  }
}

async function handleSave() {
  // Web 端没有「直接写系统相册」的能力（那需要 Capacitor/原生插件）。
  // 正确做法：优先用系统分享面板（移动端可在面板里「存储图像」进相册），
  // 不支持分享时退化为下载。
  if (canShare) {
    await handleShare();
    return;
  }
  await handleDownload();
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
      a.download = `我们的一年-${data.value.title || ''}.png`;
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
    const file = new File([blob], `我们的一年-${data.value.title || ''}.png`, { type: 'image/png' });
    await navigator.share({
      files: [file],
      title: '我们的一年',
      text: `我们相恋 ${data.value.togetherDays ?? 0} 天`,
    });
  } catch {
    /* 用户取消分享 */
  } finally {
    exporting.value = false;
  }
}
</script>

<style scoped>
/* 弹窗遮罩 + 对话框（与 YearPoster 同款交互） */
.ps-mask {
  position: fixed;
  inset: 0;
  z-index: 1000;
  background: rgba(40, 32, 30, 0.55);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: calc(16px + env(safe-area-inset-top)) 16px calc(16px + env(safe-area-inset-bottom));
}
.ps-dialog {
  width: min(420px, 94vw);
  max-height: 92vh;
  background: #fbf7f2;
  border-radius: 18px;
  box-shadow: 0 30px 80px -20px rgba(0, 0, 0, 0.5);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}
.ps-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  border-bottom: 1px solid #efe6db;
  flex-shrink: 0;
}
.ps-title { font-size: 15px; font-weight: 700; color: #4a3e39; }
.ps-close {
  width: 30px; height: 30px; border-radius: 50%; border: none;
  background: #f0e7dc; color: #6b5d54; font-size: 20px; line-height: 1; cursor: pointer;
}
.ps-close:hover { background: #e7dccd; }
.ps-body {
  padding: 16px;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  align-items: center;
}
.poster-fade-enter-active, .poster-fade-leave-active { transition: opacity 0.22s ease; }
.poster-fade-enter-from, .poster-fade-leave-to { opacity: 0; }

/* 控制栏（不被截图） */
.action-bar {
  display: flex;
  gap: 12px;
  margin-bottom: 16px;
  flex-wrap: wrap;
  justify-content: center;
}

/* 预览舞台：响应式宽、内部 zoom 0.5 显示 750px 海报，避免手机横向溢出 */
.poster-stage {
  width: 100%;
  max-width: 375px;
  overflow: hidden;
  border-radius: 18px;
  box-shadow: 0 24px 60px -16px rgba(0, 0, 0, 0.35);
  background: #f8f5ef;
}
.poster-stage .poster-container {
  zoom: 0.5; /* 375/750 */
}

/* 海报本体（导出以 1:1 克隆体为准） */
.poster-container {
  width: 750px;
  background-color: #f8f5ef;
  background-image: radial-gradient(#e8e3da 1px, transparent 1px);
  background-size: 16px 16px;
  padding: 36px 28px;
  box-sizing: border-box;
  color: #3c3633;
  user-select: none;
}

.polaroid-photo {
  background: #fff;
  padding: 10px 10px 20px 10px;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.08);
  position: relative;
  width: 220px;
  flex-shrink: 0;
}
.rotate-neg { transform: rotate(-2.5deg); }
.polaroid-photo .photo-img,
.polaroid-photo .photo-fallback {
  width: 100%;
  height: 200px;
  object-fit: cover;
  display: block;
  background: #ffe8ec;
}
.polaroid-photo .photo-fallback {
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 72px;
}

/* 纸胶带效果 */
.washi-tape {
  position: absolute;
  top: -10px;
  left: 50%;
  transform: translateX(-50%) rotate(1.5deg);
  width: 70px;
  height: 22px;
  background-color: rgba(238, 169, 169, 0.65);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}
.tape-pink { background-color: rgba(238, 169, 169, 0.65); }

.header-section {
  display: flex;
  align-items: flex-start;
  gap: 24px;
  margin-bottom: 24px;
}
.caption {
  margin-top: 8px;
  text-align: center;
  font-size: 13px;
  color: #796e65;
}
.title-area .main-title {
  font-size: 38px;
  line-height: 1.2;
  margin: 0 0 6px 0;
  color: #2f2926;
}
.title-area .sub-title {
  font-size: 13px;
  color: #8c827a;
  margin-bottom: 16px;
}
.days-pill {
  background: #fff0ed;
  border: 1px dashed #f8b4a6;
  border-radius: 10px;
  padding: 10px 16px;
  display: inline-block;
}
.days-count {
  font-size: 32px;
  font-weight: 800;
  color: #e2583e;
  line-height: 1.1;
}

/* 手账通用便签卡片 */
.memo-card {
  background: #ffffff;
  border-radius: 12px;
  padding: 18px 20px;
  box-shadow: 0 4px 12px rgba(60, 54, 51, 0.05);
  margin-bottom: 20px;
  position: relative;
}
.card-pin {
  position: absolute;
  top: 10px;
  right: 14px;
  font-size: 16px;
}
.section-title {
  font-size: 16px;
  font-weight: 700;
  margin: 0 0 14px 0;
  color: #4a3e39;
}

/* 数据看板网格 */
.metric-list {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
}
.metric-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px dotted #eadbce;
  padding-bottom: 6px;
  font-size: 13px;
}
.metric-value {
  font-weight: bold;
  color: #e2583e;
}

/* 九宫格心动图片 */
.grid-gallery {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 8px;
}
.grid-img-wrap img {
  width: 100%;
  height: 110px;
  object-fit: cover;
  border-radius: 6px;
}

/* 足迹小图 */
.footprint-items {
  display: flex;
  gap: 12px;
  flex-wrap: wrap;
}
.footprint-tag {
  background: #fdfbf7;
  border: 1px solid #ece3d4;
  padding: 6px;
  border-radius: 6px;
  text-align: center;
  font-size: 12px;
}
.footprint-tag .thumb {
  width: 80px;
  height: 60px;
  object-fit: cover;
  border-radius: 4px;
  margin-bottom: 4px;
  display: block;
}
.thumb-fallback {
  display: block;
  width: 80px;
  height: 60px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 32px;
  margin-bottom: 4px;
}

/* 约定 / 小目标 两栏 */
.two-col {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
}
.mini-card { margin-bottom: 0; }
.mini-title {
  font-size: 14px;
  font-weight: 700;
  margin: 0 0 10px 0;
  color: #4a3e39;
}
.checklist {
  list-style: none;
  padding: 0;
  margin: 0;
  font-size: 12px;
  line-height: 1.8;
  color: #635852;
}

/* 底部手写落款 */
.footer-section {
  text-align: center;
  margin-top: 10px;
}
.footer-p {
  font-size: 13px;
  color: #8c827a;
  margin-bottom: 6px;
}
.signature {
  font-size: 28px;
  color: #e2583e;
}

/* 手写体（无 @font-face 时回退到系统手写/圆体） */
.hand-font {
  font-family: 'Segoe Script', 'Comic Sans MS', 'STKaiti', 'KaiTi', cursive;
}

/* 文本小工具类 */
.text-xs { font-size: 11px; color: #8c827a; }
.text-sm { font-size: 13px; font-weight: normal; color: #6b5d54; }
.text-mono { font-size: 11px; color: #a99e93; font-family: ui-monospace, monospace; }
</style>
