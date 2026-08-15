import api from '@/utils/request';
import type { ApiResult, LetterDto, LetterReq } from '@/types';

export async function listLetter() {
  const { data } = await api.get('/letter/list');
  return (data as ApiResult<LetterDto[]>).data;
}

export async function getLetter(id: number) {
  const { data } = await api.get(`/letter/${id}`);
  return (data as ApiResult<LetterDto>).data;
}

export async function createLetter(req: LetterReq) {
  const { data } = await api.post('/letter/create', req);
  return (data as ApiResult<LetterDto>).data;
}

export async function deleteLetter(id: number) {
  const { data } = await api.delete('/letter/delete', { params: { id } });
  return (data as ApiResult<object>).data;
}
