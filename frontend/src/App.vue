<template>
  <n-config-provider :theme="theme" :theme-overrides="overrides">
    <n-message-provider :duration="2400" placement="top" :max="3">
      <n-notification-provider>
        <AppRoot />
      </n-notification-provider>
    </n-message-provider>
  </n-config-provider>
</template>
<script setup lang="ts">
import AppRoot from '@/AppRoot.vue';
import { darkTheme, type GlobalThemeOverrides } from 'naive-ui';
import { computed } from 'vue';
import { useSettingStore, ACCENTS } from '@/store/settingStore';

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
    modalColor: 'var(--color-surface)',
    popoverColor: 'var(--color-surface)',
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
    boxShadow1: '0 1px 2px rgba(31,41,55,0.04), 0 10px 28px -10px rgba(122,100,98,0.16)',
    boxShadow2: '0 4px 12px rgba(31,41,55,0.06), 0 18px 44px -12px rgba(122,100,98,0.20)',
    boxShadow3: '0 4px 12px rgba(31,41,55,0.06), 0 18px 44px -12px rgba(122,100,98,0.20)',
  },
  Button: {
    borderRadius: '10',
    borderRadiusSmall: '8',
    fontWeight: '600',
    colorPrimary: acc.value.p,
    colorHoverPrimary: acc.value.h,
    colorPressedPrimary: acc.value.pr,
    colorFocusPrimary: acc.value.h,
    textColorPrimary: '#ffffff',
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
    tabTextColorActiveBar: acc.value.p,
    tabTextColorActive: acc.value.p,
    tabTextColorHover: acc.value.p,
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
    color: 'var(--color-surface)',
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
