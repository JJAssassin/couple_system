// @vitest-environment jsdom
/**
 * GlobalLoadingBar 测试：验证顶栏加载条显隐逻辑、路由布尔信号的重定向防泄漏、无障碍角色。
 */
import { describe, it, expect, vi, afterEach } from 'vitest';
import { mount } from '@vue/test-utils';
import { nextTick } from 'vue';
import GlobalLoadingBar from './GlobalLoadingBar.vue';
import { useGlobalLoading } from '@/composables/useGlobalLoading';

describe('GlobalLoadingBar', () => {
  afterEach(() => {
    // 复位单例状态，避免测试间泄漏
    useGlobalLoading().endNav();
    vi.useRealTimers();
  });

  it('导航开始→延迟 120ms 显示 on，结束后→延迟 360ms 隐藏', async () => {
    vi.useFakeTimers();
    const w = mount(GlobalLoadingBar);
    const { startNav, endNav } = useGlobalLoading();

    startNav();
    await nextTick(); // 让 watch 回调执行，设置 120ms 显示计时器
    expect(w.find('.glb').classes()).not.toContain('on'); // 120ms 内不显示，防闪烁
    vi.advanceTimersByTime(200);
    await nextTick(); // 计时器触发 visible=true → DOM 刷新
    expect(w.find('.glb').classes()).toContain('on');

    endNav();
    await nextTick(); // 让 watch 回调执行，设置 360ms 收尾计时器
    vi.advanceTimersByTime(400);
    await nextTick(); // 收尾动画结束 → DOM 刷新
    expect(w.find('.glb').classes()).not.toContain('on');
    w.unmount();
  });

  it('守卫重定向式「双 start 单 end」不泄漏（布尔信号）', async () => {
    vi.useFakeTimers();
    const w = mount(GlobalLoadingBar);
    const { startNav, endNav } = useGlobalLoading();

    startNav(); // 原始导航（被守卫重定向取消）
    startNav(); // 重定向后的最终导航
    endNav(); // 仅最终导航触发 afterEach
    await nextTick();
    vi.advanceTimersByTime(500);
    await nextTick();
    expect(w.find('.glb').classes()).not.toContain('on'); // 布尔信号：已结束→不卡死
    w.unmount();
  });

  it('带无障碍角色与标签', () => {
    const w = mount(GlobalLoadingBar);
    const root = w.find('.glb');
    expect(root.attributes('role')).toBe('status');
    expect(root.attributes('aria-label')).toBe('加载中');
    w.unmount();
  });
});
