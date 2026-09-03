<template>
  <n-config-provider :theme="theme" :theme-overrides="overrides">
    <n-message-provider :duration="2400" placement="top" :max="3">
      <n-notification-provider>
        <AppRoot />
      </n-notification-provider>
    </n-message-provider>
  </n-config-provider>
  <!-- PWA 安装提示：由 VitePWA 生成 SW，本组件只负责 beforeinstallprompt 引导 -->
  <PwaInstallPrompt />
  <!-- 离线状态指示：Service Worker 已支持离线访问，断网时给明确反馈而非转圈/报错 -->
  <OfflineBanner />
</template>
<script setup lang="ts">
import AppRoot from '@/AppRoot.vue';
import { darkTheme, type GlobalThemeOverrides } from 'naive-ui';
import { computed } from 'vue';
import { useSettingStore, ACCENTS } from '@/store/settingStore';
import PwaInstallPrompt from '@/components/Common/PwaInstallPrompt.vue';
import OfflineBanner from '@/components/Common/OfflineBanner.vue';

// 让 NaiveUI 组件（对话框/输入/按钮/卡片/表格…）跟随浪漫柔光配色。
// 颜色尽量引用 CSS 变量，使 html.dark 切换时自动适配，无需两套覆盖。
const setting = useSettingStore();
const theme = computed(() => (setting.dark ? darkTheme : null));
const acc = computed(() => ACCENTS[setting.accent] ?? ACCENTS.rose);
// overrides 改为 computed 跟随主题色：主色族（p/h/pr）取自当前调色板，其余表面/文字仍走 CSS 变量自动明暗适配
const overrides = computed<GlobalThemeOverrides>(() => ({
  common: {
    primaryColor: acc.value.p,
    primaryColorHover: acc.value.h,
    primaryColorPressed: acc.value.pr,
    primaryColorSuppl: acc.value.h,
    borderRadius: '10',
    fontFamily: 'var(--font-sans)',
    // 表面与文字（暗色下由 CSS 变量自动切换）
    bodyColor: 'var(--color-cream)',
    cardColor: 'var(--color-surface)',
    modalColor: 'var(--glass-surface-strong)',
    popoverColor: 'var(--glass-surface-strong)',
    tableColor: 'var(--color-surface)',
    tableHeaderColor: 'var(--color-surface-2)',
    borderColor: 'var(--color-border)',
    dividerColor: 'var(--color-border)',
    textColor1: 'var(--color-ink)',
    textColor2: 'var(--color-ink-2)',
    textColor3: 'var(--color-ink-3)',
    placeholderColor: 'var(--color-ink-3)',
    placeholderColorDisabled: 'var(--color-ink-3)',
    iconColor: 'var(--color-ink-2)',
    iconColorHover: 'var(--color-rose)',
    // 改引用令牌：--shadow-card / --shadow-float 指向 --shadow-glass-md / --shadow-glass-lg，
    // 后者在 global.css 的 html.dark 内有专门的暗色重定义（深色底改用 rgba(0,0,0,.45)）。
    // 原先这里的字面量是近黑阴影，在暗色 #2a2429 上完全不可见 —— 导致全站所有
    // naive-ui 浮层（NModal / NPopover / NSelect 下拉 / NDatePicker 面板 /
    // NPopconfirm / NDrawer / NMessage）在暗色下没有投影。
    boxShadow1: 'var(--shadow-card)',
    boxShadow2: 'var(--shadow-overlay)',
    boxShadow3: 'var(--shadow-overlay)',
  },
  Button: {
    borderRadius: '10',
    borderRadiusSmall: '8',
    fontWeight: '600',
    colorPrimary: acc.value.p,
    colorHoverPrimary: acc.value.h,
    colorPressedPrimary: acc.value.pr,
    colorFocusPrimary: acc.value.h,
    // 白字 on 亮主色实测仅 2.15~3.26:1（5 套主题色全部不达 AA 4.5:1，樱花粉最差）。
    // 改用暖近黑 --color-on-primary(#2b1416)：实测 5.30~8.04:1 全部达标，
    // 且品牌色一个像素都不用改、明暗模式通用（对比度只取决于底与字）。
    textColorPrimary: 'var(--color-on-primary)',
    borderColorPrimary: acc.value.p,
  },
  Input: {
    color: 'var(--color-surface)',
    colorFocus: 'var(--color-surface)',
    boxShadowFocus: '0 0 0 2px var(--color-rose-soft)',
    border: '1px solid var(--color-border)',
    borderHover: '1px solid var(--color-rose-soft)',
    borderFocus: '1px solid var(--color-rose)',
    borderRadius: '10',
    colorDisabled: 'var(--color-surface-2)',
  },
  Card: {
    color: 'var(--color-surface)',
    borderColor: 'var(--color-border)',
    borderRadius: '16',
    titleTextColor: 'var(--color-ink)',
    colorEmbedded: 'var(--color-surface-2)',
  },
  Table: {
    borderColor: 'var(--color-border)',
    thColor: 'var(--color-surface-2)',
    thTextColor: 'var(--color-ink-2)',
    tdColor: 'var(--color-surface)',
    tdColorHover: 'var(--color-surface-2)',
    tdTextColor: 'var(--color-ink)',
    borderRadius: '10',
  },
  Tabs: {
    // 指示条与 hover 属装饰，继续用亮主色；
    // 但「激活态文字」是文字 —— 实测主色作文字仅 2.68:1（樱花粉 2.15:1），
    // 改用按模式切换的文字安全色（浅色用加深版、暗色用亮主色，见 applyAccent 注释）。
    // 影响 Wish / Todo 的页面主筛选控件 <n-tabs type="segment">。
    tabTextColorActiveBar: acc.value.p,
    tabTextColorActive: 'var(--color-rose-text)',
    tabTextColorHover: 'var(--color-rose-text)',
    tabTextColor: 'var(--color-ink-2)',
    barColor: acc.value.p,
    paneTextColor: 'var(--color-ink)',
    tabFontWeightActive: '600',
  },
  Tag: {
    borderRadius: '8',
    colorBordered: 'var(--color-surface)',
  },
  Modal: {
    color: 'var(--color-surface)',
    borderRadius: '16',
    titleTextColor: 'var(--color-ink)',
  },
  Drawer: {
    color: 'var(--glass-surface-strong)',
  },
  Message: {
    color: 'var(--color-surface)',
    colorSuccess: 'var(--color-surface)',
    colorWarning: 'var(--color-surface)',
    colorError: 'var(--color-surface)',
    colorInfo: 'var(--color-surface)',
    borderRadius: '10',
    textColor: 'var(--color-ink)',
    textColorSuccess: 'var(--color-ink)',
    textColorWarning: 'var(--color-ink)',
    textColorError: 'var(--color-ink)',
    textColorInfo: 'var(--color-ink)',
  },
  Dialog: {
    borderRadius: '16',
  },
}));
</script>
