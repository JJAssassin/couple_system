// @vitest-environment jsdom
/**
 * PullRefresh 下拉刷新组件测试
 */
import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import PullRefresh from './PullRefresh.vue';

describe('PullRefresh', () => {
  it('渲染默认状态：无刷新提示', () => {
    const w = mount(PullRefresh, {
      slots: { default: '<div class="body">内容</div>' },
    });
    expect(w.find('.pull-refresh__hint').classes()).not.toContain('show');
    expect(w.find('.body').exists()).toBe(true);
    w.unmount();
  });

  it('刷新中显示 spinner 和提示文案', async () => {
    const w = mount(PullRefresh, {
      slots: { default: '<div class="body">内容</div>' },
    });
    // 通过组件实例设置 refreshing 状态
    const vm = w.vm as any;
    vm.refreshing = true;
    vm.pulling = false;
    vm.delta = 64;
    await w.vm.$nextTick();
    expect(w.find('.pull-refresh__hint').classes()).toContain('show');
    expect(w.find('.pull-refresh__spinner').exists()).toBe(true);
    expect(w.text()).toContain('刷新中…');
    w.unmount();
  });

  it('调用 done() 后收起刷新指示器', async () => {
    const w = mount(PullRefresh, {
      slots: { default: '<div class="body">内容</div>' },
    });
    const vm = w.vm as any;
    vm.refreshing = true;
    vm.delta = 64;
    await w.vm.$nextTick();
    expect(w.find('.pull-refresh__hint').classes()).toContain('show');
    // 调用 done
    vm.done();
    await w.vm.$nextTick();
    expect(vm.refreshing).toBe(false);
    expect(vm.delta).toBe(0);
    w.unmount();
  });
});
