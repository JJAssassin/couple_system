import { describe, it, expect, beforeEach, vi } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import axios, { AxiosError } from 'axios';
import api from '@/utils/request';
import { useAuthStore } from '@/store/authStore';
import { useNotifyStore, bindNotify } from '@/store/notifyStore';

// 用内存 adapter 模拟不同响应，验证 request 拦截器的三处分支：
// 1) 业务成功(success:true) -> 原样 resolve；2) 业务失败(success:false) -> reject；3) HTTP 403 -> reject。
function mockAdapter(body: unknown, status = 200) {
  api.defaults.adapter = (async (config: any) => ({
    data: body,
    status,
    statusText: status === 200 ? 'OK' : 'ERR',
    headers: {},
    config,
  })) as any;
}

beforeEach(() => {
  setActivePinia(createPinia());
  // node 环境无 localStorage，authStore 启动会读取；提供内存 shim
  const store: Record<string, string> = {};
  (globalThis as any).localStorage = {
    getItem: (k: string) => (k in store ? store[k] : null),
    setItem: (k: string, v: string) => { store[k] = v; },
    removeItem: (k: string) => { delete store[k]; },
  };
});

describe('request 拦截器', () => {
  it('业务成功时 resolve 并返回整体响应', async () => {
    mockAdapter({ code: 200, success: true, msg: 'ok', data: { hello: 'world' } });
    const res = await api.get('/test');
    expect((res.data as any).data.hello).toBe('world');
  });

  it('业务失败(success:false)时 reject', async () => {
    mockAdapter({ code: 400, success: false, msg: '参数错误', data: null });
    await expect(api.get('/test')).rejects.toBeTruthy();
  });

  it('HTTP 403 时 reject', async () => {
    mockAdapter({}, 403);
    await expect(api.get('/test')).rejects.toBeTruthy();
  });

  it('网络层错误弹出重试提示，点击重试重放原请求', async () => {
    useAuthStore().setAccessToken('fake-token');
    bindNotify(
      { success: vi.fn(), error: vi.fn(), info: vi.fn() } as any,
      { create: vi.fn(() => ({ destroy: vi.fn() })) } as any
    );
    let calls = 0;
    // 第一次抛网络错误（err.response 为 undefined），第二次（重放）成功
    api.defaults.adapter = (config: any) => {
      calls++;
      if (calls === 1) {
        const e = new AxiosError('network down', 'ECONNABORTED', config, {}, undefined);
        return Promise.reject(e);
      }
      return Promise.resolve({
        data: { success: true, data: 'ok' },
        status: 200,
        statusText: 'OK',
        headers: {},
        config,
      });
    };

    const reqErrSpy = vi.spyOn(useNotifyStore(), 'requestError');
    // 立即 catch，避免 rejected promise 在 await 间隙触发 unhandledRejection
    const p = api.get('/test').catch(() => {});
    await new Promise((r) => setTimeout(r, 20));

    expect(reqErrSpy).toHaveBeenCalledTimes(1);
    const onRetry = reqErrSpy.mock.calls[0][1];
    expect(typeof onRetry).toBe('function');

    // 模拟用户点「重试一下」按钮 → 重放原请求
    onRetry!();
    await new Promise((r) => setTimeout(r, 20));

    expect(calls).toBe(2);
    await p;
  });
});
