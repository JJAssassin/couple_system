<template>
  <teleport to="body">
    <transition name="upd-fade">
      <div v-if="info" class="upd-mask" @click.self="later">
        <div class="upd-card">
          <div class="upd-ico">💗</div>
          <div class="upd-title">发现新版本 v{{ info.versionName }}</div>
          <div v-if="info.changelog" class="upd-log">{{ info.changelog }}</div>
          <div class="upd-actions">
            <button class="upd-btn primary" :disabled="downloading" @click="go">
              {{ downloading ? '下载中…' : '立即更新' }}
            </button>
            <button class="upd-btn" :disabled="downloading" @click="later">稍后</button>
          </div>
        </div>
      </div>
    </transition>
  </teleport>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';

interface UpdateManifest {
  versionCode: number;
  versionName: string;
  url: string;
  changelog?: string;
}

const info = ref<UpdateManifest | null>(null);
const downloading = ref(false);
const LS_KEY = 'cl_update_dismiss';

/** Capacitor 原生插件（壳内置 UpdatePlugin）的 JS 桥：window.Capacitor.Plugins.Update */
function nativeUpdate(): any {
  const cap = (window as any).Capacitor;
  if (!cap?.isNativePlatform?.() || cap.getPlatform() !== 'android') return null;
  return cap.Plugins?.Update ?? null;
}

onMounted(async () => {
  try {
    const upd = nativeUpdate();
    if (!upd) return; // 浏览器/PWA 环境不检测（PWA 自身有 SW 更新）
    if (localStorage.getItem(LS_KEY) === String(new Date().getDate())) return; // 当天已忽略
    const { versionCode: current } = await upd.getVersionCode();
    const manifest = await (await fetch('/app/version.json', { cache: 'no-store' })).json();
    if (manifest?.versionCode && manifest.versionCode > current) {
      info.value = manifest;
    }
  } catch {
    /* 网络/非 App 环境静默 */
  }
});

async function go() {
  const upd = nativeUpdate();
  const u = info.value;
  if (!upd || !u) return;
  downloading.value = true;
  try {
    await upd.downloadAndInstall({ url: u.url });
    info.value = null; // 关闭弹层，系统下载通知接管
  } catch {
    downloading.value = false;
  }
}

function later() {
  try {
    localStorage.setItem(LS_KEY, String(new Date().getDate()));
  } catch {
    /* 忽略 */
  }
  info.value = null;
}
</script>

<style scoped>
.upd-mask {
  position: fixed; inset: 0; z-index: 1500;
  background: rgba(60, 30, 35, 0.5);
  display: flex; align-items: center; justify-content: center; padding: 24px;
}
.upd-card {
  width: min(88vw, 320px); background: var(--color-surface);
  border-radius: 20px; padding: 26px 22px; text-align: center;
  box-shadow: 0 24px 60px -16px rgba(0, 0, 0, 0.35);
}
.upd-ico { font-size: 40px; }
.upd-title { margin-top: 10px; font-size: 17px; font-weight: 800; color: var(--color-ink); }
.upd-log { margin-top: 8px; font-size: 13px; color: var(--color-ink-2); line-height: 1.6; }
.upd-actions { margin-top: 18px; display: flex; gap: 10px; justify-content: center; }
.upd-btn {
  padding: 10px 22px; border-radius: 999px; border: 1px solid var(--color-border);
  background: var(--color-surface); color: var(--color-ink-2); font-size: 14px; cursor: pointer;
}
.upd-btn.primary { background: var(--color-rose); border-color: var(--color-rose); color: #fff; font-weight: 600; }
.upd-btn:disabled { opacity: 0.5; cursor: default; }
.upd-fade-enter-active, .upd-fade-leave-active { transition: opacity 0.25s var(--ease-love); }
.upd-fade-enter-from, .upd-fade-leave-to { opacity: 0; }
:global(.reduce-motion) .upd-fade-enter-active,
:global(.reduce-motion) .upd-fade-leave-active { transition: none; }
</style>
