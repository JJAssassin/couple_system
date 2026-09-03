import { describe, it, expect, vi, beforeEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { useNotifyStore, bindNotify } from '@/store/notifyStore';

function bindMock(createMock: ReturnType<typeof vi.fn>) {
  bindNotify(
    { success: vi.fn(), error: vi.fn(), info: vi.fn() } as any,
    { create: createMock } as any
  );
}

describe('notifyStore.requestError', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it('create 被以正确参数调用，且可重试时含 action（重试按钮）', () => {
    const mockCreate = vi.fn(() => ({ destroy: vi.fn() }));
    bindMock(mockCreate);
    const onRetry = vi.fn();
    useNotifyStore().requestError('服务器开了个小差，重试试试？', onRetry);

    expect(mockCreate).toHaveBeenCalledTimes(1);
    const opt = mockCreate.mock.calls[0][0];
    expect(opt.title).toBe('出了点小状况');
    expect(opt.content).toBe('服务器开了个小差，重试试试？');
    expect(opt.type).toBe('error');
    expect(typeof opt.action).toBe('function');
  });

  it('无 onRetry 时不渲染 action', () => {
    const mockCreate = vi.fn(() => ({ destroy: vi.fn() }));
    bindMock(mockCreate);
    useNotifyStore().requestError('无权访问该内容');
    const opt = mockCreate.mock.calls[0][0];
    expect(opt.action).toBeUndefined();
  });

  it('重复调用时先销毁旧通知再建新', () => {
    const destroyA = vi.fn();
    const destroyB = vi.fn();
    const mockCreate = vi.fn((_opt: any) => ({
      destroy: mockCreate.mock.calls.length === 1 ? destroyA : destroyB,
    }));
    bindMock(mockCreate);
    const n = useNotifyStore();
    n.requestError('第一次', () => {});
    n.requestError('第二次', () => {});
    expect(mockCreate).toHaveBeenCalledTimes(2);
    expect(destroyA).toHaveBeenCalledTimes(1);
  });
});
