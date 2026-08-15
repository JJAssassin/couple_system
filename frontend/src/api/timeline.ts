import api from '@/utils/request';
import type { ApiResult, TimelineItemDto } from '@/types';

export async function listTimeline(params: { year?: number | null; month?: number | null }) {
  const { data } = await api.get('/timeline/list', { params });
  return (data as ApiResult<TimelineItemDto[]>).data;
}
