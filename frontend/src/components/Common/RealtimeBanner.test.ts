import { describe, it, expect, vi, beforeEach } from 'vitest';
import { mount } from '@vue/test-utils';
import { ref, nextTick } from 'vue';

type ConnState = 'idle' | 'connecting' | 'connected' | 'reconnecting' | 'disconnected';

// 顶层 const + 普通 vi.mock：factory 在模块被 import 时才执行，此时 ref 已初始化（vitest 官方支持）。
// 必须用真正的 ref：组件模板会对 ref 自动解包，普通 { value } 对象在模板里比较会恒为 false。
const mockConnState = ref<ConnState>('connected');
const mockReconnect = vi.fn();
const mockOnline = ref(true);

vi.mock('@/composables/useRealtime', () => ({
  useRealtime: () => ({ connState: mockConnState, reconnect: mockReconnect }),
}));
vi.mock('@/composables/useOnlineStatus', () => ({
  useOnlineStatus: () => ({ isOnline: mockOnline }),
}));

import RealtimeBanner from '@/components/Common/RealtimeBanner.vue';

describe('RealtimeBanner', () => {
  beforeEach(() => {
    mockReconnect.mockClear();
    mockConnState.value = 'connected';
    mockOnline.value = true;
    document.body.innerHTML = '';
  });

  it('连接正常时隐藏提示条', () => {
    mockConnState.value = 'connected';
    const w = mount(RealtimeBanner);
    expect(document.body.querySelector('.rt-bar')).toBeNull();
    w.unmount();
  });

  it('断线时显示并重连按钮可触发 reconnect', async () => {
    mockConnState.value = 'disconnected';
    const w = mount(RealtimeBanner);
    expect(document.body.querySelector('.rt-bar')).not.toBeNull();
    const btn = document.body.querySelector('.rt-btn') as HTMLButtonElement | null;
    expect(btn).not.toBeNull();
    btn!.click();
    await nextTick();
    expect(mockReconnect).toHaveBeenCalledTimes(1);
    w.unmount();
  });

  it('重连中显示但不含重连按钮', () => {
    mockConnState.value = 'reconnecting';
    const w = mount(RealtimeBanner);
    expect(document.body.querySelector('.rt-bar')).not.toBeNull();
    expect(document.body.querySelector('.rt-btn')).toBeNull();
    w.unmount();
  });

  it('浏览器离线时不显示（交由 OfflineBanner）', () => {
    mockOnline.value = false;
    mockConnState.value = 'disconnected';
    const w = mount(RealtimeBanner);
    expect(document.body.querySelector('.rt-bar')).toBeNull();
    w.unmount();
  });
});
