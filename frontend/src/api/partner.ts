import api from '@/utils/request';
import { useAuthStore } from '@/store/authStore';
import type { ApiResult, BindStatus, PartnerInfo, InviteResp, JoinResult, LoginResp } from '@/types';

export function getStatus() {
  return api.get('/partner/status');
}
export function createInvite() {
  return api.post('/partner/invite');
}

// 加入并绑定：后端已用最新 cid 重签令牌并随响应返回，必须落地到本地，
// 否则旧 token 的 cid 仍是旧值，全局隔离过滤器会挡掉共享数据（"绑定成功却空库"）。
export async function joinPartner(code: string): Promise<JoinResult> {
  const { data } = await api.post('/partner/join', { code });
  const r = (data as ApiResult<JoinResult>).data;
  const auth = useAuthStore();
  auth.setAccessToken(r.tokens.accessToken);
  return r;
}

// 解除绑定：后端重签 cid="" 的全新令牌并返回，必须落地，否则解绑后仍可能以旧 token 读到原情侣数据。
export async function unbindPartner(): Promise<LoginResp> {
  const { data } = await api.post('/partner/unbind');
  const r = (data as ApiResult<LoginResp>).data;
  const auth = useAuthStore();
  auth.setAccessToken(r.accessToken);
  return r;
}

export type { ApiResult, BindStatus, PartnerInfo, InviteResp, JoinResult, LoginResp };
