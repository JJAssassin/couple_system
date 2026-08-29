<template>
  <teleport to="body">
    <transition name="poster-fade">
      <div v-if="visible" class="poster-mask" role="button" tabindex="0" aria-label="关闭" @click.self="onClose" @keydown.enter.prevent="onClose" @keydown.space.prevent="onClose">
        <div class="poster-wrap" @click.stop>
          <div class="poster-stage">
            <div ref="dom" class="poster-dom" :class="{ rendering: busy }">
              <!-- 纸张纹理背景 -->
              <div class="paper-bg" />

              <!-- 顶部胶带标题区 -->
              <div class="tape tape-t" />
              <div class="header">
                <div class="sub">我们的小世界 · {{ r?.year }}</div>
                <h1 class="title">我们的一年</h1>
                <div class="deco">✿ 有你在身边的每一天，都是最好的时光 ✿</div>
              </div>

              <!-- 首行：拍立得 + 恋爱天数便利贴 -->
              <div class="top-row">
                <div class="polaroid" :style="{ transform: `rotate(${rot(0)}deg)` }">
                  <div class="polaroid-img">
                    <img v-if="cover" :src="toAbs(cover)" crossorigin="anonymous" @error="onImgError" />
                    <div v-else class="ph-img">💕</div>
                  </div>
                  <div class="polaroid-cap">一起看过的日落</div>
                </div>

                <div class="days-note" :style="{ transform: `rotate(${rot(1)}deg)` }">
                  <div class="dn-ico">❤️</div>
                  <div class="dn-label">我们在一起已经</div>
                  <div class="dn-num">{{ r?.loveDays ?? 0 }}</div>
                  <div class="dn-unit">天</div>
                  <div class="dn-sub">每一天都是限定浪漫</div>
                </div>
              </div>

              <!-- 统计列表 -->
              <div class="stat-grid">
                <div v-for="(s, i) in stats" :key="s.label" class="stat-cell" :style="{ transform: `rotate(${rot(i + 2)}deg)` }">
                  <span class="stat-ico">{{ s.ico }}</span>
                  <span class="stat-txt">{{ s.label }}</span>
                  <span class="stat-val">{{ s.val }}</span>
                </div>
              </div>

              <!-- 心动瞬间照片墙 -->
              <div class="section">
                <div class="sec-title">
                  <span class="dot" />
                  那些心动瞬间
                </div>
                <div class="photo-wall">
                  <div v-for="(p, i) in wallPhotos" :key="p.id" class="wall-item" :style="{ transform: `rotate(${rot(i + 5)}deg)` }">
                    <img v-if="p.url || p.imagePath" :src="toAbs(p.url || p.imagePath)" crossorigin="anonymous" @error="onImgError" />
                    <div v-else class="ph">{{ ['💖', '💗', '💘', '💕', '💞', '💝'][i % 6] }}</div>
                  </div>
                </div>
              </div>

              <!-- 我们的足迹 -->
              <div v-if="footprints.length" class="section">
                <div class="sec-title"><span class="dot" />我们的足迹</div>
                <div class="footprints">
                  <div v-for="(f, i) in footprints.slice(0, 4)" :key="f.id" class="fp-card" :style="{ transform: `rotate(${rot(i + 11)}deg)` }">
                    <span class="fp-emoji">{{ f.emoji || '📍' }}</span>
                    <span class="fp-title">{{ f.title }}</span>
                    <span class="fp-count">{{ f.count }} 次</span>
                  </div>
                </div>
              </div>

              <!-- 纪念日 -->
              <div v-if="r?.anniversaries?.length" class="section">
                <div class="sec-title"><span class="dot" />这一年我们纪念过</div>
                <div class="ann-row">
                  <div v-for="(a, i) in r.anniversaries.slice(0, 4)" :key="i" class="ann-chip">
                    <span class="ann-ico">💝</span>
                    <span class="ann-name">{{ a.name }}</span>
                    <span class="ann-date">{{ fmtDate(a.targetDate) }}</span>
                  </div>
                </div>
              </div>

              <!-- 财务小结 -->
              <div class="section">
                <div class="sec-title"><span class="dot" />一起记账</div>
                <div class="finance">
                  <div class="fin-card">
                    <div class="fin-label">收入</div>
                    <div class="fin-val inc">+{{ fmtMoney(r?.income ?? 0) }}</div>
                  </div>
                  <div class="fin-card">
                    <div class="fin-label">支出</div>
                    <div class="fin-val exp">-{{ fmtMoney(r?.expense ?? 0) }}</div>
                  </div>
                  <div class="fin-card">
                    <div class="fin-label">结余</div>
                    <div class="fin-val bal">{{ fmtMoney((r?.income ?? 0) - (r?.expense ?? 0)) }}</div>
                  </div>
                </div>
              </div>

              <!-- 给彼此的话（固定文案，后期可扩展为真实数据） -->
              <div class="letter" :style="{ transform: `rotate(${rot(15)}deg)` }">
                <div class="letter-title">写给彼此的话</div>
                <p>谢谢你出现在我的生命里，</p>
                <p>让我的世界变得温暖又美好。</p>
                <p>遇见你是我最幸运的事，</p>
                <p>期待下一年我们创造更多美好的回忆。</p>
              </div>

              <!-- 底部 -->
              <div class="footer">
                <div class="love-you">I LOVE YOU</div>
                <div class="footer-line">7182629.xyz · 我们的专属小世界</div>
              </div>
            </div>
          </div>

          <div class="poster-actions">
            <button class="p-btn primary" :disabled="busy" @click="download">
              <span v-if="busy">生成中…</span>
              <span v-else>保存图片</span>
            </button>
            <button v-if="canShare" class="p-btn" :disabled="busy" @click="share">分享</button>
            <button class="p-btn" :disabled="busy" @click="onClose">关闭</button>
          </div>
          <p class="poster-tip">生成的是图片，可发到朋友圈 / 发给 TA 💝</p>
        </div>
      </div>
    </transition>
  </teleport>
