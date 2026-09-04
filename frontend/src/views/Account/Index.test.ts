// @vitest-environment jsdom
/**
 * Account 乐观更新视图接线测试。
 * 契约：失败以 refresh() 拉回服务端真值（顺带重算汇总/预算/统计），
 * 保存失败弹窗保持打开便于重试，成功后占位行被服务端真实数据替换。
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import Account from './Index.vue';

const hoisted = vi.hoisted(() => {
  const requestError = vi.fn();
  const ac = {
    listAccount: vi.fn(),
    getAccount: vi.fn(),
    createAccount: vi.fn(),
    updateAccount: vi.fn(),
    deleteAccount: vi.fn(),
    accountSummary: vi.fn(),
    accountStatistics: vi.fn(),
    exportAccountCsv: vi.fn(),
    importAccountPreview: vi.fn(),
    importAccountCommit: vi.fn(),
  };
  const bg = {
    getMonthlyBudget: vi.fn(),
    getCurrentBudget: vi.fn(),
    listBudgets: vi.fn(),
    setBudget: vi.fn(),
    deleteBudget: vi.fn(),
  };
  return { requestError, ac, bg };
});

vi.mock('@/api/account', () => hoisted.ac);
vi.mock('@/api/budget', () => hoisted.bg);
vi.mock('@/store/notifyStore', () => ({
  useNotifyStore: () => ({ requestError: hoisted.requestError }),
}));
vi.mock('@/composables/useRealtime', () => ({
  useRealtime: () => ({ onSync: vi.fn(), useModuleSync: vi.fn() }),
  overlaySyncMap: { toServer: {}, toClient: {} },
}));
vi.mock('@/composables/useAnimation', () => ({ useStaggerEnter: vi.fn() }));
vi.mock('@/utils/feedback', () => ({
  feedback: { deleted: vi.fn(), created: vi.fn(), updated: vi.fn(), warn: vi.fn() },
}));

const R = (id: number, amount: number) => ({
  id, recordType: 2, category: '餐饮', amount, recordTime: '2026-09-01T06:00:00.000Z',
  remark: null, createUserId: 1, createTime: '2026-09-01T06:00:00.000Z',
});
const serverList = () => ({ items: [R(1, 12.5), R(2, 30)], total: 2 });
const summary = () => ({ income: 5000, expend: 42.5, balance: 4957.5 });
const stats = () => ({ year: 2026, month: 9, monthIncome: 5000, monthExpense: 42.5, trend: [] });
const budget = () => ({
  year: 2026, month: 9, income: 5000, expense: 42.5, totalBudget: 3000,
  remaining: 2957.5, isOverspent: false, categories: [],
});

describe('Account 乐观更新接线', () => {
  let w: ReturnType<typeof mount>;

  beforeEach(() => {
    setActivePinia(createPinia());
    hoisted.requestError.mockReset();
    for (const fn of Object.values(hoisted.ac)) fn.mockReset();
    for (const fn of Object.values(hoisted.bg)) fn.mockReset();
    // 每次返回新对象：乐观 apply 可能原位改写，共享引用会污染回滚真值
    hoisted.ac.listAccount.mockImplementation(async () => serverList());
    hoisted.ac.accountSummary.mockImplementation(async () => summary());
    hoisted.ac.accountStatistics.mockImplementation(async () => stats());
    hoisted.bg.getMonthlyBudget.mockImplementation(async () => budget());
    hoisted.bg.listBudgets.mockResolvedValue([]);
    w = mount(Account, {
      attachTo: document.body,
      global: { stubs: { ChartWrap: true, LiquidSlider: true, ExpensePoster: true } },
    });
  });
  afterEach(() => {
    w.unmount();
    document.body.innerHTML = '';
  });

  it('删除成功：本地立即移除，refresh 重拉列表与汇总', async () => {
    await flushPromises();
    expect(hoisted.ac.listAccount).toHaveBeenCalledTimes(1); // 挂载
    hoisted.ac.deleteAccount.mockResolvedValue(undefined);
    hoisted.ac.listAccount.mockImplementation(async () => ({ items: [R(2, 30)], total: 1 }));

    await (w.vm as any).remove(R(1, 12.5));
    await flushPromises();

    expect(hoisted.ac.deleteAccount).toHaveBeenCalledWith(1);
    expect(hoisted.ac.listAccount).toHaveBeenCalledTimes(2); // 挂载 + 成功后 refresh
    expect(hoisted.ac.accountSummary).toHaveBeenCalledTimes(2); // 汇总重算
    expect((w.vm as any).list.some((x: { id: number }) => x.id === 1)).toBe(false);
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('删除失败：refresh 回滚恢复记录并弹可重试错误卡', async () => {
    await flushPromises();
    hoisted.ac.deleteAccount.mockRejectedValue(new Error('boom'));

    await (w.vm as any).remove(R(1, 12.5));
    await flushPromises();

    expect(hoisted.ac.listAccount).toHaveBeenCalledTimes(2); // 挂载 + 回滚
    // 记录被服务端真值恢复
    expect((w.vm as any).list.some((x: { id: number }) => x.id === 1)).toBe(true);
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('删除记录');
    expect(typeof hoisted.requestError.mock.calls[0][1]).toBe('function');
  });

  it('记一笔失败：弹窗保持打开便于重试，占位行被回滚清除', async () => {
    await flushPromises();
    (w.vm as any).editing = null;
    (w.vm as any).form = { recordType: 2, category: '餐饮', amount: 20, time: Date.now(), remark: '' };
    (w.vm as any).showModal = true;
    hoisted.ac.createAccount.mockRejectedValue(new Error('boom'));

    await (w.vm as any).save();
    await flushPromises();

    // 失败即回滚：占位行（负 id）被 refresh 清掉；弹窗保持打开便于重试
    expect((w.vm as any).list.some((x: { id: number }) => x.id < 0)).toBe(false);
    expect((w.vm as any).list.length).toBe(2);
    expect((w.vm as any).showModal).toBe(true);
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('记一笔');
  });

  it('记一笔成功：占位行被服务端真实数据替换，弹窗关闭', async () => {
    await flushPromises();
    (w.vm as any).editing = null;
    (w.vm as any).form = { recordType: 2, category: '餐饮', amount: 20, time: Date.now(), remark: '' };
    (w.vm as any).showModal = true;
    hoisted.ac.createAccount.mockResolvedValue(undefined);
    hoisted.ac.listAccount.mockImplementation(async () => ({
      items: [R(9, 20), R(1, 12.5), R(2, 30)], total: 3,
    }));

    await (w.vm as any).save();
    await flushPromises();

    expect(hoisted.ac.createAccount).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError).not.toHaveBeenCalled();
    expect((w.vm as any).showModal).toBe(false);
    // 成功后 refresh：占位负 id 被服务端真实 id=9 替换
    expect((w.vm as any).list.some((x: { id: number }) => x.id === 9)).toBe(true);
    expect((w.vm as any).list.every((x: { id: number }) => x.id > 0)).toBe(true);
  });
});
