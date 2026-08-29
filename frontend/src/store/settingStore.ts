import { defineStore } from 'pinia';
import { ref, computed, watch } from 'vue';

const LS_MODE = 'cl_theme_mode'; // 'light' | 'dark' | 'system'
const LS_DARK = 'cl_dark'; // 旧版布尔（兼容迁移）
const LS_MOTION = 'cl_reduce_motion';
const LS_ACCENT = 'cl_accent'; // 主题色 key
const LS_NOTIFY = 'cl_notify'; // 消息系统通知开关

export type ThemeMode = 'light' | 'dark' | 'system';

/** 主题色调色板：每套提供主色族（p/h/pr）+ 明暗两套柔和底/装饰次色 */
export interface AccentPalette {
  label: string;
  desc: string;
  p: string;   // 主色 / 强调
  h: string;   // hover
  pr: string;  // pressed
  softL: string; // 浅色模式柔和底
  softD: string; // 深色模式柔和底
  deepL: string; // 浅色模式装饰次色
  deepD: string; // 深色模式装饰次色
  tx: string;    // 文字安全色：同色系加深（降饱和），用于把主色当作「文字」的场景。
                 // 主色 p 是高明度粉彩，直接作文字仅 2.15~3.26:1（不达 WCAG AA 4.5:1）；
                 // tx 实测 on 奶油底 ≈5.0:1、on 白卡 ≈5.4:1、on 柔和底 ≈4.65:1，全部达标。
                 // 由脚本 solve_text_colors2.py 按「保持色相 + 降饱和度 + 加深」求解。
}

/** 预设主题色（情侣可共用专属色；首个为默认经典玫瑰，与旧版硬编码一致） */
export const ACCENTS: Record<string, AccentPalette> = {
  rose:     { label: '经典玫瑰', desc: '原版柔珊瑚', p: '#ff6f7d', h: '#ff8893', pr: '#e25a68', softL: '#ffe9ec', softD: 'rgba(255,111,125,0.16)', deepL: '#D88593', deepD: '#e3a3ad', tx: '#ce2232' },
  sakura:   { label: '樱花粉',   desc: '甜系少女',   p: '#ff8fab', h: '#ffa7c0', pr: '#e06a88', softL: '#ffe6ee', softD: 'rgba(255,143,171,0.16)', deepL: '#f3a0bb', deepD: '#f6b3c7', tx: '#cc214c' },
  ocean:    { label: '海洋蓝',   desc: '清新静谧',   p: '#3ea6e0', h: '#5fb8ea', pr: '#2c82b8', softL: '#e3f2fb', softD: 'rgba(62,166,224,0.16)',   deepL: '#6fb6d8', deepD: '#a7d3ec', tx: '#2f7095' },
  mint:     { label: '薄荷绿',   desc: '治愈自然',   p: '#34c0a3', h: '#54cdb4', pr: '#23937c', softL: '#def6f0', softD: 'rgba(52,192,163,0.16)',   deepL: '#5fc3ae', deepD: '#9bddcf', tx: '#317567' },
  twilight: { label: '暮光紫',   desc: '神秘浪漫',   p: '#9b7ede', h: '#b098e6', pr: '#7a5fc4', softL: '#efe9fb', softD: 'rgba(155,126,222,0.16)', deepL: '#b09ee2', deepD: '#cab8ee', tx: '#7657bc' },
};

/**
 * 把 #rrggbb 转成 CSS `rgb()` 所需的「R G B」空格分隔通道串，
 * 便于用 `rgb(var(--color-rose-rgb) / 0.16)` 这类语法派生任意透明度的同色系色值。
 * 解析失败时回退到经典玫瑰，避免因脏数据把整站颜色打空。
 */
function hexToRgbChannels(hex: string): string {
  const m = /^#?([0-9a-f]{6})$/i.exec((hex || '').trim());
  if (!m) return '255 111 125'; // 经典玫瑰 #ff6f7d
  const n = parseInt(m[1], 16);
  return `${(n >> 16) & 255} ${(n >> 8) & 255} ${n & 255}`;
}

