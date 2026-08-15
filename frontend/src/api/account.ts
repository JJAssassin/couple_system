import api from '@/utils/request';
import type { ApiResult, PagedResult, AccountRecordDto, AccountRecordReq } from '@/types';

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
