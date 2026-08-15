import api from '@/utils/request';
import type { ApiResult, BindStatus, PartnerInfo, InviteResp } from '@/types';

export function getStatus() {
  return api.get('/partner/status');
}
export function createInvite() {
  return api.post('/partner/invite');
}
export function joinPartner(code: string) {
  return api.post('/partner/join', { code });
}
export function unbindPartner() {
  return api.post('/partner/unbind');
}

export type { ApiResult, BindStatus, PartnerInfo, InviteResp };
