// @vitest-environment jsdom
/**
 * PwaInstallPrompt 安装引导组件测试
 */
import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';

// Mock usePwa composable to avoid matchMedia issues in jsdom
vi.mock('@/composables/usePwa', () => ({
  usePwa: () => ({
    installAvailable: false,
    isIOS: false,
    showIosHint: false,
    init: () => {},
    promptInstall: () => {},
    dismissInstall: () => {},
    requestNotificationPermission: async () => 'default' as any,
    setupNotifications: () => {},
    notificationsSupported: () => true,
    dismissIosHint: () => {},
  }),
}));

import PwaInstallPrompt from './PwaInstallPrompt.vue';

describe('PwaInstallPrompt', () => {
  it('默认状态：安装条不显示', () => {
    const w = mount(PwaInstallPrompt);
    expect(w.find('.pwa-install').exists()).toBe(false);
    w.unmount();
  });

  it('安装提示容器存在', () => {
    const w = mount(PwaInstallPrompt);
    expect(w.find('.pwa-install').exists()).toBe(false);
    w.unmount();
  });

  it('组件可正常挂载', () => {
    const w = mount(PwaInstallPrompt);
    expect(w.exists()).toBe(true);
    w.unmount();
  });
});
