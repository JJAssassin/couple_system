// @vitest-environment jsdom
/**
 * Anniversary 乐观更新视图接线测试。
 * 契约：编辑走乐观（失败回滚 + 弹窗保持可重试，成功后 load 拉服务端重算的
 * daysLeft/nextOccurrence）；删除保留「延迟移除 + cancelled 兜底」竞态修复；
 * 新增不做乐观占位（daysLeft 等由服务端计算，等回包再插入）。
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import Anniversary from './Index.vue';

const hoisted = vi.hoisted(() => {
  const requestError = vi.fn();
  const api = {
    listAnniversaries: vi.fn(),
    createAnniversary: vi.fn(),
    updateAnniversary: vi.fn(),
    deleteAnniversary: vi.fn(),
  };
  return { requestError, api };
});

vi.mock('@/api/anniversary', () => hoisted.api);
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

const A = (id: number, name: string, daysLeft: number) => ({
  id, name, anniversaryType: 1, targetDate: '2026-10-01', coverImage: null,
  remindDays: 3, daysLeft, isYearly: true, nextOccurrence: '2026-10-01',
  lunarDate: null, createUserId: 1, createTime: '2026-08-01T06:00:00.000Z',
});
const serverList = () => ({ items: [A(1, '领证纪念日', 28), A(2, '生日', 60)] });
const EDIT_FORM = { name: '领证纪念日改', anniversaryType: 1, dateTs: new Date('2026-11-01').getTime(), remindDays: 5, isYearly: true, coverImage: '' };

describe('Anniversary 乐观更新接线', () => {
  let w: ReturnType<typeof mount>;

  beforeEach(() => {
    vi.useFakeTimers();
    setActivePinia(createPinia());
    hoisted.requestError.mockReset();
    for (const fn of Object.values(hoisted.api)) fn.mockReset();
    hoisted.api.listAnniversaries.mockImplementation(async () => serverList());
    w = mount(Anniversary, {
      attachTo: document.body,
      // 弹窗的 Transition/Teleport 在 fake timers 冻结下卸载会触发 Vue 内部崩溃，直接 stub
      global: { stubs: { transition: true, teleport: true } },
    });
  });
  afterEach(async () => {
    // 带过渡的弹窗在 fake timers 挂起状态下直接 unmount 会触发 Vue 内部崩溃，先复位
    try {
      (w.vm as any).showForm = false;
      await flushPromises();
    } catch { /* 已卸载则忽略 */ }
    w.unmount();
    document.body.innerHTML = '';
    vi.useRealTimers();
  });

  it('编辑成功：updateAnniversary 收到表单请求，load 拉回服务端重算值', async () => {
    await flushPromises();
    hoisted.api.updateAnniversary.mockResolvedValue(undefined);
    hoisted.api.listAnniversaries.mockImplementation(async () => ({
      items: [A(1, '领证纪念日改', 59), A(2, '生日', 60)],
    }));
    (w.vm as any).editingId = 1;
    (w.vm as any).form = { ...EDIT_FORM };

    await (w.vm as any).submit();
    await flushPromises();

    const [id, req] = hoisted.api.updateAnniversary.mock.calls[0];
    expect(id).toBe(1);
    expect(req.name).toBe('领证纪念日改');
    expect(req.targetDate).toBe('2026-11-01');
    // 成功后 load：daysLeft 以服务端重算值为准（本地乐观合并不含 daysLeft）
    expect((w.vm as any).items[0].daysLeft).toBe(59);
    expect((w.vm as any).items[0].name).toBe('领证纪念日改');
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('编辑失败：回滚到服务端真值并弹可重试错误卡', async () => {
    await flushPromises();
    hoisted.api.updateAnniversary.mockRejectedValue(new Error('boom'));
    (w.vm as any).editingId = 1;
    (w.vm as any).form = { ...EDIT_FORM };

    await (w.vm as any).submit();
    await flushPromises();

    expect(hoisted.api.listAnniversaries).toHaveBeenCalledTimes(2); // 挂载 + 回滚
    expect((w.vm as any).items[0].name).toBe('领证纪念日'); // 服务端真值
    // 契约：失败保持弹窗打开（showForm 不被关闭）便于重试；此处不开真弹窗，
    // 只断言失败路径不会触发「保存成功后延时收起表单」的收尾
    expect((w.vm as any).saved).toBe(false);
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('保存纪念日');
  });

  it('新增：不做乐观占位，等 createAnniversary 回包再插入', async () => {
    await flushPromises();
    const created = A(9, '旅行出发日', 10);
    hoisted.api.createAnniversary.mockResolvedValue(created);
    (w.vm as any).editingId = null;
    (w.vm as any).form = { ...EDIT_FORM, name: '旅行出发日' };

    await (w.vm as any).submit();
    await flushPromises();

    expect(hoisted.api.createAnniversary).toHaveBeenCalledTimes(1);
    // 直接插入服务端回包（含真实 id 与 daysLeft），无占位行
    expect((w.vm as any).items.some((x: { id: number }) => x.id === 9 && x.daysLeft === 10)).toBe(true);
    expect((w.vm as any).items.every((x: { id: number }) => x.id > 0)).toBe(true);
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('删除失败：延迟移除被 cancelled 兜底，项不得被误删', async () => {
    await flushPromises();
    hoisted.api.deleteAnniversary.mockRejectedValue(new Error('boom'));

    await (w.vm as any).onDelete(A(1, '领证纪念日', 28));
    await flushPromises();
    await vi.advanceTimersByTimeAsync(400);

    expect((w.vm as any).items.some((x: { id: number }) => x.id === 1)).toBe(true);
    expect((w.vm as any).poppingId).toBeNull();
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('删除纪念日');
  });

  it('删除成功：pop 动画播完（300ms）后才本地移除', async () => {
    await flushPromises();
    hoisted.api.deleteAnniversary.mockResolvedValue(undefined);

    const p = (w.vm as any).onDelete(A(1, '领证纪念日', 28));
    await vi.advanceTimersByTimeAsync(100);
    expect((w.vm as any).items.some((x: { id: number }) => x.id === 1)).toBe(true);

    await p;
    await vi.advanceTimersByTimeAsync(300);
    expect((w.vm as any).items.some((x: { id: number }) => x.id === 1)).toBe(false);
    expect((w.vm as any).poppingId).toBeNull();
  });
});
