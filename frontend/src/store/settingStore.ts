import { defineStore } from 'pinia';
import { ref, computed, watch } from 'vue';

const LS_MODE = 'cl_theme_mode'; // 'light' | 'dark' | 'system'
const LS_DARK = 'cl_dark'; // 旧版布尔（兼容迁移）
const LS_MOTION = 'cl_reduce_motion';

export type ThemeMode = 'light' | 'dark' | 'system';

function readInitialMode(): ThemeMode {
  const m = localStorage.getItem(LS_MODE);
  if (m === 'light' || m === 'dark' || m === 'system') return m;
  // 兼容旧版 cl_dark
  const legacy = localStorage.getItem(LS_DARK);
  if (legacy === '1') return 'dark';
  if (legacy === '0') return 'light';
  return 'light';
}

export const useSettingStore = defineStore('setting', () => {
  const mode = ref<ThemeMode>(readInitialMode());
  const reduceMotion = ref(localStorage.getItem(LS_MOTION) === '1');

  // 系统外观偏好（仅 mode === 'system' 时生效）
  const mql =
    typeof window !== 'undefined' && window.matchMedia
      ? window.matchMedia('(prefers-color-scheme: dark)')
      : null;
  const systemDark = ref(mql ? mql.matches : false);

  // 解析后的「当前是否深色」——驱动 html.dark 与 Naive darkTheme
  const dark = computed(
    () => mode.value === 'dark' || (mode.value === 'system' && systemDark.value),
  );

  function apply() {
    if (typeof document === 'undefined') return;
    document.documentElement.classList.toggle('dark', dark.value);
    document.documentElement.classList.toggle('reduce-motion', reduceMotion.value);
  }
  // 启动即应用已保存偏好（main 中调用）
  function hydrate() {
    apply();
  }
  // 设置外观模式（显式 浅/深 或 跟随系统），持久化
  function setMode(next: ThemeMode) {
    mode.value = next;
    localStorage.setItem(LS_MODE, next);
    apply();
  }
  // 快速切换：在显式 浅/深 间翻转（来自顶栏按钮）
  function toggleDark() {
    setMode(dark.value ? 'light' : 'dark');
  }
  function toggleMotion() {
    reduceMotion.value = !reduceMotion.value;
    localStorage.setItem(LS_MOTION, reduceMotion.value ? '1' : '0');
    apply();
  }

  // 跟随系统：监听系统主题变化，仅 system 模式下实时应用
  if (mql) {
    const handler = (e: MediaQueryListEvent) => {
      systemDark.value = e.matches;
      if (mode.value === 'system') apply();
    };
    mql.addEventListener('change', handler);
  }
  // 解析值变化即重应用（覆盖 systemDark 变化 / 直接切 mode 两种路径）
  watch(dark, apply);

  return { mode, dark, reduceMotion, hydrate, setMode, toggleDark, toggleMotion };
});
