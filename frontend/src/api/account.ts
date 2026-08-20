import api from '@/utils/request';
import type { ApiResult, PagedResult, AccountRecordDto, AccountRecordReq, AccountStatisticsDto } from '@/types';

export interface AccountSummary {
  income: number;
  expend: number;
  balance: number;
}

export function listAccount(params: { page?: number; pageSize?: number }) {
  return api.get('/account/list', { params }).then((r) => (r.data as ApiResult<PagedResult<AccountRecordDto>>).data);
}
export function getAccount(id: number) {
  return api.get(`/account/${id}`).then((r) => (r.data as ApiResult<AccountRecordDto>).data);
}
export function createAccount(req: AccountRecordReq) {
  return api.post('/account/create', req).then((r) => (r.data as ApiResult<AccountRecordDto>).data);
}
export function updateAccount(id: number, req: AccountRecordReq) {
  return api.put(`/account/update?id=${id}`, req).then((r) => (r.data as ApiResult<AccountRecordDto>).data);
}
export function deleteAccount(id: number) {
  return api.delete(`/account/delete?id=${id}`).then((r) => (r.data as ApiResult<unknown>).data);
}
export function accountSummary() {
  return api.get('/account/summary').then((r) => (r.data as ApiResult<AccountSummary>).data);
}
/** 记账统计：当月收支 + 近 6 个月收支趋势 */
export function accountStatistics(year: number, month: number) {
  return api.get('/account/statistics', { params: { year, month } }).then((r) => (r.data as ApiResult<AccountStatisticsDto>).data);
}
/** 导出某月账单 CSV（返回 Blob，由调用方触发下载） */
export async function exportAccountCsv(year: number, month: number) {
  const resp = await api.get('/account/export', { params: { year, month }, responseType: 'blob' });
  return resp.data as Blob;
}
