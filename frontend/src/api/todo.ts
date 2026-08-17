import api from '@/utils/request';
import type { ApiResult, PagedResult, TodoDto, TodoReq } from '@/types';

export interface TodoIdReq { id: number; }
export interface TodoAssignReq { id: number; assigneeUserId?: number | null; }

export async function listTodo(params: { page?: number; pageSize?: number }) {
  const { data } = await api.get('/todo/list', { params });
  return (data as ApiResult<PagedResult<TodoDto>>).data;
}
export async function getTodo(id: number) {
  const { data } = await api.get(`/todo/${id}`);
  return (data as ApiResult<TodoDto>).data;
}
export async function createTodo(req: TodoReq) {
  const { data } = await api.post('/todo/create', req);
  return (data as ApiResult<TodoDto>).data;
}
export async function updateTodo(id: number, req: TodoReq) {
  const { data } = await api.put('/todo/update', req, { params: { id } });
  return (data as ApiResult<TodoDto>).data;
}
export async function deleteTodo(id: number) {
  const { data } = await api.delete('/todo/delete', { params: { id } });
  return (data as ApiResult<object>).data;
}
export async function toggleTodo(req: TodoIdReq) {
  const { data } = await api.put('/todo/toggle', req);
  return (data as ApiResult<TodoDto>).data;
}
export async function assignTodo(req: TodoAssignReq) {
  const { data } = await api.put('/todo/assign', req);
  return (data as ApiResult<TodoDto>).data;
}
