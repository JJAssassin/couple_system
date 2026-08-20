// @vitest-environment jsdom
/**
 * HeroIcon 组件测试：验证本地 heroicons 动态加载与未知图标降级。
 */
import { describe, it, expect, vi, afterEach } from 'vitest';
import { mount } from '@vue/test-utils';
import HeroIcon from './HeroIcon.vue';

describe('HeroIcon', () => {
  afterEach(() => vi.restoreAllMocks());

  it('已知图标渲染为 svg 且应用尺寸', () => {
    const w = mount(HeroIcon, { props: { name: 'heart', size: 24 } });
    expect(w.find('svg').exists()).toBe(true);
    expect(w.attributes('style')).toContain('width: 24px');
    expect(w.attributes('style')).toContain('height: 24px');
    w.unmount();
  });

  it('solid 变体同样可渲染', () => {
    const w = mount(HeroIcon, { props: { name: 'heart-solid' } });
    expect(w.find('svg').exists()).toBe(true);
    w.unmount();
  });

  it('未知图标：不渲染并告警', () => {
    const warn = vi.spyOn(console, 'warn').mockImplementation(() => {});
    const w = mount(HeroIcon, { props: { name: 'not-exists-icon' } });
    expect(w.find('.hero-icon').exists()).toBe(false);
    expect(warn).toHaveBeenCalledWith(expect.stringContaining('not-exists-icon'));
    w.unmount();
  });
});
