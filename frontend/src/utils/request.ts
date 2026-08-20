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
    // 二进制下载（如 CSV/文件导出）不经过 ApiResult 包装，直接放行
    if (res.config.responseType === 'blob') return res;
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
      // 网络层错误（后端没起 / 代理连不上 / 超时 / 弱网 / 离线）：
      // ① 幂等读接口先尝试 Service Worker 的 API 离线缓存（方向④ 弱网降级）；
      //    命中则静默返回缓存数据，不打扰用户；② 无缓存才提示网络异常。
      const method = (cfg.method || 'get').toLowerCase();
      if (method === 'get' && typeof caches !== 'undefined') {
        const cached = await readApiCache(cfg.url || '');
        if (cached) return cached;
      }
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

/* ---------- 离线 API 缓存兜底（方向④，与 sw.js 的 key 逻辑保持一致） ---------- */

/** 用户指纹：与 sw.js userKey 相同的 djb2，保证能命中 SW 写入的缓存条目 */
function offlineUserKey(token: string): string {
  let h = 5381;
  for (let i = 0; i < token.length; i++) h = ((h << 5) + h + token.charCodeAt(i)) >>> 0;
  return h.toString(36);
}

/** 缓存条目 URL（与 sw.js apiKeyUrl 一致）：原 URL + 用户指纹参数 */
function offlineKeyUrl(url: string, token: string): string {
  const sep = url.includes('?') ? '&' : '?';
  return url + sep + '__u=' + offlineUserKey(token);
}

/** 网络失败时从 SW 的 pw-api-v1 缓存读取该用户的数据副本；返回 axios 兼容的响应结构，未命中返回 null */
async function readApiCache(url: string): Promise<{ data: unknown; status: number } | null> {
  try {
    const token = useAuthStore().accessToken;
    if (!token) return null;
    const cache = await caches.open('pw-api-v1');
    const res = await cache.match(offlineKeyUrl(url, token));
    if (!res) return null;
    const data = await res.json();
    return { data, status: 200 };
  } catch {
    return null;
  }
}

export default api;
