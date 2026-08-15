import api from '@/utils/request';
import type { ApiResult, AnniversaryDto, AnniversaryReq } from '@/types';

export async function listAnniversaries(page = 1, pageSize = 50) {
  const { data } = await api.get('/anniversary/list', { params: { page, pageSize } });
  return (data as ApiResult<{ items: AnniversaryDto[]; total: number }>).data;
}

export async function createAnniversary(req: AnniversaryReq) {
  const { data } = await api.post('/anniversary/create', req);
  return (data as ApiResult<AnniversaryDto>).data;
}

export async function updateAnniversary(id: number, req: AnniversaryReq) {
  const { data } = await api.put('/anniversary/update', req, { params: { id } });
  return (data as ApiResult<AnniversaryDto>).data;
}

export async function deleteAnniversary(id: number) {
  const { data } = await api.delete('/anniversary/delete', { params: { id } });
  return (data as ApiResult<object>).data;
}
