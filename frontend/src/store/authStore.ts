import { defineStore } from 'pinia';
import { ref } from 'vue';
import axios from 'axios';
import api from '@/utils/request';
import { getApiBase } from '@/config/server';
import type { ApiResult, LoginResp, UserProfile } from '@/types';

// 安全说明（评审 #2）：
// - accessToken 仅存于内存（ref），不落 localStorage/sessionStorage/cookie，降低 XSS 持久化窃取面；
//   内存令牌仅在页面存活期内有效，刷新页面后由 /auth/refresh（HttpOnly Cookie cl_rt 自动携带）重建。
// - refreshToken 完全由后端写入 HttpOnly Cookie cl_rt，前端 JS 不可读、不可写，杜绝 XSS 窃取长生命周期凭据。
// - 仅把非敏感的用户资料缓存到 localStorage，便于刷新后即时渲染头像/昵称，不触及任何令牌。
const LS_PROFILE = 'cl_profile';

function safeProfileGet(): UserProfile | null {
  try {
    const v = localStorage.getItem(LS_PROFILE);
    return v ? (JSON.parse(v) as UserProfile) : null;
  } catch {
    return null;
  }
}
function safeProfileSet(p: UserProfile | null) {
  try {
    if (p) localStorage.setItem(LS_PROFILE, JSON.stringify(p));
    else localStorage.removeItem(LS_PROFILE);
  } catch {
    /* 忽略 */
  }
}

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string>('');
  const profile = ref<UserProfile | null>(safeProfileGet());

  function setAccessToken(at: string) {
    accessToken.value = at;
  }
  function setSession(at: string, p: UserProfile) {
    accessToken.value = at;
    profile.value = p;
    safeProfileSet(p);
  }

  async function login(userName: string, password: string) {
    const { data } = await api.post('/auth/login', { userName, password });
    const r = (data as { data: LoginResp }).data;
    setSession(r.accessToken, r.userProfile);
  }

  async function logout() {
    try {
      // 后端据 HttpOnly Cookie cl_rt 清除刷新令牌，前端无需传递任何令牌
      await api.post('/auth/logout');
    } catch {
      /* 忽略错误，强制本地登出 */
    }
    accessToken.value = '';
    profile.value = null;
    safeProfileSet(null);
  }

  /**
   * 静默续期：浏览器自动携带 HttpOnly Cookie cl_rt 调 /auth/refresh，
   * 后端轮换并返回新 accessToken（refresh 仍只在 Cookie 中，前端不接触）。
   * 注意：必须显式 withCredentials:true —— 本调用走原始 axios（非带凭据的 api 实例），
   * 否则 APK 跨站（WebView 源 https://localhost ↔ 后端不同源）时 cl_rt Cookie 不会被发送，
   * 导致刷新失败、App 冷启动后用户被静默登出（与 request.ts 的 doRefresh 保持一致）。
   */
  async function restoreSession() {
    const { data } = await axios.post(
      `${getApiBase()}/auth/refresh`,
      {},
      { withCredentials: true },
    );
    const r = (data as ApiResult<LoginResp>).data;
    setAccessToken(r.accessToken);
    if (r.userProfile) {
      profile.value = r.userProfile;
      safeProfileSet(r.userProfile);
    }
  }

  return { accessToken, profile, setAccessToken, setSession, login, logout, restoreSession };
});
