// @vitest-environment jsdom
/**
 * AppUpdatePrompt 组件测试：
 * 覆盖「平台版本比对」核心逻辑 —— Android 用 androidVersionCode（远程模式防死循环）、
 * iOS 用 versionCode 提示重装，以及「稍后」本地记忆。
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import AppUpdatePrompt from './AppUpdatePrompt.vue';

type Manifest = {
  versionName: string;
  versionCode: number;
  androidVersionCode?: number;
  changelog?: string;
  releaseUrl?: string;
  minSupportedCode?: number;
};

/** 构造 Capacitor 全局 mock：isNativePlatform / getPlatform / App.getInfo */
function stubCapacitor(platform: 'ios' | 'android', build: number) {
  (window as any).Capacitor = {
    isNativePlatform: () => true,
    getPlatform: () => platform,
    Plugins: {
      App: { getInfo: async () => ({ build: String(build) }) },
    },
  };
}

function stubFetch(manifest: Manifest | null) {
  vi.stubGlobal('fetch', async () => ({
    ok: true,
    json: async () => manifest,
  }));
}

const manifest: Manifest = {
  versionName: '1.3',
  versionCode: 4,
  androidVersionCode: 3,
  changelog: '• 测试更新',
  releaseUrl: 'https://github.com/JJAssassin/couple_system/releases/latest',
};

beforeEach(() => {
  localStorage.clear();
  vi.restoreAllMocks();
  delete (window as any).Capacitor;
});

afterEach(() => {
  vi.unstubAllGlobals();
  // 清理 teleport 到 body 的残留 DOM，避免跨用例状态泄漏
  document.body.querySelectorAll('.upd-mask').forEach((el) => el.remove());
});

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import AppUpdatePrompt from './AppUpdatePrompt.vue';

const attachToBody = { attachTo: document.body };

describe('AppUpdatePrompt 版本比对', () => {
  it('浏览器/PWA（无 Capacitor）不检测、不弹层', async () => {
    const fetchMock = vi.fn();
    vi.stubGlobal('fetch', fetchMock);
    const w = mount(AppUpdatePrompt, attachToBody);
    await flushPromises();
    expect(fetchMock).not.toHaveBeenCalled();
    expect(document.body.querySelector('.upd-mask')).toBe(null);
    w.unmount();
  });

  it('Android 且 androidVersionCode 未增长（远程模式）→ 不提示（防死循环）', async () => {
    stubCapacitor('android', 3); // 已装壳 code=3
    stubFetch(manifest);         // 清单 androidVersionCode=3
    const w = mount(AppUpdatePrompt, attachToBody);
    await flushPromises();
    expect(document.body.querySelector('.upd-mask')).toBe(null);
    w.unmount();
  });

  it('Android 且 androidVersionCode 增长 → 提示新版本', async () => {
    stubCapacitor('android', 2);
    stubFetch({ ...manifest, androidVersionCode: 4 });
    const w = mount(AppUpdatePrompt, attachToBody);
    await flushPromises();
    const mask = document.body.querySelector('.upd-mask');
    expect(mask).not.toBe(null);
    expect(mask?.textContent).toContain('v1.3');
    expect(mask?.textContent).toContain('立即更新');
    w.unmount();
  });

  it('iOS 且 versionCode 增长 → 提示并展示全能签引导', async () => {
    stubCapacitor('ios', 3);
    stubFetch(manifest); // versionCode=4 > 3
    const w = mount(AppUpdatePrompt, attachToBody);
    await flushPromises();
    const mask = document.body.querySelector('.upd-mask');
    expect(mask).not.toBe(null);
    expect(mask?.textContent).toContain('v1.3');
    expect(mask?.textContent).toContain('iOS 更新流程');
    expect(mask?.textContent).toContain('前往下载新版');
    w.unmount();
  });

  it('iOS 版本一致 → 不提示', async () => {
    stubCapacitor('ios', 4);
    stubFetch(manifest);
    const w = mount(AppUpdatePrompt, attachToBody);
    await flushPromises();
    expect(document.body.querySelector('.upd-mask')).toBe(null);
    w.unmount();
  });

  it('低于 minSupportedCode（须升级）→ 仍提示', async () => {
    stubCapacitor('android', 1);
    stubFetch({ ...manifest, androidVersionCode: 4, minSupportedCode: 2 });
    const w = mount(AppUpdatePrompt, attachToBody);
    await flushPromises();
    expect(document.body.querySelector('.upd-mask')).not.toBe(null);
    w.unmount();
  });

  it('点「稍后」→ localStorage 记录当天并关闭弹层', async () => {
    stubCapacitor('ios', 3);
    stubFetch(manifest);
    const w = mount(AppUpdatePrompt, attachToBody);
    await flushPromises();
    const btn = document.body.querySelector('.upd-btn:not(.primary)') as HTMLElement | null;
    expect(btn).not.toBe(null);
    btn?.click();
    await flushPromises();
    expect(document.body.querySelector('.upd-mask')).toBe(null);
    expect(localStorage.getItem('cl_update_dismiss')).toBe(String(new Date().getDate()));
    w.unmount();
  });
});
