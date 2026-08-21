import api from '@/utils/request';
import type { ApiResult, TaskTemplateDto, TaskTemplateReq, TaskRecordDto, TaskRecordReq, TaskStatsDto } from '@/types';

export async function listTaskTemplates(page = 1, pageSize = 50, isActive?: boolean) {
  const { data } = await api.get('/task/templates', { params: { page, pageSize, isActive } });
  return (data as ApiResult<{ items: TaskTemplateDto[]; total: number; page: number; pageSize: number }>).data;
}

export async function createTaskTemplate(req: TaskTemplateReq) {
  const { data } = await api.post('/task/templates', req);
  return (data as ApiResult<TaskTemplateDto>).data;
}

export async function updateTaskTemplate(id: number, req: TaskTemplateReq) {
  const { data } = await api.put(`/task/templates/${id}`, req);
  return (data as ApiResult<TaskTemplateDto>).data;
}

export async function toggleTaskTemplate(id: number) {
  const { data } = await api.put(`/task/templates/${id}/toggle`);
  return (data as ApiResult<object>).data;
}

export async function deleteTaskTemplate(id: number) {
  const { data } = await api.delete(`/task/templates/${id}`);
  return (data as ApiResult<object>).data;
}

export async function checkInTask(req: TaskRecordReq) {
  const { data } = await api.post('/task/checkin', req);
  return (data as ApiResult<TaskRecordDto>).data;
}

export async function cancelCheckIn(recordId: number) {
  const { data } = await api.delete(`/task/records/${recordId}`);
  return (data as ApiResult<object>).data;
}

export async function getTaskStats() {
  const { data } = await api.get('/task/stats');
  return (data as ApiResult<TaskStatsDto>).data;
}

export async function listRecentTaskRecords(take = 20) {
  const { data } = await api.get('/task/records', { params: { take } });
  return (data as ApiResult<TaskRecordDto[]>).data;
}

export async function exportTaskData() {
  const { data } = await api.get('/user/export/alldata');
  const resp = (data as ApiResult<{ downloadUrl: string; fileName: string; mediaCount: number }>).data;
  window.open(resp.downloadUrl, '_blank');
  return resp;
}
