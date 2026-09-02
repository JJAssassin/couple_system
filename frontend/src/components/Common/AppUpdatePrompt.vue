<template>
  <teleport to="body">
    <transition name="upd-fade">
      <div v-if="info" class="upd-mask" role="button" tabindex="0" aria-label="关闭" @click.self="later" @keydown.enter.prevent="later" @keydown.space.prevent="later">
        <div ref="card" v-bind="dialogAttrs" class="upd-card">
          <div class="upd-ico">💗</div>
          <div class="upd-title">发现新版本 v{{ info.versionName }}</div>
          <div v-if="info.changelog" class="upd-log">{{ info.changelog }}</div>

          <!-- iOS 专属引导：sideload 流程 -->
          <div v-if="platform === 'ios'" class="upd-tip">
            <div class="upd-tip-title">📲 iOS 更新流程</div>
            <ol class="upd-tip-steps">
              <li>点击下方「前往下载」打开 GitHub Releases</li>
              <li>下载 <code>App-unsigned.ipa</code></li>
              <li>用「全能签」签名后安装到手机</li>
            </ol>
          </div>

          <div class="upd-actions">
            <button class="upd-btn primary" :disabled="downloading" @click="go">
              {{ btnLabel }}
            </button>
            <button class="upd-btn" :disabled="downloading" @click="later">稍后</button>
          </div>
        </div>
      </div>
    </transition>
  </teleport>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useDialogA11y } from '@/composables/useDialogA11y';
import { getUpdateManifestUrl, getServerBase } from '@/config/server';

interface UpdateManifest {
  versionName: string;
  versionCode: number;
  // Android 为远程 WebView 模式（壳加载公网域名，前端改动实时生效），
  // 无需随版本重装；用独立版本号避免「更新→装旧壳→再提示」死循环。
  androidVersionCode?: number;
  changelog?: string;
  apkUrl?: string;
  releaseUrl?: string;
  minSupportedCode?: number;
}

const info = ref<UpdateManifest | null>(null);
const downloading = ref(false);
const platform = ref<'ios' | 'android' | 'web'>('web');
const LS_KEY = 'cl_update_dismiss';

const card = ref<HTMLElement>();
const isOpen = computed(() => !!info.value);

// 无障碍：对话框语义 + 焦点陷阱 + Esc + 焦点归还
const { dialogAttrs } = useDialogA11y({
  isOpen,
  close: () => {
    info.value = null;
  },
  dialogRef: card,
  ariaLabel: '发现新版本',
});

// Capacitor 全局访问
function getCap(): any {
  return (window as any).Capacitor;
}

function isNative(): boolean {
  const cap = getCap();
  return !!(cap && typeof cap.isNativePlatform === 'function' && cap.isNativePlatform());
}

function currentPlatform(): 'ios' | 'android' | 'web' {
  const cap = getCap();
  if (!cap?.isNativePlatform?.()) return 'web';
  const p = cap.getPlatform?.();
  return p === 'ios' || p === 'android' ? p : 'web';
}

// 原生 UpdatePlugin（Android 专属，用于下载并安装 APK）
function nativeUpdate(): any {
  const cap = getCap();
  if (!cap?.isNativePlatform?.() || cap.getPlatform() !== 'android') return null;
  return cap.Plugins?.Update ?? null;
}

// 跨平台获取当前安装版本号：优先用 @capacitor/app 的 getInfo()，
// 取 build 字段（CFBundleVersion / versionCode），Android 兜底用 UpdatePlugin。
async function getCurrentVersionCode(): Promise<number | null> {
  try {
    const cap = getCap();
    const info = await cap?.Plugins?.App?.getInfo?.();
    const build = info?.build ?? info?.versionCode;
    if (build != null) {
      const n = Number(build);
      if (Number.isFinite(n) && n > 0) return n;
    }
  } catch {
    /* 回退到 Android 原生插件 */
  }
  try {
    const upd = nativeUpdate();
    if (upd?.getVersionCode) {
      const { versionCode } = await upd.getVersionCode();
      if (versionCode) return Number(versionCode);
    }
  } catch {
    /* 静默 */
  }
  return null;
}

// 打开外部链接（iOS 走系统浏览器 / Android 走自定义标签 / Web 走新标签）
function openExternal(url: string) {
  const cap = getCap();
  if (cap?.Plugins?.Browser?.open) {
    cap.Plugins.Browser.open({ url }).catch(() => window.open(url, '_system'));
    return;
  }
  // Capacitor 在原生壳内会拦截 _system 跳到系统浏览器
  if (isNative()) {
    window.open(url, '_system');
  } else {
    window.open(url, '_blank', 'noopener');
  }
}

