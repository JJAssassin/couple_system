import { defineStore } from 'pinia';
import type { MessageApi, NotificationApi } from 'naive-ui';

let _msg: MessageApi | undefined;
let _ntf: NotificationApi | undefined;

export function bindNotify(m: MessageApi, n: NotificationApi) {
  _msg = m;
  _ntf = n;
}

export const useNotifyStore = defineStore('notify', () => ({
  success: (t: string) => _msg?.success(t),
  error: (t: string) => _msg?.error(t),
  info: (t: string) => _msg?.info(t),
  notify: (title: string, content: string) => _ntf?.create({ title, content }),
}));
