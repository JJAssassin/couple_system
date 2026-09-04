// @vitest-environment jsdom
/**
 * Board 乐观更新视图接线测试。
 * 契约：发送乐观负 id 上墙（成功后 load 换真实数据，失败回滚且表单保留）；
 * 反应乐观 toggle（成功采纳服务端 reactions，失败回滚）；置顶/删除失败回滚。
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { NInput } from 'naive-ui';
import Board from './Index.vue';

const hoisted = vi.hoisted(() => {
  const requestError = vi.fn();
  const success = vi.fn();
  const api = {
    listBoard: vi.fn(),
    createBoard: vi.fn(),
    updateBoard: vi.fn(),
    deleteBoard: vi.fn(),
    pinBoard: vi.fn(),
    addReaction: vi.fn(),
  };
  return { requestError, success, api };
});

vi.mock('@/api/board', () => hoisted.api);
vi.mock('@/store/notifyStore', () => ({
  useNotifyStore: () => ({ requestError: hoisted.requestError, success: hoisted.success }),
}));
vi.mock('@/composables/useRealtime', () => ({
  useRealtime: () => ({ useModuleSync: vi.fn(), onSync: vi.fn() }),
  overlaySyncMap: { toServer: {}, toClient: {} },
}));
vi.mock('@/composables/useAnimation', () => ({ useStaggerEnter: vi.fn() }));
vi.mock('@/composables/useSyncSettle', () => ({ useSyncSettle: vi.fn() }));
vi.mock('@/composables/useHaptic', () => ({ hapticForAction: vi.fn() }));
vi.mock('@/utils/feedback', () => ({
  feedback: new Proxy({}, { get: () => vi.fn() }),
}));

const M = (id: number, content: string, extra: Record<string, unknown> = {}) => ({
  id, content, color: null, imageUrl: null, isPrivate: false, receiverUserId: null,
  createUserId: 1, createTime: '2026-09-01T06:00:00.000Z', pinned: false, isUnlocked: true,
  authorName: 'TA', reactions: {}, ...extra,
});
// 每次返回新克隆：乐观 apply 会原位改写，共享引用会污染回滚真值
const serverList = () => ({ items: [M(1, '早安'), M(2, '晚安')] });

describe('Board 乐观更新接线', () => {
  let w: ReturnType<typeof mount>;

  beforeEach(() => {
    setActivePinia(createPinia());
    hoisted.requestError.mockReset();
    hoisted.success.mockReset();
    for (const fn of Object.values(hoisted.api)) fn.mockReset();
    hoisted.api.listBoard.mockImplementation(async () => serverList());
    w = mount(Board, {
      attachTo: document.body,
      // n-input 由 main.ts 全局注册，测试需显式补注册，否则 render 抛错
      global: { components: { NInput } },
    });
  });
  afterEach(() => {
    w.unmount();
    document.body.innerHTML = '';
  });

  it('发送成功：乐观负 id 上墙后由 load 换回服务端真实数据', async () => {
    await flushPromises();
    hoisted.api.createBoard.mockResolvedValue({});
    (w.vm as any).draft = '今天想你了';

    await (w.vm as any).send();
    await flushPromises();

    expect(hoisted.api.createBoard).toHaveBeenCalledWith(
      expect.objectContaining({ content: '今天想你了', isPrivate: false }),
    );
    expect(hoisted.api.listBoard).toHaveBeenCalledTimes(2); // 挂载 + 成功后 load
    expect((w.vm as any).messages.some((m: { id: number }) => m.id < 0)).toBe(false);
    expect((w.vm as any).draft).toBe(''); // 成功后清空草稿
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('发送失败：占位回滚、草稿保留并弹可重试错误卡', async () => {
    await flushPromises();
    hoisted.api.createBoard.mockRejectedValue(new Error('boom'));
    (w.vm as any).draft = '今天想你了';

    await (w.vm as any).send();
    await flushPromises();

    expect(hoisted.api.listBoard).toHaveBeenCalledTimes(2); // 挂载 + 回滚
    expect((w.vm as any).messages.some((m: { id: number }) => m.id < 0)).toBe(false);
    expect((w.vm as any).messages.map((m: { id: number }) => m.id)).toEqual([1, 2]);
    expect((w.vm as any).draft).toBe('今天想你了'); // 失败不清空草稿
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('发送留言');
  });

  it('反应成功：本地 toggle 后采纳服务端权威 reactions', async () => {
    await flushPromises();
    hoisted.api.addReaction.mockResolvedValue({ reactions: { emoji_heart: [7] } });

    await (w.vm as any).toggleReaction((w.vm as any).messages[0], 'emoji_heart');
    await flushPromises();

    expect(hoisted.api.addReaction).toHaveBeenCalledWith(
      expect.objectContaining({ id: 1, emojiKey: 'emoji_heart' }),
    );
    expect((w.vm as any).messages[0].reactions).toEqual({ emoji_heart: [7] });
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('反应失败：回滚到服务端真值并弹可重试错误卡', async () => {
    await flushPromises();
    hoisted.api.addReaction.mockRejectedValue(new Error('boom'));

    await (w.vm as any).toggleReaction((w.vm as any).messages[0], 'emoji_heart');
    await flushPromises();

    expect(hoisted.api.listBoard).toHaveBeenCalledTimes(2); // 挂载 + 回滚
    expect((w.vm as any).messages[0].reactions).toEqual({});
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('反应');
  });

  it('置顶失败：pinned 回滚并弹可重试错误卡', async () => {
    await flushPromises();
    hoisted.api.pinBoard.mockRejectedValue(new Error('boom'));

    await (w.vm as any).onPin((w.vm as any).messages[0]);
    await flushPromises();

    expect(hoisted.api.pinBoard).toHaveBeenCalledWith({ id: 1 });
    expect(hoisted.api.listBoard).toHaveBeenCalledTimes(2); // 挂载 + 回滚
    expect((w.vm as any).messages[0].pinned).toBe(false);
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('置顶');
  });

  it('删除失败：留言由服务端真值恢复并弹可重试错误卡', async () => {
    await flushPromises();
    hoisted.api.deleteBoard.mockRejectedValue(new Error('boom'));

    await (w.vm as any).onDelete(1);
    await flushPromises();

    expect(hoisted.api.listBoard).toHaveBeenCalledTimes(2); // 挂载 + 回滚
    expect((w.vm as any).messages.some((m: { id: number }) => m.id === 1)).toBe(true);
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('删除留言');
  });
});
