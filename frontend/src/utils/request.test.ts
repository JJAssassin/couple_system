import { describe, it, expect, beforeEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import api from '@/utils/request';

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
});
