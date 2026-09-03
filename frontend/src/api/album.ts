import type { AxiosResponse } from 'axios';
import api from '@/utils/request';
import type { ApiResult, PagedResult, AlbumDto, AlbumReq, ImageDto, AlbumImageBatchUploadResult } from '@/types';

export function listAlbum(params: { page?: number; pageSize?: number }): Promise<AxiosResponse<ApiResult<PagedResult<AlbumDto>>>> {
  return api.get<ApiResult<PagedResult<AlbumDto>>>('/album/list', { params });
}
export function getAlbum(id: number) {
  return api.get(`/album/${id}`);
}
export function createAlbum(req: AlbumReq) {
  return api.post('/album/create', req);
}
export function updateAlbum(id: number, req: AlbumReq) {
  return api.put(`/album/update?id=${id}`, req);
}
export function deleteAlbum(id: number) {
  return api.delete(`/album/delete?id=${id}`);
}
export function listImages(albumId: number): Promise<AxiosResponse<ApiResult<ImageDto[]>>> {
  return api.get<ApiResult<ImageDto[]>>('/album/image/list', { params: { albumId } });
}
export function uploadImage(albumId: number, file: File) {
  const fd = new FormData();
  fd.append('file', file);
  // 注意：不要手动设置 Content-Type，交给 axios 自动携带 boundary，
  // 否则后端 multipart 解析不到 IFormFile，上传会静默失败。
  return api.post(`/image/upload?albumId=${albumId}`, fd);
}
export function deleteImage(id: number) {
  return api.delete(`/image/delete?id=${id}`);
}

// #17 相册批量：多选删除 / 移动到其他相册 / 拖拽排序
export function batchDeleteImages(ids: number[]) {
  return api.post('/album/image/batch-delete', { ids });
}
export function batchMoveImages(ids: number[], targetAlbumId: number) {
  return api.post('/album/image/batch-move', { ids, targetAlbumId });
}
export function reorderImages(ids: number[]) {
  return api.post('/album/image/reorder', { ids });
}

// #16-c 相册照片批量导入：一次请求多文件，归到指定相册
export function batchUploadImages(albumId: number, files: File[]) {
  const fd = new FormData();
  files.forEach((f) => fd.append('files', f));
  // 不手动设 Content-Type，交给 axios 自动带 boundary
  return api.post(`/image/batch-upload?albumId=${albumId}`, fd);
}

export type { AlbumDto, AlbumReq, ImageDto, PagedResult, ApiResult, AlbumImageBatchUploadResult };