</template>

<script setup lang="ts">
import { ref, computed, nextTick } from 'vue';
import html2canvas from 'html2canvas';
import type { YearReport } from '@/api/stats';
import type { ImageDto, FootprintDto } from '@/types';

const props = defineProps<{
  report: YearReport | null;
  photos?: ImageDto[];
  footprints?: FootprintDto[];
}>();

const visible = ref(false);
const dom = ref<HTMLElement>();
const busy = ref(false);
const brokenImages = ref(new Set<string>());
const canShare = typeof navigator !== 'undefined' && !!navigator.share;

const r = computed(() => props.report);
const cover = computed(() => props.photos?.[0]?.url || props.photos?.[0]?.imagePath);
const wallPhotos = computed(() => props.photos?.slice(1, 7) ?? []);
const footprints = computed(() => props.footprints ?? []);

function open() {
  visible.value = true;
  nextTick(() => { brokenImages.value.clear(); });
}
defineExpose({ open });

function onClose() {
  if (busy.value) return;
  visible.value = false;
}

function toAbs(path?: string): string {
  if (!path) return '';
  if (/^https?:\/\//i.test(path)) return path;
  const base = window.location.origin;
  return base + (path.startsWith('/') ? path : '/' + path);
}

function onImgError(e: Event) {
  const el = e.target as HTMLImageElement;
  if (el.src) brokenImages.value.add(el.src);
}

function rot(i: number): number {
  // 用固定伪随机角度，保证每次打开一致、轻盈不死板
  const seeds = [-2, 1.5, -1, 2, -1.5, 0.8, -0.8, 1.2, -2.2, 1.8, -1.1, 0.5, -0.5, 2.5, -1.8, 1.1];
  return seeds[i % seeds.length];
}

function fmtMoney(n: number): string {
  const v = Math.abs(Math.round(n * 100) / 100);
  return '¥' + v.toLocaleString('zh-CN');
}

function fmtDate(s: string): string {
  const d = new Date(s);
  return `${d.getFullYear()}.${String(d.getMonth() + 1).padStart(2, '0')}.${String(d.getDate()).padStart(2, '0')}`;
}

const stats = computed(() => {
  const x = r.value;
  if (!x) return [];
  return [
    { ico: '📅', label: '一起走过的日子', val: `${x.loveDays} 天` },
    { ico: '📖', label: '一起记录的日记', val: `${x.diaryCount} 篇` },
    { ico: '📷', label: '一起拍的照片', val: `${x.imageCount} 张` },
    { ico: '🌟', label: '愿望达成', val: `${x.wishDone}/${x.wishCreated}` },
    { ico: '💞', label: '默契率', val: `${x.matchRate}%` },
    { ico: '🗓️', label: '纪念日', val: `${x.anniversaryTotal} 个` },
    { ico: '👣', label: '去过的地方', val: `${x.footprintCount} 个` },
    { ico: '✅', label: '完成的待办', val: `${x.todoDone} 件` },
  ];
});

async function renderCanvas(): Promise<HTMLCanvasElement | null> {
  if (!dom.value) return null;
  busy.value = true;
  // 将海报克隆到一个离屏、无缩放、无 overflow 裁剪的容器里再导出，避免预览缩放影响输出
  const off = document.createElement('div');
  off.style.position = 'fixed';
  off.style.left = '-9999px';
  off.style.top = '0';
  off.style.width = '1080px';
  off.style.height = 'auto';
  off.style.zIndex = '-1';
  off.style.overflow = 'visible';
  document.body.appendChild(off);
  const clone = dom.value.cloneNode(true) as HTMLElement;
  clone.style.zoom = '1'; // 导出时用 1:1 原始尺寸（预览为了缩放用的是 zoom: 0.398）
  clone.style.borderRadius = '0';
  clone.style.position = 'relative';
  off.appendChild(clone);
  try {
    await waitImages(clone);
    // 海报高度自适应内容（min-height 1600，实际可能更高），按克隆 DOM 的真实高度导出，
    // 避免固定 1600 把照片墙/足迹/财务等底部板块从导出图里裁掉
    const h = Math.max(clone.scrollHeight, 1600);
    off.style.height = `${h}px`;
    const canvas = await html2canvas(clone, {
      width: 1080,
      height: h,
      scale: 2,
      useCORS: true,
      allowTaint: false,
      backgroundColor: null,
      logging: false,
    });
    return canvas;
  } finally {
    document.body.removeChild(off);
    busy.value = false;
  }
}

function waitImages(root: HTMLElement): Promise<void> {
  const imgs = Array.from(root.querySelectorAll('img'));
  return Promise.all(
    imgs.map((img) => new Promise<void>((resolve) => {
      if (img.complete) { resolve(); return; }
      img.addEventListener('load', () => resolve(), { once: true });
      img.addEventListener('error', () => resolve(), { once: true });
      setTimeout(resolve, 600);
    }))
  ).then(() => {});
}

async function download() {
  const canvas = await renderCanvas();
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
  const canvas = await renderCanvas();
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
.poster-wrap {
  display: flex; flex-direction: column; align-items: center;
  max-height: 92vh; overflow-y: auto;
}
.poster-stage {
  /* 预览视窗固定 430px 宽（对应 1080 缩放 0.398）；高度不再写死，
     由内部 zoom 后的海报真实撑开，滚动交给外层 .poster-wrap，
     避免固定 637px 把照片墙/足迹等底部板块裁掉 */
  width: 430px; overflow: hidden;
  border-radius: 18px; box-shadow: 0 24px 60px -16px rgba(0, 0, 0, 0.45);
  background: #fdf6f0;
  position: relative;
}
.poster-dom {
  /* 高度改为自适应内容（min-height 1600），并去掉 overflow:hidden，
     否则内容超过 1600px 时底部的照片墙/足迹/财务等板块会被整条裁掉 */
  width: 1080px; min-height: 1600px;
  /* 用 zoom 而非 transform: scale —— zoom 会同步缩放布局尺寸，容器高度才正确；
     transform 不改变布局占位，会让外层出现大片空白 */
  zoom: 0.398148; /* 430/1080 */
  position: relative;
  background: #fdf6f0;
  box-sizing: border-box;
  color: #5d3a3f;
  font-family: "PingFang SC", "Microsoft YaHei", sans-serif;
}
.poster-dom.rendering { opacity: 0.92; }

/* 纸张纹理 */
.paper-bg {
  position: absolute; inset: 0;
  background:
    radial-gradient(circle at 12% 15%, rgba(255, 111, 125, 0.08), transparent 35%),
    radial-gradient(circle at 88% 12%, rgba(216, 133, 147, 0.07), transparent 32%),
    radial-gradient(circle at 50% 55%, rgba(255, 220, 224, 0.28), transparent 60%),
    #fdf6f0;
}
.paper-bg::after {
  content: ''; position: absolute; inset: 0;
  background-image: repeating-linear-gradient(0deg, transparent, transparent 39px, rgba(216, 133, 147, 0.05) 40px);
  pointer-events: none;
}

/* 胶带 */
.tape {
  position: absolute; width: 160px; height: 46px;
  background: rgba(255, 220, 224, 0.78);
  border: 1px dashed rgba(255, 111, 125, 0.35);
  border-radius: 4px;
  box-shadow: 0 2px 8px rgba(93, 58, 63, 0.06);
}
.tape-t { top: 28px; left: 50%; transform: translateX(-50%) rotate(-1.5deg); }

/* 顶部标题 */
.header {
  position: relative; text-align: center; padding-top: 88px; z-index: 1;
}
.sub { font-size: 30px; color: #9a6b72; letter-spacing: 0.12em; margin-bottom: 10px; }
.title {
  font-size: 108px; font-weight: 800; margin: 0;
  color: var(--color-rose);
  font-family: Georgia, "STKaiti", "KaiTi", "PingFang SC", serif;
  letter-spacing: -0.02em;
  text-shadow: 2px 2px 0 rgba(216, 133, 147, 0.18);
}
.deco { font-size: 28px; color: #d88593; margin-top: 12px; }

/* 首行 */
.top-row {
  position: relative; z-index: 1;
  display: flex; align-items: center; justify-content: center;
  gap: 40px; margin-top: 36px; padding: 0 50px;
}
.polaroid {
  width: 340px; padding: 20px 20px 30px; background: #fff;
  box-shadow: 0 10px 28px rgba(93, 58, 63, 0.16);
  border-radius: 4px;
}
.polaroid-img {
  width: 300px; height: 300px; border-radius: 2px;
  background: #ffe8ec; display: grid; place-items: center;
  overflow: hidden;
}
.polaroid-img img { width: 100%; height: 100%; object-fit: cover; }
.ph-img { font-size: 120px; }
.polaroid-cap {
  text-align: center; font-size: 26px; color: #9a6b72; margin-top: 18px;
  font-family: "STKaiti", "KaiTi", Georgia, serif;
}

.days-note {
  width: 440px; padding: 36px 28px;
  background: linear-gradient(160deg, #fff8f9, #ffeaed);
  border: 1px solid rgba(255, 111, 125, 0.22);
  border-radius: 18px;
  box-shadow: 0 10px 28px rgba(93, 58, 63, 0.12);
  text-align: center;
}
.dn-ico { font-size: 44px; margin-bottom: 4px; }
.dn-label { font-size: 26px; color: #9a6b72; }
.dn-num { font-size: 130px; font-weight: 800; color: var(--color-rose); line-height: 1; margin: 6px 0; }
.dn-unit { font-size: 34px; color: #d88593; margin-top: -8px; }
.dn-sub { font-size: 24px; color: #9a6b72; margin-top: 10px; }

/* 统计网格 */
.stat-grid {
  position: relative; z-index: 1;
  display: grid; grid-template-columns: repeat(4, 1fr); gap: 22px;
  margin: 44px 54px 0; padding: 0;
}
.stat-cell {
  background: rgba(255, 255, 255, 0.72);
  border: 1px solid rgba(255, 111, 125, 0.18);
  border-radius: 16px;
  padding: 18px 14px;
  display: flex; flex-direction: column; align-items: center;
  gap: 6px;
  box-shadow: 0 6px 16px rgba(93, 58, 63, 0.06);
}
.stat-ico { font-size: 34px; }
.stat-txt { font-size: 22px; color: #9a6b72; }
.stat-val { font-size: 34px; font-weight: 800; color: #5d3a3f; }

/* 区块 */
.section { position: relative; z-index: 1; margin: 44px 54px 0; }
.sec-title {
  display: inline-flex; align-items: center; gap: 10px;
  font-size: 32px; font-weight: 700; color: #5d3a3f; margin-bottom: 18px;
  font-family: "STKaiti", "KaiTi", Georgia, serif;
}
.dot { width: 12px; height: 12px; border-radius: 50%; background: var(--color-rose); }

/* 照片墙 */
.photo-wall {
  display: grid; grid-template-columns: repeat(3, 1fr); gap: 18px;
}
.wall-item {
  aspect-ratio: 1; border-radius: 10px; overflow: hidden;
  background: #ffe8ec; box-shadow: 0 6px 14px rgba(93, 58, 63, 0.1);
  display: grid; place-items: center;
  border: 4px solid #fff;
}
.wall-item img { width: 100%; height: 100%; object-fit: cover; }
.wall-item .ph { font-size: 60px; }

/* 足迹 */
.footprints {
  display: flex; flex-wrap: wrap; gap: 16px;
}
.fp-card {
  display: flex; align-items: center; gap: 10px;
  background: #fff; border: 1px solid rgba(255, 111, 125, 0.16);
  border-radius: 14px; padding: 14px 22px;
  box-shadow: 0 6px 14px rgba(93, 58, 63, 0.06);
}
.fp-emoji { font-size: 34px; }
.fp-title { font-size: 28px; font-weight: 600; color: #5d3a3f; }
.fp-count { font-size: 22px; color: #9a6b72; margin-left: 6px; }

/* 纪念日 */
.ann-row { display: flex; flex-wrap: wrap; gap: 14px; }
.ann-chip {
  display: flex; align-items: center; gap: 10px;
  background: rgba(255, 234, 237, 0.7); border: 1px dashed rgba(255, 111, 125, 0.25);
  border-radius: 999px; padding: 12px 22px;
}
.ann-ico { font-size: 28px; }
.ann-name { font-size: 26px; font-weight: 600; color: #5d3a3f; }
.ann-date { font-size: 20px; color: #9a6b72; margin-left: 8px; }

/* 财务 */
.finance { display: grid; grid-template-columns: repeat(3, 1fr); gap: 20px; }
.fin-card {
  background: #fff; border: 1px solid rgba(255, 111, 125, 0.14);
  border-radius: 16px; padding: 22px 16px; text-align: center;
  box-shadow: 0 6px 14px rgba(93, 58, 63, 0.06);
}
.fin-label { font-size: 24px; color: #9a6b72; }
.fin-val { font-size: 34px; font-weight: 800; margin-top: 6px; }
.fin-val.inc { color: #16a34a; }
.fin-val.exp { color: #dc2626; }
.fin-val.bal { color: var(--color-rose); }

/* 给彼此的话 */
.letter {
  margin: 44px 54px 0; padding: 28px 32px;
  background: #fffef0; border: 1px solid rgba(216, 133, 147, 0.18);
  border-radius: 8px;
  box-shadow: 0 8px 20px rgba(93, 58, 63, 0.08);
  position: relative; z-index: 1;
}
.letter::before {
  content: ''; position: absolute; top: -12px; left: 50%; transform: translateX(-50%);
  width: 80px; height: 28px; background: rgba(255, 220, 224, 0.85);
  border: 1px dashed rgba(255, 111, 125, 0.3); border-radius: 4px;
}
.letter-title { font-size: 30px; font-weight: 700; color: #d88593; margin-bottom: 12px; }
.letter p { font-size: 24px; line-height: 1.8; color: #7a4f57; margin: 0; }

/* 底部 */
.footer { position: relative; z-index: 1; text-align: center; margin-top: 44px; padding-bottom: 50px; }
.love-you {
  font-size: 80px; font-weight: 800; color: var(--color-rose);
  font-family: Georgia, serif; letter-spacing: 0.08em;
  text-decoration: underline wavy rgba(255, 111, 125, 0.4);
}
.footer-line { font-size: 24px; color: #9a6b72; margin-top: 12px; }

/* 预览外层滚动与操作 */
.poster-actions { display: flex; gap: 10px; margin-top: 14px; }
.p-btn {
  padding: 10px 22px; border-radius: 999px; border: 1px solid var(--color-border);
  background: var(--color-surface); color: var(--color-ink-2); font-size: 14px; cursor: pointer;
}
.p-btn.primary { background: var(--color-rose); border-color: var(--color-rose); color: #fff; font-weight: 600; }
.p-btn:disabled { opacity: 0.6; cursor: not-allowed; }
.poster-tip { margin-top: 10px; font-size: 12px; color: rgba(255, 255, 255, 0.85); }
.poster-fade-enter-active, .poster-fade-leave-active { transition: opacity 0.25s var(--ease-love); }
.poster-fade-enter-from, .poster-fade-leave-to { opacity: 0; }
:global(.reduce-motion) .poster-fade-enter-active,
:global(.reduce-motion) .poster-fade-leave-active { transition: none; }
</style>
