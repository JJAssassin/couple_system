<template>
  <!-- 纯逻辑组件：仅在收到伴侣的实时更新时弹轻提示，无可见 DOM -->
</template>

<script setup lang="ts">
import { useRealtime } from '@/composables/useRealtime';
import { useNotifyStore } from '@/store/notifyStore';

const { onAnySync } = useRealtime();
const notify = useNotifyStore();

// 从 JWT 解析当前用户 Id（不依赖 authStore.profile：刷新页面后 profile 可能尚未加载，
// 但 token 已在 localStorage，故直接解 token 最稳，避免漏提示伴侣更新）。
function currentUserId(): number | null {
  const t = localStorage.getItem('cl_at');
  if (!t) return null;
  try {
    const p = t.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const json = JSON.parse(decodeURIComponent(escape(window.atob(p))));
    const id = json.sub ?? json['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
    return id != null ? Number(id) : null;
  } catch {
    return null;
  }
}

// 模块名 -> 中文展示（与后端 [Broadcast(Module=...)] 对齐；缺省回退为原始模块名）
const MODULE_NAMES: Record<string, string> = {
  partner: '亲密关系',
  anniversary: '纪念日',
  diary: '日记',
  wish: '愿望',
  album: '相册',
  conflict: '矛盾记录',
  letter: '信件',
  account: '记账',
  setting: '伴侣设置',
  footprint: '足迹',
};

// 同一模块短时间内多次变更（一次保存可能触发多条）只提示一次，避免刷屏
const DEBOUNCE_MS = 1500;
const lastShownAt = new Map<string, number>();

onAnySync((sig) => {
  const senderId = sig.senderId;
  if (senderId == null) return; // 系统消息 / 后台任务无发送者，不提示
  const myId = currentUserId();
  if (myId == null || senderId === myId) return; // 只看伴侣的改动，自己的回显不提示

  const now = Date.now();
  const last = lastShownAt.get(sig.module) ?? 0;
  if (now - last < DEBOUNCE_MS) return;
  lastShownAt.set(sig.module, now);

  const label = MODULE_NAMES[sig.module] ?? sig.module;
  notify.notify('伴侣更新了', `对方刚刚更新了「${label}」`);
});
</script>
