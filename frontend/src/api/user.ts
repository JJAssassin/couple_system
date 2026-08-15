import api from '@/utils/request';
import type { ApiResult, UpdateProfileReq, UserProfile, ExportResp } from '@/types';

export async function updateProfile(req: UpdateProfileReq) {
  const { data } = await api.put('/user/profile', req);
  return (data as ApiResult<UserProfile>).data;
}

export async function exportAll() {
  const { data } = await api.get('/user/export/alldata');
  const resp = (data as ApiResult<ExportResp>).data;
  // 触发浏览器下载
  window.open(resp.downloadUrl, '_blank');
  return resp;
}
