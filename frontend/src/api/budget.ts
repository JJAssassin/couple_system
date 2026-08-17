import api from '@/utils/request';
import type { ApiResult, BudgetDto, BudgetSetReq, MonthlyBudgetDto } from '@/types';

export function getMonthlyBudget(year: number, month: number) {
  return api.get('/budget/monthly', { params: { year, month } }).then((r) => (r.data as ApiResult<MonthlyBudgetDto>).data);
}
export function getCurrentBudget() {
  return api.get('/budget/current').then((r) => (r.data as ApiResult<MonthlyBudgetDto>).data);
}
export function listBudgets(year: number, month: number) {
  return api.get('/budget/list', { params: { year, month } }).then((r) => (r.data as ApiResult<BudgetDto[]>).data);
}
export function setBudget(req: BudgetSetReq) {
  return api.post('/budget/set', req).then((r) => (r.data as ApiResult<BudgetDto>).data);
}
export function deleteBudget(id: number) {
  return api.delete(`/budget/delete?id=${id}`).then((r) => (r.data as ApiResult<unknown>).data);
}
