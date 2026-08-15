import { defineStore } from 'pinia';
import { ref } from 'vue';
import * as partnerApi from '@/api/partner';
import type { ApiResult, BindStatus } from '@/types';

export const usePartnerStore = defineStore('partner', () => {
  const status = ref<BindStatus | null>(null);
  const loading = ref(false);

  async function load() {
    loading.value = true;
    try {
      const { data } = await partnerApi.getStatus();
      status.value = (data as ApiResult<BindStatus>).data;
    } catch {
      /* 忽略：未登录或网络异常时静默 */
    } finally {
      loading.value = false;
    }
  }

  return { status, loading, load };
});
