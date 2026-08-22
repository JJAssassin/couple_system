// @vitest-environment jsdom
/**
 * IndCard 卡片组件测试
 */
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import IndCard from './IndCard.vue';
import IndLed from './IndLed.vue';

describe('IndCard', () => {
  it('默认渲染为 section 元素', () => {
    const w = mount(IndCard, {
      slots: { default: '卡片内容' },
    });
    expect(w.element.tagName).toBe('SECTION');
    expect(w.text()).toContain('卡片内容');
    w.unmount();
  });

  it('支持自定义 as 属性渲染为 div', () => {
    const w = mount(IndCard, {
      props: { as: 'div' },
      slots: { default: '内容' },
    });
    expect(w.element.tagName).toBe('DIV');
    w.unmount();
  });

  it('显示标题和 LED 指示灯', () => {
    const w = mount(IndCard, {
      props: { title: '卡片标题', led: true },
      slots: { default: '内容' },
    });
    expect(w.text()).toContain('卡片标题');
    expect(w.findComponent(IndLed).exists()).toBe(true);
    w.unmount();
  });

  it('不显示 LED 当 led=false', () => {
    const w = mount(IndCard, {
      props: { title: '标题', led: false },
      slots: { default: '内容' },
    });
    expect(w.findComponent(IndLed).exists()).toBe(false);
    w.unmount();
  });

  it('渲染 header 插槽', () => {
    const w = mount(IndCard, {
      props: { title: '标题' },
      slots: {
        default: '内容',
        header: '<button class="custom-header">操作</button>',
      },
    });
    expect(w.find('.custom-header').exists()).toBe(true);
    expect(w.find('.custom-header').text()).toBe('操作');
    w.unmount();
  });
});
