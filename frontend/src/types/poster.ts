// 年度海报组件（CoupleSummaryPoster.vue）的数据契约。
// 全部字段可选：后端 YearReport 不提供 agreements/goals/coverCaption 等自定义内容，
// 由调用方（Stats 页）以可选 prop 传入；缺失字段对应板块自动隐藏。

export interface PosterMetric {
  icon?: string;
  label: string;
  value: string | number;
  unit?: string;
}

export interface PosterFootprint {
  city: string;
  thumb?: string;
  emoji?: string;
}

export interface PosterData {
  title?: string;
  togetherDays?: number;
  dateRange?: string;
  coverPhoto?: string;
  coverCaption?: string;
  metrics?: PosterMetric[];
  momentPhotos?: string[];
  footprints?: PosterFootprint[];
  agreements?: string[];
  goals?: string[];
}
