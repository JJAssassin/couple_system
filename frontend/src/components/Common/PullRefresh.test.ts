// @vitest-environment jsdom
/**
 * PullRefresh 下拉刷新组件测试
 */
import { describe, it, expect, vi, afterEach } from 'vitest';
import { mount } from '@vue/test-utils';
import PullRefresh from './PullRefresh.vue';

/** 构造一个够用的 TouchEvent 替身（jsdom 无原生 TouchEvent 构造） */
function touch(target: Element, x: number, y: number) {
  return {
    touches: [{ clientX: x, clientY: y }],
    target,
    cancelable: false,
    preventDefault: () => {},
  } as unknown as TouchEvent;
}

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

  it('下拉超过阈值触发 refresh 事件并回传 done 回调', async () => {
    const w = mount(PullRefresh, {
      slots: { default: '<div class="body">内容</div>' },
    });
    const vm = w.vm as any;
    await w.trigger('touchstart', { touches: [{ clientX: 50, clientY: 100 }] });
    await w.trigger('touchmove', { touches: [{ clientX: 50, clientY: 260 }] }); // dy=160 → 阻尼后 delta=80
    await w.trigger('touchend');
    const emitted = w.emitted('refresh');
    expect(emitted).toBeTruthy();
    const done = (emitted as unknown[])[0][0] as () => void;
    expect(done).toBeTypeOf('function');
    expect(vm.refreshing).toBe(true);
    // 调用方完成后回调 done，收起指示器
    done();
    await w.vm.$nextTick();
    expect(vm.refreshing).toBe(false);
    expect(vm.delta).toBe(0);
    w.unmount();
  });

  it('横向滑动不触发下拉刷新（与胶片横滑手势共存）', async () => {
    const w = mount(PullRefresh, {
      slots: { default: '<div class="body">内容</div>' },
    });
    const vm = w.vm as any;
    await w.trigger('touchstart', { touches: [{ clientX: 50, clientY: 100 }] });
    await w.trigger('touchmove', { touches: [{ clientX: 160, clientY: 100 }] }); // 纯横向 dx=110
    await w.trigger('touchend');
    expect(w.emitted('refresh')).toBeFalsy();
    expect(vm.refreshing).toBe(false);
    expect(vm.delta).toBe(0);
    w.unmount();
  });

  it('调用方未在 WATCHDOG 内 done() 时自动收起，不永久卡住', async () => {
    vi.useFakeTimers();
    const w = mount(PullRefresh, {
      slots: { default: '<div class="body">内容</div>' },
    });
    const vm = w.vm as any;
    await w.trigger('touchstart', { touches: [{ clientX: 50, clientY: 100 }] });
    await w.trigger('touchmove', { touches: [{ clientX: 50, clientY: 260 }] });
    await w.trigger('touchend');
    expect(vm.refreshing).toBe(true);
    // 模拟调用方忘记回调 done
    vi.advanceTimersByTime(8000);
    await w.vm.$nextTick();
    expect(vm.refreshing).toBe(false);
    expect(w.emitted('refresh')).toBeTruthy();
    vi.useRealTimers();
    w.unmount();
  });
});
