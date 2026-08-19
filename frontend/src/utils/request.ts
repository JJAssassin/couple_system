import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/store/authStore';
import { useNotifyStore } from '@/store/notifyStore';
import type { ApiResult, LoginResp } from '@/types';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE || '/api',
  timeout: 15000,
});

// 请求拦截：注入 AccessToken
api.interceptors.request.use((cfg: InternalAxiosRequestConfig) => {
  const at = useAuthStore().accessToken;
  if (at) cfg.headers.Authorization = `Bearer ${at}`;
  return cfg;
});

// 并发刷新锁：避免多个 401 同时刷新
let refreshing: Promise<string> | null = null;

api.interceptors.response.use(
  (res) => {
    const body = res.data as ApiResult<unknown>;
    if (!body.success) {
      useNotifyStore().error(body.msg || '请求失败');
      return Promise.reject(body);
    }
    return res;
  },
  async (err: AxiosError<ApiResult<unknown>>) => {
    const cfg = err.config!;
    if (err.response?.status === 401 && !cfg.headers['X-Retry']) {
      cfg.headers['X-Retry'] = '1';
      try {
        const newAt = await (refreshing ??= doRefresh());
        refreshing = null;
        useAuthStore().setAccessToken(newAt);
        return api(cfg); // 用新 token 重试
      } catch (refreshErr) {
        refreshing = null;
        const rerr = refreshErr as AxiosError;
        if (rerr?.response) {
          // 服务端明确拒绝刷新（refreshToken 失效/过期）→ 真登出
          useAuthStore().logout();
        } else {
          // 网络层失败（弱网/代理抖动/CF 边缘波动）：绝不登出，保留本地 token，
          // 避免移动端弱网下「点功能 → 刷新请求超时 → 被误踢回登录页」。
        }
        return Promise.reject(err);
      }
    }
    if (err.response?.status === 403) {
      useNotifyStore().error('无权访问该内容');
    } else if (!err.response) {
      // 网络层错误（后端没起 / 代理连不上 / 超时）—— 此前被静默 reject，表现为"点了没反应"
      useNotifyStore().error('网络异常：请确认后端服务已启动（dotnet run）');
    } else if (err.response.status >= 500) {
      // 5xx：后端业务异常，把服务端 msg 透出，便于排查
      const body = err.response.data as ApiResult<unknown> | undefined;
      useNotifyStore().error(body?.msg || '服务器开小差了，请稍后再试');
    }
    return Promise.reject(err);
  }
);

async function doRefresh(): Promise<string> {
  const rt = useAuthStore().refreshToken;
  const { data } = await axios.post(`${api.defaults.baseURL}/auth/refresh`, { refreshToken: rt });
  const payload = (data as ApiResult<LoginResp>).data;
  // 保存轮换后的新 refreshToken，否则后端轮换后旧 token 失效会反复 401 登出
  useAuthStore().setTokens(payload.accessToken, payload.refreshToken);
  return payload.accessToken;
}

export default api;
