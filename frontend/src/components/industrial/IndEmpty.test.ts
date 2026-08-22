// @vitest-environment jsdom
/**
 * IndEmpty 组件测试：验证空状态渲染、插槽、事件。
 */
import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import IndEmpty from './IndEmpty.vue';
import IndButton from '../industrial/IndButton.vue';

describe('IndEmpty', () => {
  it('渲染默认空状态', () => {
    const w = mount(IndEmpty);
    expect(w.text()).toContain('这里还是空的');
    expect(w.find('svg').exists()).toBe(true);
    w.unmount();
  });

  it('自定义标题和描述', () => {
    const w = mount(IndEmpty, {
      props: { title: '暂无数据', desc: '快去添加第一条记录吧' },
    });
    expect(w.text()).toContain('暂无数据');
    expect(w.text()).toContain('快去添加第一条记录吧');
    w.unmount();
  });

  it('隐藏装饰插画', () => {
    const w = mount(IndEmpty, { props: { showArt: false } });
    expect(w.find('svg').exists()).toBe(false);
    expect(w.find('.halo').exists()).toBe(false);
    w.unmount();
  });

  it('emoji 替代插画', () => {
    const w = mount(IndEmpty, { props: { emoji: '🎂' } });
    expect(w.find('.halo').text()).toBe('🎂');
    w.unmount();
  });

  it('action 按钮触发事件', async () => {
    const emit = vi.fn();
    const w = mount(IndEmpty, {
      props: { actionText: '去创建' },
      attrs: { onClick: emit },
    });
    await w.find('button').trigger('click');
    expect(emit).toHaveBeenCalled();
    w.unmount();
  });

  it('自定义 action 插槽', () => {
    const w = mount(IndEmpty, {
      props: { title: 'test' },
      slots: {
        action: '<button class="custom">自定义</button>',
      },
    });
    expect(w.find('.custom').exists()).toBe(true);
    expect(w.findComponent(IndButton).exists()).toBe(false);
    w.unmount();
  });
});
