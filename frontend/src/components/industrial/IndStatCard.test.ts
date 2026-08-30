// @vitest-environment jsdom
/**
 * IndStatCard 测试：数字滚动（count-up）的行为契约。
 * - 无数字 value 直接透传；slot 优先于动画值
 * - reduce-motion 直出终值，不做动画
 * - 前缀/后缀/小数位原样保留；动画 800ms 后到位
 */
import { describe, it, expect, vi, afterEach } from 'vitest';
import { mount } from '@vue/test-utils';
import IndStatCard from './IndStatCard.vue';

afterEach(() => {
  document.documentElement.classList.remove('reduce-motion');
  vi.useRealTimers();
  vi.unstubAllGlobals();
});

/** jsdom 的 rAF 不受 fake timers 驱动：用 faked setTimeout shim 掉，并 fake performance 使动画时钟前进 */
function stubRaf() {
  // toFake 会替换默认列表：setTimeout/clearTimeout/performance 都要显式列出
  vi.useFakeTimers({ toFake: ['performance', 'setTimeout', 'clearTimeout'] });
  vi.stubGlobal('requestAnimationFrame', (cb: (t: number) => void) => setTimeout(() => cb(performance.now()), 16) as unknown as number);
  vi.stubGlobal('cancelAnimationFrame', (id: unknown) => clearTimeout(id as number));
}

describe('IndStatCard', () => {
  it('无数字的 value 直接透传（如 "—"）', async () => {
    const w = mount(IndStatCard, { props: { label: 'x', value: '—' } });
    await w.vm.$nextTick();
    expect(w.find('.ind-stat-v').text()).toBe('—');
    w.unmount();
  });

  it('slot 内容优先于动画值', () => {
    const w = mount(IndStatCard, {
      props: { label: 'x', value: '5' },
      slots: { default: '自定义' },
    });
    expect(w.find('.ind-stat-v').text()).toBe('自定义');
    w.unmount();
  });

  it('reduce-motion 时直出终值（含前后缀与小数位）', async () => {
    document.documentElement.classList.add('reduce-motion');
    const w = mount(IndStatCard, { props: { label: 'x', value: '¥123.00' } });
    await w.vm.$nextTick();
    expect(w.find('.ind-stat-v').text()).toBe('¥123.00');
    w.unmount();
  });

  it('动画从 0 滚到终值，前后缀保留', async () => {
    stubRaf();
    const w = mount(IndStatCard, { props: { label: 'x', value: '¥123.00' } });
    // jsdom 无 IntersectionObserver → 挂载即开始动画；先显示占位 0 值
    await w.vm.$nextTick();
    expect(w.find('.ind-stat-v').text()).toBe('¥0.00');
    await vi.advanceTimersByTimeAsync(900);
    await w.vm.$nextTick();
    expect(w.find('.ind-stat-v').text()).toBe('¥123.00');
    w.unmount();
  });

  it('百分比后缀与一位小数保留', async () => {
    stubRaf();
    const w = mount(IndStatCard, { props: { label: 'x', value: '72.5%' } });
    await vi.advanceTimersByTimeAsync(900);
    await w.vm.$nextTick();
    expect(w.find('.ind-stat-v').text()).toBe('72.5%');
    w.unmount();
  });
});
