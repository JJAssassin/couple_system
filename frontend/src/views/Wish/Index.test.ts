// @vitest-environment jsdom
/**
 * Wish 乐观更新视图接线测试。
 * 契约：拖拽排序仅持久化（失败经 load() 回滚到服务端顺序并可重试）；
 * 认领乐观置 claimUserId=-1（成功后 load 补真实认领人）；删除失败回滚。
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { NSelect } from 'naive-ui';
import Wish from './Index.vue';

const hoisted = vi.hoisted(() => {
  const requestError = vi.fn();
  const success = vi.fn();
  const api = {
    listWish: vi.fn(),
    createWish: vi.fn(),
    updateWish: vi.fn(),
    deleteWish: vi.fn(),
    claimWish: vi.fn(),
    completeWish: vi.fn(),
    reorderWishes: vi.fn(),
  };
  return { requestError, success, api };
});

vi.mock('@/api/wish', () => hoisted.api);
vi.mock('@/store/notifyStore', () => ({
  useNotifyStore: () => ({ requestError: hoisted.requestError, success: hoisted.success }),
}));
vi.mock('@/composables/useRealtime', () => ({
  useRealtime: () => ({ useModuleSync: vi.fn(), onSync: vi.fn() }),
  overlaySyncMap: { toServer: {}, toClient: {} },
}));
vi.mock('@/composables/useAnimation', () => ({ useStaggerEnter: vi.fn() }));
vi.mock('@/composables/useSyncSettle', () => ({ useSyncSettle: vi.fn() }));
vi.mock('@/utils/feedback', () => ({
  feedback: { deleted: vi.fn(), created: vi.fn(), updated: vi.fn(), warn: vi.fn(), saved: vi.fn() },
}));

const W = (id: number, title: string, extra: Record<string, unknown> = {}) => ({
  id, title, wishType: 1, description: null, expectTime: null, priority: 1,
  status: 1, claimUserId: null, claimUserName: null, completeTime: null,
  completeRemark: null, completeImage: null, createUserId: 1,
  createTime: '2026-09-01T06:00:00.000Z', ...extra,
});
const serverList = () => ({ items: [W(1, '去看海'), W(2, '学吉他'), W(3, '一起做饭')], total: 3 });

describe('Wish 乐观更新接线', () => {
  let w: ReturnType<typeof mount>;

  beforeEach(() => {
    setActivePinia(createPinia());
    hoisted.requestError.mockReset();
    hoisted.success.mockReset();
    for (const fn of Object.values(hoisted.api)) fn.mockReset();
    // 每次调用返回新克隆：认领/编辑等乐观 apply 会原位改写对象，共享引用会污染回滚真值
    hoisted.api.listWish.mockImplementation(async () => serverList());
    w = mount(Wish, {
      attachTo: document.body,
      // 模板使用全局注册的 n-select（main.ts naive 全局安装），测试需显式补注册
      global: { components: { NSelect } },
    });
  });
  afterEach(() => {
    w.unmount();
    document.body.innerHTML = '';
  });

  it('拖拽排序成功：拖后顺序原样回写后端且保留本地顺序', async () => {
    await flushPromises();
    hoisted.api.reorderWishes.mockResolvedValue(undefined);
    (w.vm as any).wishDrag = [W(3, '一起做饭'), W(1, '去看海'), W(2, '学吉他')];

    await (w.vm as any).onWishReorder();
    await flushPromises();

    expect(hoisted.api.reorderWishes).toHaveBeenCalledWith([3, 1, 2]);
    expect((w.vm as any).wishDrag.map((x: { id: number }) => x.id)).toEqual([3, 1, 2]); // 成功不回弹
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('拖拽排序失败：load 回滚到服务端顺序并弹可重试错误卡', async () => {
    await flushPromises();
    hoisted.api.reorderWishes.mockRejectedValue(new Error('boom'));
    (w.vm as any).wishDrag = [W(3, '一起做饭'), W(1, '去看海'), W(2, '学吉他')];

    await (w.vm as any).onWishReorder();
    await flushPromises();

    expect(hoisted.api.listWish).toHaveBeenCalledTimes(2); // 挂载 + 回滚
    expect((w.vm as any).wishDrag.map((x: { id: number }) => x.id)).toEqual([1, 2, 3]);
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('排序');
  });

  it('认领成功：乐观置 -1 后由 load 补服务端真实认领人', async () => {
    await flushPromises();
    hoisted.api.claimWish.mockResolvedValue(undefined);
    hoisted.api.listWish.mockImplementation(async () => ({
      items: [W(1, '去看海', { claimUserId: 2, claimUserName: '对方' }), W(2, '学吉他'), W(3, '一起做饭')],
      total: 3,
    }));

    await (w.vm as any).onClaim(W(1, '去看海'));
    await flushPromises();

    expect(hoisted.api.claimWish).toHaveBeenCalledWith(1);
    expect(hoisted.success).toHaveBeenCalledWith('已认领');
    expect((w.vm as any).wishes.find((x: { id: number }) => x.id === 1).claimUserId).toBe(2);
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('认领失败：claimUserId 回滚为空并弹可重试错误卡', async () => {
    await flushPromises();
    hoisted.api.claimWish.mockRejectedValue(new Error('boom'));

    await (w.vm as any).onClaim(W(1, '去看海'));
    await flushPromises();

    expect((w.vm as any).wishes.find((x: { id: number }) => x.id === 1).claimUserId).toBeNull();
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('认领愿望');
  });

  it('删除失败：愿望被服务端真值恢复并弹可重试错误卡', async () => {
    await flushPromises();
    hoisted.api.deleteWish.mockRejectedValue(new Error('boom'));

    await (w.vm as any).onDelete(1);
    await flushPromises();

    expect(hoisted.api.listWish).toHaveBeenCalledTimes(2); // 挂载 + 回滚
    expect((w.vm as any).wishes.some((x: { id: number }) => x.id === 1)).toBe(true);
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('删除愿望');
  });
});