onMounted(async () => {
  if (!isNative()) return; // 浏览器/PWA 不检测（SW 自管更新）
  platform.value = currentPlatform();

  // 同一天已点过「稍后」，不打扰
  const dismissedDay = (() => {
    try { return localStorage.getItem(LS_KEY); } catch { return null; }
  })();
  if (dismissedDay === String(new Date().getDate())) return;

  const current = await getCurrentVersionCode();
  if (current == null) return;

  let manifest: UpdateManifest | null = null;
  try {
    // 打包 APK 时页面源是 https://localhost，相对 /app/version.json 取不到后端域名清单，
    // 因此改为随「服务器地址」指向用户后端（同源托管 /app/version.json）。
    const r = await fetch(getUpdateManifestUrl(), { cache: 'no-store' });
    if (r.ok) manifest = (await r.json()) as UpdateManifest;
  } catch {
    return;
  }
  if (!manifest?.versionCode) return;

  // 平台各自比对：Android 用 androidVersionCode（远程模式常驻最新，不提示），
  // iOS 用 versionCode（原生启动屏需重装才生效，故提示）。
  const targetCode =
    platform.value === 'android'
      ? (manifest.androidVersionCode ?? manifest.versionCode)
      : manifest.versionCode;
  const needUpdate = targetCode > current;
  if (needUpdate) info.value = manifest;
});

const btnLabel = computed(() => {
  if (downloading.value) return '处理中…';
  if (platform.value === 'ios') return '前往下载新版';
  return '立即更新';
});

// 相对地址（以 / 开头）按「服务器地址」拼成绝对 URL，使清单与 APK 同址托管时
// 无需在 version.json 里写死域名（部署方只放 /app/our-little-world-release.apk 即可）。
function resolveUrl(u?: string): string | undefined {
  if (!u) return undefined;
  if (/^https?:\/\//i.test(u)) return u;
  const base = getServerBase();
  return base ? `${base}${u.startsWith('/') ? '' : '/'}${u}` : u;
}

async function go() {
  const m = info.value;
  if (!m) return;
  const apk = resolveUrl(m.apkUrl);
  const fallback = resolveUrl(m.releaseUrl) || apk;
  if (platform.value === 'ios') {
    if (fallback) {
      openExternal(fallback);
      info.value = null; // 已引导去下载，关闭弹层
    }
    return;
  }
  // Android：优先原生插件「下载并安装」；未装插件时退化为系统浏览器打开 APK 下载。
  const upd = nativeUpdate();
  if (upd && apk) {
    downloading.value = true;
    try {
      await upd.downloadAndInstall({ url: apk });
      info.value = null; // 关闭弹层，系统下载通知接管
    } catch {
      // 失败兜底：打开 APK 下载地址（系统浏览器触发下载）
      if (fallback) openExternal(fallback);
      downloading.value = false;
    }
  } else if (fallback) {
    openExternal(fallback);
    info.value = null;
  }
}

function later() {
  try { localStorage.setItem(LS_KEY, String(new Date().getDate())); } catch { /* 忽略 */ }
  info.value = null;
}
</script>

<style scoped>
.upd-mask {
  position: fixed; inset: 0; z-index: 1500;
  background: rgba(60, 30, 35, 0.5);
  display: flex; align-items: center; justify-content: center;
  padding: calc(24px + env(safe-area-inset-top)) 24px calc(24px + env(safe-area-inset-bottom));
}
.upd-card {
  width: min(92vw, 360px); background: var(--color-surface);
  border-radius: 20px; padding: 26px 22px; text-align: center;
  box-shadow: 0 24px 60px -16px rgba(0, 0, 0, 0.35);
  max-height: 86vh; overflow-y: auto;
}
.upd-ico { font-size: 40px; }
.upd-title { margin-top: 10px; font-size: 17px; font-weight: 800; color: var(--color-ink); }
.upd-log {
  margin-top: 10px; font-size: 13px; color: var(--color-ink-2); line-height: 1.7;
  text-align: left; white-space: pre-line;
  background: color-mix(in srgb, var(--color-rose) 6%, transparent);
  padding: 10px 12px; border-radius: 10px;
}
.upd-tip {
  margin-top: 12px; padding: 12px 14px;
  background: color-mix(in srgb, var(--color-rose) 10%, transparent);
  border-radius: 12px; text-align: left;
}
.upd-tip-title { font-size: 13px; font-weight: 700; color: var(--color-ink); margin-bottom: 6px; }
.upd-tip-steps { margin: 0; padding-left: 20px; font-size: 12.5px; color: var(--color-ink-2); line-height: 1.7; }
.upd-tip-steps code {
  background: var(--color-surface-2); padding: 1px 6px; border-radius: 4px;
  font-size: 11.5px; font-family: ui-monospace, monospace;
}
.upd-actions { margin-top: 18px; display: flex; gap: 10px; justify-content: center; }
.upd-btn {
  padding: 10px 22px; border-radius: 999px; border: 1px solid var(--color-border);
  background: var(--color-surface); color: var(--color-ink-2); font-size: 14px; cursor: pointer;
}
.upd-btn.primary { background: var(--color-rose); border-color: var(--color-rose); color: var(--color-on-primary); font-weight: 600; }
.upd-btn:disabled { opacity: 0.5; cursor: default; }
.upd-fade-enter-active, .upd-fade-leave-active { transition: opacity 0.25s var(--ease-love); }
.upd-fade-enter-from, .upd-fade-leave-to { opacity: 0; }
:global(.reduce-motion) .upd-fade-enter-active,
:global(.reduce-motion) .upd-fade-leave-active { transition: none; }
</style>
