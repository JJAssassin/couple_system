import { ref } from 'vue';
import { useSettingStore } from '@/store/settingStore';
import { useAuthStore } from '@/store/authStore';
import { useRealtime, type SyncSignal } from '@/composables/useRealtime';

/** 安装引导：浏览器触发 beforeinstallprompt 后置位；用户点「添加到主屏」时调用 prompt() */
const installAvailable = ref(false);
let deferredPrompt: any = null;

/** 模块名 → 中文（用于后台通知文案） */
const MODULE_CN: Record<string, string> = {
  message: '消息',
  wish: '愿望',
  album: '相册',
  diary: '日记',
  anniversary: '纪念日',
  conflict: '矛盾',
  letter: '书信',
  account: '记账',
  setting: '设置',
  footprint: '足迹',
  board: '留言',
  todo: '待办',
  quiz: '默契问答',
  budget: '预算',
  partner: '绑定',
  user: '资料',
};

export function notificationsSupported(): boolean {
  return typeof window !== 'undefined' && 'Notification' in window;
}

export function usePwa() {
  /** 注册 Service Worker + 监听安装事件（仅生产构建；dev 不注册以免劫持 HMR） */
  function register() {
    if (typeof window === 'undefined') return;
    if (import.meta.env.PROD && 'serviceWorker' in navigator) {
      window.addEventListener('load', () => {
        navigator.serviceWorker
          .register(`${import.meta.env.BASE_URL}sw.js`)
          .catch(() => {
            /* 注册失败不影响主流程 */
          });
      });
    }
    window.addEventListener('beforeinstallprompt', (e: any) => {
      e.preventDefault();
      deferredPrompt = e;
      installAvailable.value = true;
    });
    window.addEventListener('appinstalled', () => {
      installAvailable.value = false;
      deferredPrompt = null;
    });
  }

  function promptInstall() {
    if (!deferredPrompt) return;
    deferredPrompt
      .prompt()
      .catch(() => {
        /* 用户取消或环境不支持 */
      })
      .finally(() => {
        deferredPrompt = null;
        installAvailable.value = false;
      });
  }

  function dismissInstall() {
    installAvailable.value = false;
  }

  /** 申请通知权限（需用户手势触发）；返回最终权限状态 */
  async function requestNotificationPermission(): Promise<NotificationPermission | null> {
    if (!notificationsSupported()) return null;
    const cur = Notification.permission;
    if (cur === 'granted' || cur === 'denied') return cur;
    try {
      return await Notification.requestPermission();
    } catch {
      return null;
    }
  }

  /** 后台系统通知：页面隐藏 + 设置开启 + 已授权时，收到伴侣/新消息信号弹系统通知 */
  function setupNotifications() {
    if (!notificationsSupported()) return;
    const setting = useSettingStore();
    const auth = useAuthStore();
    const { onAnySync } = useRealtime();
    onAnySync((sig: SyncSignal) => {
      if (!setting.notifications) return;
      if (Notification.permission !== 'granted') return;
      if (!document.hidden) return; // 前台有 in-app 轻提示，不重复打扰
      fireNotification(sig, auth.profile?.id ?? 0);
    });
  }

  return {
    installAvailable,
    register,
    promptInstall,
    dismissInstall,
    requestNotificationPermission,
    setupNotifications,
    notificationsSupported,
  };
}

function fireNotification(sig: SyncSignal, myId: number) {
  const isPartner = sig.senderId != null && sig.senderId !== myId;
  let title = '我们的小世界';
  let body = '你们有了新动态 💞';
  if (sig.module === 'message') {
    title = '新消息';
    body = 'TA 给你发来了新消息 💌';
  } else if (isPartner) {
    const cn = MODULE_CN[sig.module] || '内容';
    title = `${cn}更新了`;
    body = `TA 刚刚更新了${cn} 💞`;
  } else {
    return; // 自己的变更、非消息模块：不打扰
  }
  try {
    new Notification(title, { body, icon: '/pwa-192x192.png', badge: '/pwa-192x192.png' });
  } catch {
    /* 部分浏览器要求从 SW 上下文构造 Notification，忽略即可 */
  }
}
