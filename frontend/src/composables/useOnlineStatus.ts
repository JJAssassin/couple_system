import { ref, onMounted, onUnmounted } from 'vue';

/**
 * 在线/离线状态。Service Worker 已让 app-shell / 资源 / 上传图可离线访问，
 * 但断网时用户仍需要明确的状态反馈，避免把「离线」误认为「加载中/出错」。
 * 监听 window 的 online/offline 事件，SSR 安全（CSR SPA 下 window 必存在，仍做守卫）。
 */
export function useOnlineStatus() {
  const isOnline = ref(typeof navigator !== 'undefined' ? navigator.onLine : true);

  function update() {
    isOnline.value = navigator.onLine;
  }

  onMounted(() => {
    window.addEventListener('online', update);
    window.addEventListener('offline', update);
  });
  onUnmounted(() => {
    window.removeEventListener('online', update);
    window.removeEventListener('offline', update);
  });

  return { isOnline };
}
