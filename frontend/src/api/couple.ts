import api from '@/utils/request';
import type { ApiResult, CoupleSetting } from '@/types';

export function getCoupleSetting() {
  return api.get('/couple/setting');
}
export function setLoveStart(loveStartTime: string) {
  return api.post('/couple/lovestart', { loveStartTime });
}
export function updateCoupleSetting(req: {
  coupleName?: string;
  coupleAvatar?: string;
  loveStartTime?: string;
}) {
  return api.put('/couple/setting', req);
}

export type { ApiResult, CoupleSetting };
