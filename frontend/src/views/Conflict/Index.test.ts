// @vitest-environment jsdom
/**
 * Conflict 列表卡键盘可访问性测试：锁定「整卡可点即整卡可键盘操作」约定。
 * 覆盖：role=button / tabindex=0、Enter 打开详情、Space 打开详情且阻止默认滚动。
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import Conflict from './Index.vue';
import { getConflict } from '@/api/conflict';

vi.mock('@/api/conflict', () => ({
  listConflict: vi.fn().mockResolvedValue({
    items: [
      { id: 11, summary: '吵架A', conflictLevel: 1, occurTime: '2026-08-01T10:00:00.000Z', reconcileTime: null },
      { id: 12, summary: '吵架B', conflictLevel: 2, occurTime: '2026-08-02T10:00:00.000Z', reconcileTime: '2026-08-03T10:00:00.000Z' },
    ],
    total: 2,
  }),
  getConflict: vi.fn().mockResolvedValue({ id: 11, summary: '吵架A详情', conflictLevel: 1, occurTime: '2026-08-01T10:00:00.000Z', reconcileTime: null }),
  createConflict: vi.fn(),
  updateConflict: vi.fn(),
  deleteConflict: vi.fn(),
}));
// 有副作用 / 需要后端的 composables 全部桩掉，让测试只聚焦键盘行为
vi.mock('@/composables/useRealtime', () => ({ useRealtime: () => ({ onSync: vi.fn(), useModuleSync: vi.fn() }) }));
vi.mock('@/composables/useSyncSettle', () => ({ useSyncSettle: vi.fn() }));
vi.mock('@/composables/useAnimation', () => ({ useStaggerEnter: vi.fn() }));
vi.mock('@/composables/useDevice', () => ({ isMobile: () => false }));

describe('Conflict 列表卡键盘可访问性', () => {
  let w: ReturnType<typeof mount>;

  beforeEach(() => {
    setActivePinia(createPinia());
    w = mount(Conflict, { attachTo: document.body });
  });
  afterEach(() => {
    w.unmount();
    document.body.innerHTML = '';
  });

  it('每张复盘卡带 role=button 与 tabindex=0，可键盘聚焦', async () => {
    await flushPromises();
    const cards = w.findAll('.love-card');
    expect(cards.length).toBe(2);
    for (const c of cards) {
      expect(c.attributes('role')).toBe('button');
      expect(c.attributes('tabindex')).toBe('0');
    }
  });

  it('在卡片上按 Enter 打开详情（键盘可达）', async () => {
    await flushPromises();
    const card = w.find('.love-card');
    await card.trigger('keydown', { key: 'Enter' });
    await flushPromises();
    expect(getConflict).toHaveBeenCalledWith(11);
    expect((w.vm as any).showDetail).toBe(true);
  });

  it('在卡片上按 Space 打开详情且阻止默认滚动', async () => {
    await flushPromises();
    const card = w.find('.love-card');
    const ev = new window.KeyboardEvent('keydown', { key: ' ', bubbles: true, cancelable: true });
    card.element.dispatchEvent(ev);
    await flushPromises();
    expect(getConflict).toHaveBeenCalledWith(11);
    expect(ev.defaultPrevented).toBe(true);
    expect((w.vm as any).showDetail).toBe(true);
  });
});
