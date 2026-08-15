import api from '@/utils/request';
import type { ApiResult } from '@/types';

// 通用单图上传：不依赖相册，返回可访问的相对路径 /uploads/xxx.jpg
export async function uploadStandalone(file: File): Promise<string> {
  const fd = new FormData();
  fd.append('file', file);
  // 不要手动设置 Content-Type，交给 axios 自动携带 boundary
  const { data } = await api.post('/image/upload-standalone', fd);
  const r = data as ApiResult<{ path: string }>;
  return r.data?.path ?? '';
}
