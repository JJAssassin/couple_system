import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/store/authStore';
import { useNotifyStore } from '@/store/notifyStore';
import { useGlobalLoading } from '@/composables/useGlobalLoading';
import type { ApiResult, LoginResp } from '@/types';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE || '/api',
  timeout: 15000,
});

// 顶部加载条：仅在非刷新、非文件下载的请求上跟踪（其余请求自带骨架/按钮 loading）
function shouldTrack(cfg: InternalAxiosRequestConfig): boolean {
  const url = String(cfg.url || '').toLowerCase();
  if (url.includes('/auth/refresh')) return false;
  if (cfg.responseType === 'blob') return false;
  return true;
}

// 请求拦截：注入 AccessToken + 启动全局加载反馈
api.interceptors.request.use((cfg: InternalAxiosRequestConfig) => {
  const at = useAuthStore().accessToken;
  if (at) cfg.headers.Authorization = `Bearer ${at}`;
  if (shouldTrack(cfg)) useGlobalLoading().start();
  return cfg;
});

// 并发刷新锁：避免多个 401 同时刷新
let refreshing: Promise<string> | null = null;
// 刷新冷却：限流(429)或网络抖动时，短时间内不再重复打 /auth/refresh。
// 目的① 别把后端「刷新 5 次/分/IP」限流桶打满；目的② 杜绝「刷新被限流 → 登出 →
// 下一笔请求又刷新 → 又被限流」的自杀循环（见 2026-08-28 复盘）。
let lastRefreshAt = 0;
const REFRESH_COOLDOWN_MS = 15_000;

api.interceptors.response.use(
  (res) => {
    // 二进制下载（如 CSV/文件导出）不经过 ApiResult 包装，直接放行
    if (res.config.responseType === 'blob') return res;
    if (shouldTrack(res.config)) useGlobalLoading().end();
    const body = res.data as ApiResult<unknown>;
    if (!body.success) {
      useNotifyStore().error(body.msg || '请求失败');
      return Promise.reject(body);
    }
    return res;
  },
  async (err: AxiosError<ApiResult<unknown>>) => {
    const cfg = err.config!;
    if (cfg && shouldTrack(cfg)) useGlobalLoading().end();
    if (err.response?.status === 401 && !cfg.headers['X-Retry']) {
      cfg.headers['X-Retry'] = '1';
      const now = Date.now();
      // 冷却期内：跳过刷新，直接按失败处理（不登出、保留本地 token），
      // 等冷却后由下次请求再试，避免在限流窗口里高频重试把桶打满。
      if (now - lastRefreshAt < REFRESH_COOLDOWN_MS) {
        return Promise.reject(err);
      }
      lastRefreshAt = now;
      try {
        const newAt = await (refreshing ??= doRefresh());
        refreshing = null;
        useAuthStore().setAccessToken(newAt);
        return api(cfg); // 用新 token 重试
      } catch (refreshErr) {
        refreshing = null;
        const rerr = refreshErr as AxiosError;
        const status = rerr?.response?.status;
        // 仅当刷新端点「明确拒绝」令牌（401/400/403 失效/过期）才真登出；
        // 429 限流或网络层失败一律不登出——保留本地 token，等冷却后下次请求自动恢复，
        // 避免移动端弱网/被限流时「刷新失败 → 登出 → 又被踢回登录页」的循环。
        if (status === 401 || status === 400 || status === 403) {
          // 令牌失效/过期：优雅登出——明确提示用户并跳回登录页，
          // 避免「静默登出」让用户误以为系统故障（整改 #8 / WCAG 3.2 可预测性）
          useNotifyStore().error('登录已过期，请重新登录');
          useAuthStore().logout();
          // 动态 import 避免与 router 形成静态循环依赖；replace 不入历史栈，避免返回键回到登录前页
          void import('@/router')
            .then((m) => m.default.replace('/login'))
            .catch(() => {});
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
  // refresh 走 HttpOnly Cookie cl_rt，浏览器自动携带，前端不持有（评审 #2）
  const { data } = await axios.post(`${api.defaults.baseURL}/auth/refresh`, {});
  const payload = (data as ApiResult<LoginResp>).data;
  useAuthStore().setSession(payload.accessToken, payload.userProfile);
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
