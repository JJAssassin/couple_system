/**
 * 心情分（1-10）与卡通心情图标（mood_*.png）的共享映射。
 * 5 档：糟糕 1-2 / 难过 3-4 / 平静 5-6 / 开心 7-8 / 幸福 9-10，
 * 图标资产对应 src/assets/icons/ip/mood_*.png。
 */
export type MoodIconName =
  | 'mood_terrible'
  | 'mood_sad'
  | 'mood_neutral'
  | 'mood_good'
  | 'mood_great';

export function moodIconName(score: number): MoodIconName {
  const s = Math.max(1, Math.min(10, Math.round(score)));
  if (s <= 2) return 'mood_terrible';
  if (s <= 4) return 'mood_sad';
  if (s <= 6) return 'mood_neutral';
  if (s <= 8) return 'mood_good';
  return 'mood_great';
}

/** 心情选择器的 5 档选项：value 取各档代表分，label 为档位名 */
export const MOOD_LEVELS: { value: number; icon: MoodIconName; label: string }[] = [
  { value: 2, icon: 'mood_terrible', label: '糟糕' },
  { value: 4, icon: 'mood_sad', label: '难过' },
  { value: 6, icon: 'mood_neutral', label: '平静' },
  { value: 8, icon: 'mood_good', label: '开心' },
  { value: 10, icon: 'mood_great', label: '幸福' },
];
