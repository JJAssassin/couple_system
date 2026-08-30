import api from '@/utils/request';
import type { ApiResult, PagedResult, BoardMessageDto, BoardMessageReq } from '@/types';

export interface BoardMessageIdReq { id: number; }
export interface BoardReactionReq { id: number; emojiKey: string; }

export async function listBoard(params: { page?: number; pageSize?: number }) {
  const { data } = await api.get('/board/list', { params });
  return (data as ApiResult<PagedResult<BoardMessageDto>>).data;
}
export async function getBoard(id: number) {
  const { data } = await api.get(`/board/${id}`);
  return (data as ApiResult<BoardMessageDto>).data;
}
export async function createBoard(req: BoardMessageReq) {
  const { data } = await api.post('/board/create', req);
  return (data as ApiResult<BoardMessageDto>).data;
}
export async function updateBoard(id: number, req: BoardMessageReq) {
  const { data } = await api.put('/board/update', req, { params: { id } });
  return (data as ApiResult<BoardMessageDto>).data;
}
export async function deleteBoard(id: number) {
  const { data } = await api.delete('/board/delete', { params: { id } });
  return (data as ApiResult<object>).data;
}
export async function pinBoard(req: BoardMessageIdReq) {
  const { data } = await api.put('/board/pin', req);
  return (data as ApiResult<BoardMessageDto>).data;
}
export async function addReaction(req: BoardReactionReq) {
  const { data } = await api.post('/board/reaction', req);
  return (data as ApiResult<BoardMessageDto>).data;
}
