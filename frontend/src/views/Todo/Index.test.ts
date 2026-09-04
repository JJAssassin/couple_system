// @vitest-environment jsdom
/**
 * Todo 拖拽排序乐观接线测试。
 * 契约：拖拽即时改动本地顺序（乐观），onTodoReorder 仅负责持久化；
 * 成功后不动本地（保留拖后顺序），失败经 load() 回滚到服务端顺序并可重试。
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import { NSelect } from 'naive-ui';
import Todo from './Index.vue';

const hoisted = vi.hoisted(() => {
  const requestError = vi.fn();
  const api = {
    listTodo: vi.fn(),
    createTodo: vi.fn(),
    updateTodo: vi.fn(),
    deleteTodo: vi.fn(),
    toggleTodo: vi.fn(),
    assignTodo: vi.fn(),
    reorderTodos: vi.fn(),
  };
  return { requestError, api };
});

vi.mock('@/api/todo', () => hoisted.api);
vi.mock('@/store/notifyStore', () => ({
  useNotifyStore: () => ({ requestError: hoisted.requestError }),
}));
// partner.load() 会在挂载时发真请求（jsdom 下 ECONNREFUSED 且污染 requestError 调用计数），桩掉
vi.mock('@/store/partnerStore', () => ({
  usePartnerStore: () => ({ status: true, load: vi.fn(), partnerName: '对方' }),
}));
vi.mock('@/composables/useRealtime', () => ({
  useRealtime: () => ({ useModuleSync: vi.fn() }),
  overlaySyncMap: { toServer: {}, toClient: {} },
}));
vi.mock('@/composables/useAnimation', () => ({ useStaggerEnter: vi.fn() }));
vi.mock('@/composables/useSyncSettle', () => ({ useSyncSettle: vi.fn() }));
vi.mock('@/utils/feedback', () => ({
  feedback: { deleted: vi.fn(), created: vi.fn(), updated: vi.fn(), warn: vi.fn(), toggled: vi.fn() },
}));

const T = (id: number, title: string, sortOrder: number) => ({
  id, title, isDone: false, priority: 1, dueTime: null, doneTime: null,
  doneUserId: null, doneUserName: null, category: null, assigneeUserId: null,
  assigneeName: null, createUserId: 1, createTime: '2026-08-01T06:00:00.000Z',
  description: null, sortOrder,
});
const serverOrder = () => [T(1, 'A', 0), T(2, 'B', 1), T(3, 'C', 2)];

describe('Todo 拖拽排序乐观接线', () => {
  let w: ReturnType<typeof mount>;

  beforeEach(() => {
    setActivePinia(createPinia());
    hoisted.requestError.mockReset();
    for (const fn of Object.values(hoisted.api)) fn.mockReset();
    // Todo 的 load 是分页接口：listTodo({page,pageSize}) → { items }，且每次返回新克隆
    hoisted.api.listTodo.mockImplementation(async () => ({ items: serverOrder(), total: 3 }));
    w = mount(Todo, {
      attachTo: document.body,
      // 视图模板使用全局注册的 n-select（main.ts 里 naive-ui 全局安装），测试需显式补注册
      global: { components: { NSelect } },
    });
  });
  afterEach(() => {
    w.unmount();
    document.body.innerHTML = '';
  });

  it('成功路径：把拖拽后的本地顺序原样回写后端', async () => {
    await flushPromises();
    hoisted.api.reorderTodos.mockResolvedValue(undefined);
    // 模拟 vuedraggable 已即时改写本地数组：本地顺序变为 2,3,1
    (w.vm as any).activeDrag = [T(2, 'B', 1), T(3, 'C', 2), T(1, 'A', 0)];

    await (w.vm as any).onTodoReorder('active');
    await flushPromises();

    expect(hoisted.api.reorderTodos).toHaveBeenCalledWith([2, 3, 1]);
    // 成功路径不回弹、不弹错
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('失败路径：load 回滚到服务端顺序并弹可重试错误卡', async () => {
    await flushPromises();
    hoisted.api.reorderTodos.mockRejectedValue(new Error('boom'));
    (w.vm as any).activeDrag = [T(2, 'B', 1), T(3, 'C', 2), T(1, 'A', 0)];

    await (w.vm as any).onTodoReorder('active');
    await flushPromises();

    // 回滚：load 重拉服务端顺序 1,2,3，经 watch 链回写拖拽数组
    expect(hoisted.api.listTodo).toHaveBeenCalledTimes(2); // 挂载 + 回滚
    expect((w.vm as any).activeDrag.map((t: { id: number }) => t.id)).toEqual([1, 2, 3]);
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('排序');
    expect(typeof hoisted.requestError.mock.calls[0][1]).toBe('function');
  });

  it('少于 2 项的分组不发起排序请求', async () => {
    await flushPromises();
    (w.vm as any).activeDrag = [T(1, 'A', 0)];

    await (w.vm as any).onTodoReorder('active');

    expect(hoisted.api.reorderTodos).not.toHaveBeenCalled();
  });
});
