import { defineStore } from 'pinia';
import { ref } from 'vue';
import api from '@/utils/request';
import type { LoginResp, UserProfile } from '@/types';

const LS_AT = 'cl_at';
const LS_RT = 'cl_rt';

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref(localStorage.getItem(LS_AT) || '');
  const refreshToken = ref(localStorage.getItem(LS_RT) || '');
  const profile = ref<UserProfile | null>(null);

  function setTokens(at: string, rt: string) {
    accessToken.value = at;
    refreshToken.value = rt;
    localStorage.setItem(LS_AT, at);
    localStorage.setItem(LS_RT, rt);
  }
  function setAccessToken(at: string) {
    accessToken.value = at;
    localStorage.setItem(LS_AT, at);
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
    localStorage.removeItem(LS_AT);
    localStorage.removeItem(LS_RT);
  }

  return { accessToken, refreshToken, profile, setTokens, setAccessToken, login, logout };
});
