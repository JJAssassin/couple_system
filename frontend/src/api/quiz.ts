import api from '@/utils/request';
import type {
  ApiResult, PagedResult, QuizQuestionDto, QuizQuestionReq, QuizRoundDto, QuizStatsDto,
} from '@/types';

export interface QuizStartReq { questionId?: number | null; }
export interface QuizAnswerReq { roundId: number; answer: number; }

// ---------- 题库 ----------
export async function listQuizQuestions() {
  const { data } = await api.get('/quiz/questions');
  return (data as ApiResult<QuizQuestionDto[]>).data;
}
export async function createQuizQuestion(req: QuizQuestionReq) {
  const { data } = await api.post('/quiz/question/create', req);
  return (data as ApiResult<QuizQuestionDto>).data;
}
export async function deleteQuizQuestion(id: number) {
  const { data } = await api.delete('/quiz/question/delete', { params: { id } });
  return (data as ApiResult<object>).data;
}

// ---------- 对局 ----------
export async function listQuizRounds(params: { page?: number; pageSize?: number }) {
  const { data } = await api.get('/quiz/rounds', { params });
  return (data as ApiResult<PagedResult<QuizRoundDto>>).data;
}
export async function getQuizRound(id: number) {
  const { data } = await api.get(`/quiz/round/${id}`);
  return (data as ApiResult<QuizRoundDto>).data;
}
export async function startQuizRound(req: QuizStartReq) {
  const { data } = await api.post('/quiz/start', req);
  return (data as ApiResult<QuizRoundDto>).data;
}
export async function answerQuizRound(req: QuizAnswerReq) {
  const { data } = await api.put('/quiz/answer', req);
  return (data as ApiResult<QuizRoundDto>).data;
}
export async function deleteQuizRound(id: number) {
  const { data } = await api.delete('/quiz/round/delete', { params: { id } });
  return (data as ApiResult<object>).data;
}
export async function getQuizStats() {
  const { data } = await api.get('/quiz/stats');
  return (data as ApiResult<QuizStatsDto>).data;
}
