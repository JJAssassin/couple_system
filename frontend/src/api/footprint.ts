import api from '@/utils/request';
import type { ApiResult, FootprintDto, FootprintReq } from '@/types';

export async function listFootprints() {
  const { data } = await api.get('/footprint/list');
  return (data as ApiResult<FootprintDto[]>).data;
}

export async function createFootprint(req: FootprintReq) {
  const { data } = await api.post('/footprint/create', req);
  return (data as ApiResult<FootprintDto>).data;
}

export async function deleteFootprint(id: number) {
  const { data } = await api.delete('/footprint/delete', { params: { id } });
  return (data as ApiResult<object>).data;
}

export async function incrementFootprint(id: number) {
  const { data } = await api.put(`/footprint/increment/${id}`);
  return (data as ApiResult<FootprintDto>).data;
}

export async function updateFootprint(id: number, req: FootprintReq) {
  const { data } = await api.put(`/footprint/update/${id}`, req);
  return (data as ApiResult<FootprintDto>).data;
}
