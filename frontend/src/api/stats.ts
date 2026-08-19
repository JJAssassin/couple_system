import api from '@/utils/request';

// 年度恋爱报告（后端 /api/stats/yearreport，字段与 YearReportDto 对齐）
export interface AnniversaryPassed {
  name: string;
  targetDate: string;
}
export interface MonthlyFinance {
  month: string;
  income: number;
  expense: number;
}
export interface CategorySpend {
  category: string;
  amount: number;
}
export interface ChartPoint {
  label: string;
  value: number;
}
export interface YearReport {
  year: number;
  loveDays: number;
  anniversaryTotal: number;
  anniversaries: AnniversaryPassed[];
  diaryCount: number;
  avgMood: number;
  wishCreated: number;
  wishDone: number;
  todoDone: number;
  conflictCount: number;
  conflictResolved: number;
  letterCount: number;
  boardCount: number;
  imageCount: number;
  footprintCount: number;
  dateCount: number;
  dateCompleted: number;
  quizRounds: number;
  quizRevealed: number;
  quizMatched: number;
  matchRate: number;
  income: number;
  expense: number;
  monthlyFinance: MonthlyFinance[];
  topSpend: CategorySpend[];
  moodTrend: ChartPoint[];
  conflictTrend: ChartPoint[];
}

export async function fetchYearReport(year: number): Promise<YearReport> {
  const { data } = await api.get('/stats/yearreport', { params: { year } });
  return (data as { data: YearReport }).data;
}
