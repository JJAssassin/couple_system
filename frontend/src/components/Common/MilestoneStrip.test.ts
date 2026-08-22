// @vitest-environment jsdom
/**
 * MilestoneStrip 恋爱里程碑组件测试
 */
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import MilestoneStrip from './MilestoneStrip.vue';

describe('MilestoneStrip', () => {
  it('渲染里程碑列表', () => {
    const w = mount(MilestoneStrip, {
      props: { totalDays: 365, loveStartTime: '2025-08-15' },
    });
    expect(w.text()).toContain('恋爱里程碑');
    expect(w.text()).toContain('一周年');
    expect(w.findAll('.ms').length).toBeGreaterThan(0);
    w.unmount();
  });

  it('未达里程碑显示进度条', () => {
    const w = mount(MilestoneStrip, {
      props: { totalDays: 50, loveStartTime: '2026-07-01' },
    });
    expect(w.text()).toContain('百日之好');
    expect(w.find('.ms-bar').exists()).toBe(true);
    w.unmount();
  });

  it('已达成里程碑显示「已达成」标签', () => {
    const w = mount(MilestoneStrip, {
      props: { totalDays: 400, loveStartTime: '2025-08-15' },
    });
    const reached = w.findAll('.ms.reached');
    expect(reached.length).toBeGreaterThan(0);
    expect(w.text()).toContain('已达成');
    w.unmount();
  });

  it('无 loveStartTime 且未达里程碑时无「已达成」', () => {
    const w = mount(MilestoneStrip, {
      props: { totalDays: 50 },
    });
    expect(w.text()).toContain('百日之好');
    expect(w.text()).not.toContain('已达成');
    w.unmount();
  });

  it('未达里程碑显示剩余天数', () => {
    const w = mount(MilestoneStrip, {
      props: { totalDays: 50, loveStartTime: '2026-07-01' },
    });
    // 百日之好（100天），已过50天，还差50天
    expect(w.text()).toContain('还差 50 天');
    w.unmount();
  });
});
