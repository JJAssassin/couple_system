import { describe, it, expect, vi, beforeEach } from 'vitest';

// 把 notifyStore 的 useNotifyStore 替换成只暴露 requestError 的桩，
// 以便断言「乐观失败时是否弹可重试错误卡」而不依赖真实 UI 通知层。
const hoisted = vi.hoisted(() => {
  const requestError = vi.fn();
  return { requestError };
});

vi.mock('@/store/notifyStore', () => ({
  useNotifyStore: () => ({ requestError: hoisted.requestError }),
}));

import { useOptimistic } from '@/composables/useOptimistic';

describe('useOptimistic', () => {
  beforeEach(() => {
    hoisted.requestError.mockReset();
  });

  it('api 成功：仅 apply 一次，不回滚、不弹错，返回 true', async () => {
    const apply = vi.fn();
    const api = vi.fn().mockResolvedValue(undefined);
    const load = vi.fn().mockResolvedValue(undefined);
    const { mutate } = useOptimistic(load);

    const ok = await mutate({ label: '测试', apply, api });

    expect(ok).toBe(true);
    expect(apply).toHaveBeenCalledTimes(1);
    expect(api).toHaveBeenCalledTimes(1);
    expect(load).not.toHaveBeenCalled();
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('api 失败：apply 后回滚 load + 弹含重试按钮的错误卡，返回 false', async () => {
    const apply = vi.fn();
    const api = vi.fn().mockRejectedValue(new Error('boom'));
    const load = vi.fn().mockResolvedValue(undefined);
    const { mutate } = useOptimistic(load);

    const ok = await mutate({ label: '删除愿望', apply, api });

    expect(ok).toBe(false);
    expect(apply).toHaveBeenCalledTimes(1);
    expect(load).toHaveBeenCalledTimes(1); // 回滚到服务端真值
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    const [msg, onRetry] = hoisted.requestError.mock.calls[0];
    expect(msg).toContain('删除愿望');
    expect(msg).toContain('恢复');
    expect(typeof onRetry).toBe('function');
  });

  it('失败 + 重试成功：再 load 一次且不再弹错', async () => {
    const apply = vi.fn();
    const api = vi.fn()
      .mockRejectedValueOnce(new Error('boom'))
      .mockResolvedValueOnce(undefined);
    const load = vi.fn().mockResolvedValue(undefined);
    const { mutate } = useOptimistic(load);

    await mutate({ label: '保存愿望', apply, api });
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    const onRetry = hoisted.requestError.mock.calls[0][1] as () => Promise<void>;
    await onRetry();

    expect(api).toHaveBeenCalledTimes(2);
    expect(load).toHaveBeenCalledTimes(2); // 回滚 + 重试成功刷新
    expect(hoisted.requestError).toHaveBeenCalledTimes(1); // 没再弹
  });

  it('失败 + 重试仍失败：再弹无重试错误卡', async () => {
    const apply = vi.fn();
    const api = vi.fn().mockRejectedValue(new Error('boom'));
    const load = vi.fn().mockResolvedValue(undefined);
    const { mutate } = useOptimistic(load);

    await mutate({ label: '添加愿望', apply, api });
    const onRetry = hoisted.requestError.mock.calls[0][1] as () => Promise<void>;
    await onRetry();

    expect(hoisted.requestError).toHaveBeenCalledTimes(2);
    expect(hoisted.requestError.mock.calls[1][0]).toContain('还是失败了');
    expect(hoisted.requestError.mock.calls[1][1]).toBeUndefined(); // 第二次无重试
    expect(load).toHaveBeenCalledTimes(1); // 仅首次回滚
  });
});
