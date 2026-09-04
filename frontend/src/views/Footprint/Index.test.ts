// @vitest-environment jsdom
/**
 * Footprint 乐观更新视图接线测试。
 * 覆盖 useOptimistic 在视图层的三个关键契约：
 * 1. 计数 +1：本地立即生效，成功后 load() 拉服务端权威计数；
 * 2. 计数失败：回滚到服务端真值 + 弹含重试的错误卡；
 * 3. 删除的「延迟移除 vs 退场动画」竞态：
 *    - 成功：动画播完（320ms）后才本地移除；
 *    - 失败：cancelled 标志兜底，延迟回调不得误删（防假删除）。
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import Footprint from './Index.vue';

const hoisted = vi.hoisted(() => {
  const requestError = vi.fn();
  const api = {
    listFootprints: vi.fn(),
    incrementFootprint: vi.fn(),
    deleteFootprint: vi.fn(),
    createFootprint: vi.fn(),
    updateFootprint: vi.fn(),
  };
  return { requestError, api };
});

vi.mock('@/api/footprint', () => hoisted.api);
vi.mock('@/store/notifyStore', () => ({
  useNotifyStore: () => ({ requestError: hoisted.requestError }),
}));
vi.mock('@/composables/useRealtime', () => ({
  useRealtime: () => ({ useModuleSync: vi.fn() }),
  overlaySyncMap: { toServer: {}, toClient: {} },
}));
vi.mock('@/composables/useAnimation', () => ({ useStaggerEnter: vi.fn() }));
vi.mock('@/composables/useSyncSettle', () => ({ useSyncSettle: vi.fn() }));
vi.mock('@/utils/feedback', () => ({
  feedback: { deleted: vi.fn(), created: vi.fn(), updated: vi.fn(), warn: vi.fn() },
}));

const F1 = { id: 1, title: '一起看日出', emoji: '🌟', count: 3, targetCount: 5, createUserId: 1, createTime: '2026-08-01T06:00:00.000Z', description: null };
const F2 = { id: 2, title: '学会了冲咖啡', emoji: '☕', count: 1, targetCount: null, createUserId: 2, createTime: '2026-08-02T08:00:00.000Z', description: null };

describe('Footprint 乐观更新接线', () => {
  let w: ReturnType<typeof mount>;

  beforeEach(() => {
    vi.useFakeTimers();
    setActivePinia(createPinia());
    hoisted.requestError.mockReset();
    for (const fn of Object.values(hoisted.api)) fn.mockReset();
    // 每次调用返回克隆：视图的乐观 apply 会原位替换数组元素，
    // 共享引用会让「回滚重拉」拿到被改过的数据，不符合服务端语义
    hoisted.api.listFootprints.mockImplementation(async () => [{ ...F1 }, { ...F2 }]);
    w = mount(Footprint, { attachTo: document.body });
  });
  afterEach(() => {
    w.unmount();
    document.body.innerHTML = '';
    vi.useRealTimers();
  });

  it('计数 +1：本地立即生效，成功后 load 拉服务端权威值', async () => {
    await flushPromises();
    hoisted.api.incrementFootprint.mockResolvedValue(undefined);
    // 成功后服务端权威计数为 99，验证 load() 真的刷新了列表
    hoisted.api.listFootprints.mockResolvedValue([{ ...F1, count: 99 }, F2]);

    await (w.vm as any).onIncrement(F1);
    await flushPromises();

    expect(hoisted.api.incrementFootprint).toHaveBeenCalledWith(1);
    // 成功路径：以服务端真值收尾，而非本地 +1 的 4
    expect((w.vm as any).items[0].count).toBe(99);
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('计数失败：回滚到服务端真值并弹可重试错误卡', async () => {
    await flushPromises();
    hoisted.api.incrementFootprint.mockRejectedValue(new Error('boom'));

    await (w.vm as any).onIncrement(F1);
    await flushPromises();

    // 回滚：load() 重拉，计数回到服务端真值 3
    expect((w.vm as any).items[0].count).toBe(3);
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('更新足迹');
    expect(typeof hoisted.requestError.mock.calls[0][1]).toBe('function');
  });

  it('删除成功：收缩动画播完（320ms）后才本地移除', async () => {
    await flushPromises();
    hoisted.api.deleteFootprint.mockResolvedValue(undefined);

    const p = (w.vm as any).onDelete(F1);
    // 动画窗口期内：项还在（移除被延迟，动画有内容可播）
    await vi.advanceTimersByTimeAsync(100);
    expect((w.vm as any).items.some((x: { id: number }) => x.id === 1)).toBe(true);

    await p;
    await vi.advanceTimersByTimeAsync(300);
    expect((w.vm as any).items.some((x: { id: number }) => x.id === 1)).toBe(false);
    expect((w.vm as any).removingId).toBeNull();
  });

  it('删除失败：延迟移除被 cancelled 兜底，项不得被误删', async () => {
    await flushPromises();
    hoisted.api.deleteFootprint.mockRejectedValue(new Error('boom'));

    await (w.vm as any).onDelete(F1);
    await flushPromises();
    // 失败 → cancelled=true，即使播完动画窗口也不会移除
    await vi.advanceTimersByTimeAsync(400);
    expect((w.vm as any).items.some((x: { id: number }) => x.id === 1)).toBe(true);
    expect((w.vm as any).removingId).toBeNull();
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('删除足迹');
  });
});
