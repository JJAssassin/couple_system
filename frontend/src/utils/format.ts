// 通用格式化与中文标签映射（纯函数，便于单元测试与多页复用）

export function formatDate(iso?: string): string {
  if (!iso) return '';
  const d = new Date(iso);
  if (isNaN(d.getTime())) return '';
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

export function formatDateTime(iso?: string): string {
  if (!iso) return '';
  const d = new Date(iso);
  if (isNaN(d.getTime())) return '';
  return `${formatDate(iso)} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
}

/**
 * 本地 naive ISO（不含 Z）：把毫秒时间戳按「本地时区分量」拼成
 * YYYY-MM-DDTHH:mm:ss，避免 toISOString() 转 UTC 导致正偏移时区
 * （如 UTC+8）日期/时间回退的问题。后端 DateTime 字段为 timezone-less
 * 存储，按本地分量落库即可与展示端一致往返。
 */
export function toLocalISO(ts: number | null | undefined): string | undefined {
  if (ts == null) return undefined;
  const d = new Date(ts);
  if (isNaN(d.getTime())) return undefined;
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}T${p(d.getHours())}:${p(d.getMinutes())}:${p(d.getSeconds())}`;
}

export function permissionText(t: number): string {
  return t === 1 ? '公开' : t === 2 ? '仅自己可见' : t === 3 ? '对方可读' : String(t);
}

export function wishStatusText(s: number): string {
  return s === 1 ? '未开始' : s === 2 ? '进行中' : s === 3 ? '已完成' : s === 4 ? '已归档' : '未知';
}

export function conflictLevelText(l: number): string {
  return l === 1 ? '小别扭' : l === 2 ? '中度争吵' : l === 3 ? '严重分歧' : '未知';
}

export function accountTypeText(t: number): string {
  return t === 1 ? '收入' : t === 2 ? '支出' : '未知';
}

export function anniversaryTypeText(t: number): string {
  return t === 1 ? '恋爱日' : t === 2 ? '生日' : t === 3 ? '相识日' : t === 4 ? '自定义' : '未知';
}
