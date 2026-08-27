import api from '@/utils/request';
import type { ApiResult, UpdateProfileReq, UserProfile, ExportResp } from '@/types';

export async function updateProfile(req: UpdateProfileReq) {
  const { data } = await api.put('/user/profile', req);
  return (data as ApiResult<UserProfile>).data;
}

export async function exportAll() {
  const { data } = await api.get('/user/export/alldata');
  const resp = (data as ApiResult<ExportResp>).data;
  // 一次性下载令牌：映射到后端临时目录中的 zip（带短 TTL，下载即作废），杜绝公开可猜 URL 泄露 PII。
  // 用已带 Bearer 鉴权的 axios 以 blob 形式拉取，令牌仅经 X-Export-Token 头传递（绝不进 URL/历史）；
  // 二进制响应绕过 ApiResult 包装（请求拦截器对 blob 直接放行），前端手动触发下载。
  const blobResp = await api.get('/user/export/download', {
    headers: { 'X-Export-Token': resp.token },
    responseType: 'blob',
  });
  const url = window.URL.createObjectURL(blobResp.data as Blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = resp.fileName || 'couple_export.zip';
  document.body.appendChild(a);
  a.click();
  a.remove();
  window.URL.revokeObjectURL(url);
  return resp;
}
