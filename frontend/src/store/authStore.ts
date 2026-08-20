import { defineStore } from 'pinia';
import { ref } from 'vue';
import axios from 'axios';
import api from '@/utils/request';
import type { ApiResult, LoginResp, UserProfile } from '@/types';

const LS_AT = 'cl_at';
const LS_RT = 'cl_rt';

// 存储读写统一容错 + 三层持久化兜底（localStorage → sessionStorage → cookie）：
// vivo 等国产浏览器会清理 localStorage（省电/清理/无痕），页面重载后 token 读不到
// 就会被路由守卫无提示踢回登录页。这里把 token 同时镜像到 sessionStorage 与 cookie
// （浏览器最基础的持久化机制），初始化时按 本地 → 会话 → cookie 顺序回退读取。
function cookieSet(key: string, value: string) {
  try {
    if (typeof document === 'undefined') return;
    document.cookie = `${key}=${encodeURIComponent(value)}; path=/; max-age=${7 * 24 * 3600}; SameSite=Lax`;
  } catch {
    /* 忽略 */
  }
}
function cookieGet(key: string): string {
  try {
    if (typeof document === 'undefined') return '';
    const m = document.cookie.match(new RegExp('(?:^|; )' + key + '=([^;]*)'));
    return m ? decodeURIComponent(m[1]) : '';
  } catch {
    return '';
  }
}
function cookieRemove(key: string) {
  try {
    if (typeof document === 'undefined') return;
    document.cookie = `${key}=; path=/; max-age=0`;
  } catch {
    /* 忽略 */
  }
}
function safeSet(key: string, value: string) {
  try {
    localStorage.setItem(key, value);
  } catch {
    /* 存储被禁时忽略 */
  }
  try {
    sessionStorage.setItem('m_' + key, value);
  } catch {
    /* 忽略 */
  }
  cookieSet(key, value);
}
function safeGet(key: string): string {
  try {
    const v = localStorage.getItem(key);
    if (v != null) return v;
  } catch {
    /* 忽略 */
  }
  try {
    const s = sessionStorage.getItem('m_' + key);
    if (s != null) return s;
  } catch {
    /* 忽略 */
  }
  return cookieGet(key);
}
function safeRemove(key: string) {
  try {
    localStorage.removeItem(key);
  } catch {
    /* 忽略 */
  }
  try {
    sessionStorage.removeItem('m_' + key);
  } catch {
    /* 忽略 */
  }
  cookieRemove(key);
}

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref(safeGet(LS_AT));
  const refreshToken = ref(safeGet(LS_RT));
  const profile = ref<UserProfile | null>(null);

  function setTokens(at: string, rt: string) {
    accessToken.value = at;
    refreshToken.value = rt;
    safeSet(LS_AT, at);
    safeSet(LS_RT, rt);
  }
  function setAccessToken(at: string) {
    accessToken.value = at;
    safeSet(LS_AT, at);
  }

  async function login(userName: string, password: string) {
    const { data } = await api.post('/auth/login', { userName, password });
    const r = (data as { data: LoginResp }).data;
    setTokens(r.accessToken, r.refreshToken);
    profile.value = r.userProfile;
  }

  async function logout() {
    try {
      await api.post('/auth/logout', { refreshToken: refreshToken.value });
    } catch {
      /* 忽略错误，强制本地登出 */
    }
    accessToken.value = '';
    refreshToken.value = '';
    profile.value = null;
    safeRemove(LS_AT);
    safeRemove(LS_RT);
  }

  /**
   * 静默续期：用持久化的 refreshToken 重建 accessToken。
   * 用于「内存令牌在 WebView 重载后丢失、但 refreshToken 仍在」的场景（iOS 原生 App 偶发），
   * 避免路由守卫把已登录用户误踢回登录框。用裸 axios 直连 /auth/refresh，绕过请求拦截器，
   * 以免与拦截器内的刷新逻辑互相递归。
   */
  async function restoreSession() {
    const rt = refreshToken.value;
    if (!rt) return;
    const { data } = await axios.post(
      `${import.meta.env.VITE_API_BASE || '/api'}/auth/refresh`,
      { refreshToken: rt },
    );
    const r = (data as ApiResult<LoginResp>).data;
    setTokens(r.accessToken, r.refreshToken);
  }

  return { accessToken, refreshToken, profile, setTokens, setAccessToken, login, logout, restoreSession };
});
