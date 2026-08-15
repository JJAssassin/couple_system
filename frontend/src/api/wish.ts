import api from '@/utils/request';
import type { ApiResult, PagedResult, WishDto, WishReq } from '@/types';

export interface WishCompleteReq {
  id: number;
  completeRemark?: string;
  completeImage?: string;
}

export async function listWish(params: { page?: number; pageSize?: number }) {
  const { data } = await api.get('/wish/list', { params });
  return (data as ApiResult<PagedResult<WishDto>>).data;
}

export async function getWish(id: number) {
  const { data } = await api.get(`/wish/${id}`);
  return (data as ApiResult<WishDto>).data;
}

export async function createWish(req: WishReq) {
  const { data } = await api.post('/wish/create', req);
  return (data as ApiResult<WishDto>).data;
}

export async function updateWish(id: number, req: WishReq) {
  const { data } = await api.put('/wish/update', req, { params: { id } });
  return (data as ApiResult<WishDto>).data;
}

export async function deleteWish(id: number) {
  const { data } = await api.delete('/wish/delete', { params: { id } });
  return (data as ApiResult<object>).data;
}

export async function claimWish(id: number) {
  const { data } = await api.put('/wish/claim', { id });
  return (data as ApiResult<WishDto>).data;
}

export async function completeWish(req: WishCompleteReq) {
  const { data } = await api.put('/wish/complete', req);
  return (data as ApiResult<WishDto>).data;
}
