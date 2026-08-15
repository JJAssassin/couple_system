import api from '@/utils/request';
import type { ApiResult, PagedResult, DateRecordDto, DateRecordReq } from '@/types';

export interface DateStats {
  totalDates: number;
  avgScore: number;
}

export function listDate(params: { page?: number; pageSize?: number }) {
  return api.get('/daterecord/list', { params }).then((r) => (r.data as ApiResult<PagedResult<DateRecordDto>>).data);
}
export function getDate(id: number) {
  return api.get(`/daterecord/${id}`).then((r) => (r.data as ApiResult<DateRecordDto>).data);
}
export function createDate(req: DateRecordReq) {
  return api.post('/daterecord/create', req).then((r) => (r.data as ApiResult<DateRecordDto>).data);
}
export function updateDate(id: number, req: DateRecordReq) {
  return api.put(`/daterecord/update?id=${id}`, req).then((r) => (r.data as ApiResult<DateRecordDto>).data);
}
export function deleteDate(id: number) {
  return api.delete(`/daterecord/delete?id=${id}`).then((r) => (r.data as ApiResult<unknown>).data);
}
export function dateStats() {
  return api.get('/daterecord/stats').then((r) => (r.data as ApiResult<DateStats>).data);
}
