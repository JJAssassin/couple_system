import api from '@/utils/request';
import type {
  ApiResult,
  PagedResult,
  DiaryDto,
  DiaryReq,
  DiaryCommentDto,
  DiaryCommentReq,
} from '@/types';

export async function listDiary(params: { page?: number; pageSize?: number; author?: 'all' | 'mine' | 'partner' }) {
  const { data } = await api.get('/diary/list', { params });
  return (data as ApiResult<PagedResult<DiaryDto>>).data;
}

export async function getDiary(id: number) {
  const { data } = await api.get(`/diary/${id}`);
  return (data as ApiResult<DiaryDto>).data;
}

export async function createDiary(req: DiaryReq) {
  const { data } = await api.post('/diary/create', req);
  return (data as ApiResult<DiaryDto>).data;
}

export async function updateDiary(id: number, req: DiaryReq) {
  const { data } = await api.put('/diary/update', req, { params: { id } });
  return (data as ApiResult<DiaryDto>).data;
}

export async function deleteDiary(id: number) {
  const { data } = await api.delete('/diary/delete', { params: { id } });
  return (data as ApiResult<object>).data;
}

export async function listComments(diaryId: number) {
  const { data } = await api.get('/diary/comment/list', { params: { diaryId } });
  return (data as ApiResult<DiaryCommentDto[]>).data;
}

export async function addComment(req: DiaryCommentReq) {
  const { data } = await api.post('/diary/comment/create', req);
  return (data as ApiResult<DiaryCommentDto>).data;
}
