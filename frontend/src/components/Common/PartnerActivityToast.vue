<template>
  <!-- 纯逻辑组件：仅在收到伴侣的实时更新时弹轻提示，无可见 DOM -->
</template>

<script setup lang="ts">
import { useRealtime, type SyncSignal } from '@/composables/useRealtime';
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

// 同一模块短时间内多次变更（一次保存可能触发多条）只提示一次，避免刷屏
const DEBOUNCE_MS = 1500;
const lastShownAt = new Map<string, number>();

// 模块展示元信息：图标 + 展示名 + 几条浪漫文案（每次随机抽一条）
const MODULE_META: Record<string, { icon: string; display: string; lines: string[] }> = {
  partner: {
    icon: '💞',
    display: '亲密关系',
    lines: ['ta 修改了你们的资料', 'ta 改了你们的专属信息', 'ta 重新确认了和你的关系'],
  },
  anniversary: {
    icon: '🎉',
    display: '纪念日',
    lines: ['ta 标记了一个新的纪念日', 'ta 更新了你们的重要日子', 'ta 又记下了一个属于你们的日子'],
  },
  diary: {
    icon: '📔',
    display: '日记',
    lines: ['ta 写下了一篇新日记', 'ta 把今天的小心思记录了下来', 'ta 翻了一页属于你们的故事'],
  },
  wish: {
    icon: '✨',
    display: '愿望',
    lines: ['ta 新许了一个小愿望', 'ta 把一个小心愿放进了许愿瓶', 'ta 又种下了一颗想和你一起完成的事'],
  },
  album: {
    icon: '🖼️',
    display: '相册',
    lines: ['ta 上传了新照片', 'ta 把一张新回忆放进了相册', 'ta 又留下了一个瞬间'],
  },
  conflict: {
    icon: '🕊️',
    display: '矛盾记录',
    lines: ['ta 记下了一件事想跟你好好聊', 'ta 写下了想被理解的心情', 'ta 留了一笔，等你一起消化'],
  },
  letter: {
    icon: '💌',
    display: '信件',
    lines: ['ta 给你写了一封信 💌', 'ta 写了一段只对你说的话', 'ta 把心里话塞进了信箱'],
  },
  account: {
    icon: '🧾',
    display: '记账',
    lines: ['ta 记了一笔账', 'ta 更新了你们的小金库', 'ta 一起算了一笔生活账'],
  },
  setting: {
    icon: '⚙️',
    display: '伴侣设置',
    lines: ['ta 调整了一些设置', 'ta 改了你们的偏好', 'ta 完善了双方的小约定'],
  },
  footprint: {
    icon: '📍',
    display: '足迹',
    lines: ['ta 标记了一个新足迹', 'ta 记下了一个一起到过的地方', 'ta 在地图上点亮了一个坐标'],
  },
};

const KIND_NAMES: Record<string, string> = {
  created: '新增了',
  updated: '更新了',
  deleted: '删除了',
  reload: '刷新了',
};

function pick<T>(arr: T[]): T {
  return arr[Math.floor(Math.random() * arr.length)];
}

onAnySync((sig: SyncSignal) => {
  const senderId = sig.senderId;
  if (senderId == null) return; // 系统消息 / 后台任务无发送者，不提示
  const myId = currentUserId();
  if (myId == null || senderId === myId) return; // 只看伴侣的改动，自己的回显不提示

  const now = Date.now();
  const last = lastShownAt.get(sig.module) ?? 0;
  if (now - last < DEBOUNCE_MS) return;
  lastShownAt.set(sig.module, now);

  const meta = MODULE_META[sig.module] ?? {
    icon: '🔔',
    display: sig.module,
    lines: [`对方刚刚更新了「${sig.module}」`],
  };

  // 区分新增 / 更新 / 删除，让文案更具体
  const firstChange = sig.changes?.[0];
  const kind = firstChange?.kind ?? 'updated';
  const kindText = KIND_NAMES[kind] ?? '更新了';
  const line = pick(meta.lines);

  const title = `${meta.icon} 对方刚刚${kindText}「${meta.display}」`;
  // 末尾附一句轻语（不每次都换，保持自然）
  const tail = pick([
    '，快去看看吧',
    '，也许有惊喜',
    '，一起来看',
    '，别错过',
  ]);
  notify.notify(title, `${line}${tail}`);
});
</script>
