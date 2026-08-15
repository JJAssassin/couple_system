import api from '@/utils/request';
import type { ApiResult, PagedResult, SystemMessageDto } from '@/types';

export async function listMessage(params: { page?: number; pageSize?: number }) {
  const { data } = await api.get('/message/list', { params });
  return (data as ApiResult<PagedResult<SystemMessageDto>>).data;
}

export async function unreadCount() {
  const { data } = await api.get('/message/unread/count');
  return (data as ApiResult<number>).data;
}

export async function readMessage(id: number) {
  const { data } = await api.put('/message/read', { id });
  return (data as ApiResult<SystemMessageDto>).data;
}

export async function readAll() {
  const { data } = await api.put('/message/read/all');
  return (data as ApiResult<number>).data;
}
