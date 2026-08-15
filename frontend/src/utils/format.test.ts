import { describe, it, expect } from 'vitest';
import {
  formatDate, formatDateTime, permissionText, wishStatusText, conflictLevelText, accountTypeText, anniversaryTypeText,
} from './format';

describe('format 日期', () => {
  it('formatDate 正确补零', () => {
    expect(formatDate('2024-01-05T03:04:05Z')).toBe('2024-01-05');
  });
  it('空值返回空串', () => {
    expect(formatDate('')).toBe('');
    expect(formatDate(undefined)).toBe('');
    expect(formatDate('not-a-date')).toBe('');
  });
  it('formatDateTime 含时分', () => {
    expect(formatDateTime('2024-01-05T03:04:05Z')).toMatch(/^\d{4}-\d{2}-\d{2} \d{2}:\d{2}$/);
  });
});

describe('中文标签映射', () => {
  it('permissionText', () => {
    expect(permissionText(1)).toBe('公开');
    expect(permissionText(2)).toBe('仅自己可见');
    expect(permissionText(3)).toBe('对方可读');
    expect(permissionText(9)).toBe('9');
  });
  it('wishStatusText', () => {
    expect(wishStatusText(1)).toBe('未开始');
    expect(wishStatusText(3)).toBe('已完成');
    expect(wishStatusText(99)).toBe('未知');
  });
  it('conflictLevelText', () => {
    expect(conflictLevelText(1)).toBe('小别扭');
    expect(conflictLevelText(3)).toBe('严重分歧');
  });
  it('accountTypeText', () => {
    expect(accountTypeText(1)).toBe('收入');
    expect(accountTypeText(2)).toBe('支出');
  });
  it('anniversaryTypeText', () => {
    expect(anniversaryTypeText(1)).toBe('恋爱日');
    expect(anniversaryTypeText(4)).toBe('自定义');
  });
});
