// @vitest-environment jsdom
/**
 * ErrorBoundary 路由级错误边界测试：
 * 验证子树渲染错误被捕获并展示友好错误页，而非冒泡成白屏；
 * 点击「重试」触发 retry 事件由外层强制重建路由组件。
 */
import { describe, it, expect } from 'vitest';
import { defineComponent, nextTick } from 'vue';
import { mount } from '@vue/test-utils';
import ErrorBoundary from './ErrorBoundary.vue';

// 渲染即抛错的子树
const Boom = defineComponent({
  name: 'Boom',
  render() {
    throw new Error('boom-render');
  },
});

// 正常子树
const Ok = defineComponent({
  name: 'Ok',
  template: '<div class="ok-child">safe</div>',
});

describe('ErrorBoundary', () => {
  it('渲染正常子树时不显示错误页', () => {
    const w = mount(ErrorBoundary, { slots: { default: Ok } });
    expect(w.find('.ok-child').exists()).toBe(true);
    expect(w.find('.err-boundary').exists()).toBe(false);
  });

  it('捕获子树渲染错误并显示友好错误页', async () => {
    const w = mount(ErrorBoundary, { slots: { default: Boom } });
    // onErrorCaptured 捕获后通过改 ref 触发 fallback 重渲染，需等一拍
    await nextTick();
    expect(w.find('.err-boundary').exists()).toBe(true);
    expect(w.find('.err-title').text()).toContain('差错');
    expect(w.find('.err-detail').text()).toContain('boom-render');
    // 出错后插槽内容不应再渲染
    expect(w.find('.ok-child').exists()).toBe(false);
  });

  it('点击重试触发 retry 事件', async () => {
    const w = mount(ErrorBoundary, { slots: { default: Boom } });
    await nextTick();
    await w.find('.err-btn').trigger('click');
    expect(w.emitted('retry')).toBeTruthy();
    expect((w.emitted('retry') as unknown[]).length).toBe(1);
  });
});
