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
