import { defineStore } from 'pinia';
import { ref } from 'vue';
import type { MessageApi, NotificationApi } from 'naive-ui';

let _msg: MessageApi | undefined;
let _ntf: NotificationApi | undefined;

export function bindNotify(m: MessageApi, n: NotificationApi) {
  _msg = m;
  _ntf = n;
}

export const useNotifyStore = defineStore('notify', () => {
  // WCAG 4.1.3 状态播报：把成功/错误/信息同步到隐藏的 aria-live 区域，
  // 让屏幕阅读器在内容动态变化时收到「状态消息」，而无需移动焦点。
  const polite = ref('');
  const assertive = ref('');

  // 先清空再写入，确保连续出现相同文案时读屏也能重新播报
  function announce(text: string, assert: boolean) {
    const target = assert ? assertive : polite;
    target.value = '';
    setTimeout(() => {
      target.value = text;
    }, 30);
  }

  return {
    polite,
    assertive,
    success: (t: string) => {
      _msg?.success(t);
      announce(t, false);
    },
    error: (t: string) => {
      _msg?.error(t);
      announce(t, true);
    },
    info: (t: string) => {
      _msg?.info(t);
      announce(t, false);
    },
    notify: (title: string, content: string) => {
      _ntf?.create({ title, content });
      announce(content || title, false);
    },
  };
});
