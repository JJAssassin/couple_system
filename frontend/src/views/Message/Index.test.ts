// @vitest-environment jsdom
/**
 * Message 乐观更新视图接线测试。
 * 契约：全部已读/删除已读/批量删除走 useOptimistic(refresh)，失败回滚并弹可重试错误卡；
 * 打开消息即标记已读；反应乐观 toggle 成功采纳服务端 reactions。
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import Message from './Index.vue';

const hoisted = vi.hoisted(() => {
  const requestError = vi.fn();
  const success = vi.fn();
  const api = {
    listMessage: vi.fn(),
    unreadCount: vi.fn(),
    readMessage: vi.fn(),
    readAll: vi.fn(),
    deleteRead: vi.fn(),
    batchDeleteMessage: vi.fn(),
    addReaction: vi.fn(),
  };
  return { requestError, success, api };
});

vi.mock('@/api/message', () => hoisted.api);
vi.mock('gsap', () => ({ gsap: { fromTo: vi.fn() } }));
vi.mock('@/store/notifyStore', () => ({
  useNotifyStore: () => ({ requestError: hoisted.requestError, success: hoisted.success }),
}));
vi.mock('@/composables/useRealtime', () => ({
  useRealtime: () => ({ useModuleSync: vi.fn(), onSync: vi.fn() }),
}));
vi.mock('@/composables/useAnimation', () => ({ useStaggerEnter: vi.fn() }));
vi.mock('@/composables/useSyncSettle', () => ({ useSyncSettle: vi.fn() }));
vi.mock('@/composables/useHaptic', () => ({ hapticForAction: vi.fn() }));
vi.mock('@/utils/feedback', () => ({
  feedback: new Proxy({}, { get: () => vi.fn() }),
}));

const SM = (id: number, isRead: boolean, extra: Record<string, unknown> = {}) => ({
  id, title: `消息${id}`, content: '内容正文', messageType: 1, isRead,
  createTime: '2026-09-01T06:00:00.000Z', reactions: {}, ...extra,
});
// 每次返回新克隆：乐观 apply 会原位改写（isRead/reactions），共享引用会污染回滚真值
const serverList = () => ({ items: [SM(1, false), SM(2, true), SM(3, false)], total: 3 });

describe('Message 乐观更新接线', () => {
  let w: ReturnType<typeof mount>;

  beforeEach(() => {
    setActivePinia(createPinia());
    hoisted.requestError.mockReset();
    hoisted.success.mockReset();
    for (const fn of Object.values(hoisted.api)) fn.mockReset();
    hoisted.api.listMessage.mockImplementation(async () => serverList());
    hoisted.api.unreadCount.mockResolvedValue(2);
    w = mount(Message, { attachTo: document.body });
  });
  afterEach(() => {
    w.unmount();
    document.body.innerHTML = '';
  });

  it('挂载加载列表，未读/已读分组与未读计数正确', async () => {
    await flushPromises();
    expect((w.vm as any).list).toHaveLength(3);
    expect((w.vm as any).unreadList.map((m: { id: number }) => m.id)).toEqual([1, 3]);
    expect((w.vm as any).readList.map((m: { id: number }) => m.id)).toEqual([2]);
    expect((w.vm as any).unread).toBe(2);
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('全部已读成功：isRead 原位置true且未读计数归零', async () => {
    await flushPromises();
    hoisted.api.readAll.mockResolvedValue({});

    await (w.vm as any).markAllRead();
    await flushPromises();

    expect(hoisted.api.readAll).toHaveBeenCalledTimes(1);
    expect((w.vm as any).list.every((m: { isRead: boolean }) => m.isRead)).toBe(true);
    expect((w.vm as any).unread).toBe(0);
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('全部已读失败：回滚到服务端真值并弹可重试错误卡', async () => {
    await flushPromises();
    hoisted.api.readAll.mockRejectedValue(new Error('boom'));

    await (w.vm as any).markAllRead();
    await flushPromises();

    expect(hoisted.api.listMessage).toHaveBeenCalledTimes(2); // 挂载 + 回滚
    expect((w.vm as any).unreadList.map((m: { id: number }) => m.id)).toEqual([1, 3]);
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('全部已读');
  });

  it('删除已读成功：已读消息本地移除，未读不受影响', async () => {
    await flushPromises();
    hoisted.api.deleteRead.mockResolvedValue({});

    await (w.vm as any).deleteReadAll();
    await flushPromises();

    expect(hoisted.api.deleteRead).toHaveBeenCalledTimes(1);
    expect((w.vm as any).list.map((m: { id: number }) => m.id)).toEqual([1, 3]);
    expect((w.vm as any).unread).toBe(2);
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('选取模式批量删除成功：ids 原样回写并退出选取', async () => {
    await flushPromises();
    hoisted.api.batchDeleteMessage.mockResolvedValue({});
    (w.vm as any).toggleSelectMode();
    (w.vm as any).toggleSel(1);
    (w.vm as any).toggleSel(2);

    await (w.vm as any).deleteSelected();
    await flushPromises();

    expect(hoisted.api.batchDeleteMessage).toHaveBeenCalledWith([1, 2]);
    expect((w.vm as any).list.map((m: { id: number }) => m.id)).toEqual([3]);
    expect((w.vm as any).selectMode).toBe(false);
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('打开未读消息：readMessage 回写且未读计数同步递减', async () => {
    await flushPromises();
    hoisted.api.readMessage.mockResolvedValue({});
    const m = (w.vm as any).list[0];

    await (w.vm as any).open(m);
    await flushPromises();

    expect(hoisted.api.readMessage).toHaveBeenCalledWith(1);
    expect(m.isRead).toBe(true);
    expect((w.vm as any).unread).toBe(1);
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('反应成功采纳服务端 reactions，失败回滚并弹可重试错误卡', async () => {
    await flushPromises();
    hoisted.api.addReaction.mockResolvedValueOnce({ reactions: { emoji_star: [5] } });
    await (w.vm as any).toggleReaction((w.vm as any).list[0], 'emoji_star');
    await flushPromises();
    expect((w.vm as any).list[0].reactions).toEqual({ emoji_star: [5] });

    hoisted.api.addReaction.mockRejectedValueOnce(new Error('boom'));
    await (w.vm as any).toggleReaction((w.vm as any).list[1], 'emoji_heart');
    await flushPromises();
    expect(hoisted.api.listMessage).toHaveBeenCalledTimes(2); // 挂载 + 回滚
    expect((w.vm as any).list[1].reactions).toEqual({});
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('反应');
  });
});
