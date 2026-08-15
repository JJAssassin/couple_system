import api from '@/utils/request';
import type { ApiResult, DailyQuoteDto } from '@/types';

export async function getDailyQuote() {
  const { data } = await api.get('/quote/today');
  return (data as ApiResult<DailyQuoteDto>).data;
}
