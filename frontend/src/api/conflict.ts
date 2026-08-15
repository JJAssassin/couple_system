import api from '@/utils/request';
import type { ApiResult, PagedResult, ConflictDto, ConflictReq } from '@/types';

export async function listConflict(params: { page?: number; pageSize?: number }) {
  const { data } = await api.get('/conflict/list', { params });
  return (data as ApiResult<PagedResult<ConflictDto>>).data;
}

export async function getConflict(id: number) {
  const { data } = await api.get(`/conflict/${id}`);
  return (data as ApiResult<ConflictDto>).data;
}

export async function createConflict(req: ConflictReq) {
  const { data } = await api.post('/conflict/create', req);
  return (data as ApiResult<ConflictDto>).data;
}

export async function updateConflict(id: number, req: ConflictReq) {
  const { data } = await api.put('/conflict/update', req, { params: { id } });
  return (data as ApiResult<ConflictDto>).data;
}

export async function deleteConflict(id: number) {
  const { data } = await api.delete('/conflict/delete', { params: { id } });
  return (data as ApiResult<object>).data;
}
