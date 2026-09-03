import { defineStore } from 'pinia';
import { ref, h } from 'vue';
import type { MessageApi, NotificationApi } from 'naive-ui';
import { NButton } from 'naive-ui';

let _msg: MessageApi | undefined;
let _ntf: NotificationApi | undefined;
// 当前请求错误通知实例：重放/新错误时先销毁旧的，避免多条错误通知堆叠。
let _curReqNtf: { destroy: () => void } | undefined;

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
    // 请求层错误（网络异常/5xx）的浪漫风兜底：标题 + 文案 + 可选「重试一下」按钮。
    // 与 #1 路由错误边界互补——#1 兜渲染层异常，本方法兜接口层异常；
    // onRetry 仅在「可重试」场景（网络抖动、超时、5xx）传入（= 重放原请求配置）。
    requestError: (message: string, onRetry?: () => void) => {
      if (_curReqNtf) {
        try {
          _curReqNtf.destroy();
        } catch {
          /* 已销毁，忽略 */
        }
        _curReqNtf = undefined;
      }
      const inst = _ntf?.create({
        title: '出了点小状况',
        content: message,
        type: 'error',
        duration: 6000,
        action: onRetry
          ? () =>
              h(
                NButton,
                {
                  size: 'small',
                  type: 'primary',
                  secondary: true,
                  round: true,
                  onClick: () => {
                    if (_curReqNtf) _curReqNtf.destroy();
                    onRetry();
                  },
                },
                { default: () => '重试一下' }
              )
          : undefined,
      });
      _curReqNtf = inst;
      announce(message, true);
    },
  };
});
