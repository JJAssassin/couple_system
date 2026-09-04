// @vitest-environment jsdom
/**
 * DatePlan 乐观更新视图接线测试。
 * 契约：新增乐观负 id 占位（成功后 refresh 校正，失败回滚）；完成约会原位改写；
 * 删除失败由 refresh 回滚到服务端真值并弹可重试错误卡。
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import DatePlan from './Index.vue';

const hoisted = vi.hoisted(() => {
  const requestError = vi.fn();
  const success = vi.fn();
  const api = {
    listDate: vi.fn(),
    dateStats: vi.fn(),
    createDate: vi.fn(),
    updateDate: vi.fn(),
    deleteDate: vi.fn(),
  };
  return { requestError, success, api };
});

vi.mock('@/api/dateplan', () => hoisted.api);
// naive-ui 的 useMessage 无 Provider 时返回 undefined，成功路径会解引用崩溃 → 部分 mock
vi.mock('naive-ui', async (importOriginal) => {
  const actual = await importOriginal<typeof import('naive-ui')>();
  return { ...actual, useMessage: () => ({ success: vi.fn() }) };
});
vi.mock('@/store/notifyStore', () => ({
  useNotifyStore: () => ({ requestError: hoisted.requestError, success: hoisted.success }),
}));
vi.mock('@/composables/useRealtime', () => ({
  useRealtime: () => ({ useModuleSync: vi.fn(), onSync: vi.fn() }),
}));
vi.mock('@/composables/useAnimation', () => ({ useStaggerEnter: vi.fn() }));
vi.mock('@/composables/useSyncSettle', () => ({ useSyncSettle: vi.fn() }));
vi.mock('@/utils/feedback', () => ({
  feedback: new Proxy({}, { get: () => vi.fn() }),
}));

const D = (id: number, isCompleted: boolean, extra: Record<string, unknown> = {}) => ({
  id, isCompleted, planTime: '2026-09-10T18:00:00.000Z', realTime: null,
  location: '海边餐厅', budget: 200, realCost: null, experienceScore: null,
  remark: '', createTime: '2026-09-01T06:00:00.000Z', ...extra,
});
// 每次返回新克隆：乐观 apply 会原位改写（如 isCompleted），共享引用会污染回滚真值
const serverItems = () => [
  D(1, false),
  D(2, true, { experienceScore: 5, realCost: 300, realTime: '2026-09-02T12:00:00.000Z' }),
  D(3, false, { planTime: '2026-09-01T10:00:00.000Z' }), // 已过期 → 待执行区置顶
];

describe('DatePlan 乐观更新接线', () => {
  let w: ReturnType<typeof mount>;

  beforeEach(() => {
    setActivePinia(createPinia());
    hoisted.requestError.mockReset();
    hoisted.success.mockReset();
    for (const fn of Object.values(hoisted.api)) fn.mockReset();
    hoisted.api.listDate.mockImplementation(async () => ({ items: serverItems(), total: 3 }));
    hoisted.api.dateStats.mockImplementation(async () => ({ totalDates: 1, avgScore: 5 }));
    // jsdom 无 matchMedia：LoveSheet 打开时会探测 prefers-reduced-motion
    (window as any).matchMedia = (query: string) => ({
      matches: false, media: query, onchange: null,
      addListener: vi.fn(), removeListener: vi.fn(),
      addEventListener: vi.fn(), removeEventListener: vi.fn(), dispatchEvent: () => false,
    });
    w = mount(DatePlan, { attachTo: document.body });
  });
  afterEach(() => {
    w.unmount();
    document.body.innerHTML = '';
  });

  it('挂载加载统计与列表，逾期置顶/已完成分区正确', async () => {
    await flushPromises();
    expect((w.vm as any).stats.totalDates).toBe(1);
    expect((w.vm as any).stats.avgScore).toBe(5);
    // 逾期(3) 排在普通(1) 之前
    expect((w.vm as any).pending.map((d: { id: number }) => d.id)).toEqual([3, 1]);
    expect((w.vm as any).history.map((d: { id: number }) => d.id)).toEqual([2]);
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('新增成功：乐观负 id 占位上屏且预算数字化回写', async () => {
    await flushPromises();
    hoisted.api.createDate.mockResolvedValue({});
    (w.vm as any).cform = { planTime: Date.now(), location: '电影院', budget: '120', remark: '' };

    await (w.vm as any).saveCreate();
    await flushPromises();

    expect(hoisted.api.createDate).toHaveBeenCalledWith(
      expect.objectContaining({ isCompleted: false, location: '电影院', budget: 120, planTime: expect.any(String) }),
    );
    expect((w.vm as any).list.some((d: { id: number }) => d.id < 0)).toBe(true); // 占位仍在（680ms 后才 refresh）
    expect((w.vm as any).created).toBe(true);
    expect((w.vm as any).saving).toBe(false);
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('新增失败：占位回滚并弹可重试错误卡', async () => {
    await flushPromises();
    hoisted.api.createDate.mockRejectedValue(new Error('boom'));
    (w.vm as any).cform = { planTime: Date.now(), location: '电影院', budget: '', remark: '' };

    await (w.vm as any).saveCreate();
    await flushPromises();

    expect(hoisted.api.listDate).toHaveBeenCalledTimes(2); // 挂载 + 回滚 refresh
    expect((w.vm as any).list.some((d: { id: number }) => d.id < 0)).toBe(false);
    expect((w.vm as any).created).toBe(false);
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('添加约会');
  });

  it('删除失败：约会由服务端真值恢复并弹可重试错误卡', async () => {
    await flushPromises();
    hoisted.api.deleteDate.mockRejectedValue(new Error('boom'));

    await (w.vm as any).remove((w.vm as any).list[0]); // 含 320ms 收缩动画等待
    await flushPromises();

    expect(hoisted.api.deleteDate).toHaveBeenCalledWith(1);
    expect(hoisted.api.listDate).toHaveBeenCalledTimes(2); // 挂载 + 回滚 refresh
    expect((w.vm as any).list.some((d: { id: number }) => d.id === 1)).toBe(true);
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('删除约会');
  });

  it('完成约会成功：isCompleted 原位置true且花费/评分随 payload 回写', async () => {
    await flushPromises();
    hoisted.api.updateDate.mockResolvedValue({});
    (w.vm as any).openComplete((w.vm as any).list[0]);
    (w.vm as any).completeForm = { realCost: '150', score: 4 };

    await (w.vm as any).saveComplete();
    await flushPromises();

    expect(hoisted.api.updateDate).toHaveBeenCalledWith(
      1,
      expect.objectContaining({ isCompleted: true, realCost: 150, experienceScore: 4 }),
    );
    expect((w.vm as any).list.find((d: { id: number }) => d.id === 1).isCompleted).toBe(true);
    expect((w.vm as any).completed).toBe(true);
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });
});
