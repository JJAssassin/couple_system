// @vitest-environment jsdom
/**
 * LoveCount 数字滚动组件测试
 */
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import LoveCount from './LoveCount.vue';

describe('LoveCount', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('渲染数字元素', () => {
    const w = mount(LoveCount, {
      props: { value: 100 },
    });
    expect(w.find('.love-count').exists()).toBe(true);
    // 动画可能已完成或进行中，只要元素存在即可
    expect(w.text()).toMatch(/\d/);
    w.unmount();
  });

  it('接受 value 和 duration props', () => {
    const w = mount(LoveCount, {
      props: { value: 50, duration: 500 },
    });
    expect(w.props('value')).toBe(50);
    expect(w.props('duration')).toBe(500);
    w.unmount();
  });

  it('默认 duration 为 1200', () => {
    const w = mount(LoveCount, {
      props: { value: 100 },
    });
    expect(w.props('duration')).toBe(1200);
    w.unmount();
  });
});