function readInitialAccent(): string {
  const a = localStorage.getItem(LS_ACCENT);
  return a && ACCENTS[a] ? a : 'rose';
}

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
  const accent = ref<string>(readInitialAccent());
  const notifications = ref(localStorage.getItem(LS_NOTIFY) === '1');

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

  // 将当前主题色注入到 :root 的 CSS 变量上（覆盖全局样式表默认值）。
  // 注意：内联样式优先级高于 html.dark 样式表，因此必须按当前明暗模式给出对应柔和底/装饰次色。
  function applyAccent() {
    if (typeof document === 'undefined') return;
    const a = ACCENTS[accent.value] ?? ACCENTS.rose;
    const root = document.documentElement.style;
    const darkMode = dark.value;
    root.setProperty('--color-rose', a.p);
    root.setProperty('--color-rose-hover', a.h);
    root.setProperty('--color-rose-pressed', a.pr);
    root.setProperty('--color-rose-soft', darkMode ? a.softD : a.softL);
    root.setProperty('--color-rose-deep', darkMode ? a.deepD : a.deepL);
    root.setProperty('--color-accent', a.p);
    root.setProperty('--color-accent-soft', darkMode ? a.softD : a.softL);

    // —— 文字安全色：必须按模式切换 ——
    // 浅色：主色是粉彩色，直接作文字仅 2.15~3.26:1，改用同色系加深版 tx（实测 ≥5.0:1）
    // 暗色：亮主色在深底上本就 5.25~7.97:1 达标，而加深版 tx 只有 3.15~3.18:1 反而不达标
    //       → 暗色下必须继续用亮主色 p，不能一刀切用 tx
    root.setProperty('--color-rose-text', darkMode ? a.p : a.tx);
    root.setProperty('--color-accent-text', darkMode ? a.p : a.tx);

    // —— 派生色：把原先写死的玫瑰色字面量改为按主色派生 ——
    // 全站有 65 处硬编码品牌色字面量（rgba(255,111,125,…)×41 / #ff6f7d×18 /
    // rgba(216,133,147,…)×6），主题色切换时它们不会跟随。这里提供统一的派生出口，
    // 后续把那些字面量逐步替换为下列变量即可闭环（见审计报告 P0-2）。
    const rgb = hexToRgbChannels(a.p); // "255 111 125"，供 rgb(R G B / alpha) 使用
    root.setProperty('--color-rose-rgb', rgb);
    root.setProperty('--color-rose-tint', `rgb(${rgb} / 0.16)`);
    // 极光光斑（AuroraBackdrop 的 4 个 blob 原为写死的玫瑰/玫瑰粉色）
    root.setProperty('--aurora-1', `rgb(${rgb} / 0.55)`);
    root.setProperty('--aurora-2', `rgb(${hexToRgbChannels(darkMode ? a.deepD : a.deepL)} / 0.5)`);
    // 玻璃辉光阴影（原 --shadow-glass-lg 内含写死的 rgba(255,111,125,…)）
    root.setProperty(
      '--shadow-glass-lg',
      darkMode
        ? `0 16px 40px -8px rgb(${rgb} / 0.28), 0 4px 12px -2px rgba(0, 0, 0, 0.4)`
        : `0 16px 40px -8px rgb(${rgb} / 0.20), 0 4px 12px -2px rgba(0, 0, 0, 0.03)`,
    );
  }

  function apply() {
    if (typeof document === 'undefined') return;
    document.documentElement.classList.toggle('dark', dark.value);
    document.documentElement.classList.toggle('reduce-motion', reduceMotion.value);
    applyAccent();
  }
  // 切换主题色：持久化并即时应用（全站 CSS 变量 + NaiveUI overrides 自动跟随）
  function setAccent(next: string) {
    if (!ACCENTS[next]) return;
    accent.value = next;
    localStorage.setItem(LS_ACCENT, next);
    apply();
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
  // 显式设置（供 FinesseSwitch 双向绑定的 update:modelValue 使用）
  function setReduceMotion(v: boolean) {
    reduceMotion.value = v;
    localStorage.setItem(LS_MOTION, v ? '1' : '0');
    apply();
  }
  // 消息系统通知开关（开启时由 PWA 后台通知触发；真实授权在设置页切开关时申请）
  function setNotifications(v: boolean) {
    notifications.value = v;
    localStorage.setItem(LS_NOTIFY, v ? '1' : '0');
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

  return { mode, dark, reduceMotion, accent, notifications, hydrate, setMode, toggleDark, toggleMotion, setReduceMotion, setNotifications, setAccent };
});
