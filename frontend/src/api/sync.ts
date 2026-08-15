import api from '@/utils/request';
import type { ApiResult } from '@/types';

/** SignalR 握手：匿名连上 WebSocket 后，携带 JWT 上报 connectionId，后端登记并加入情侣组。*/
export async function authenticateSync(connectionId: string): Promise<void> {
  await api.post<ApiResult<unknown>>('/sync/authenticate', { connectionId });
}

/** 登出/断开时解绑（连接断开后端也会自动清理，此处为显式兜底）。*/
export async function deauthenticateSync(connectionId: string): Promise<void> {
  await api.post<ApiResult<unknown>>('/sync/deauthenticate', { connectionId }).catch(() => {});
}
