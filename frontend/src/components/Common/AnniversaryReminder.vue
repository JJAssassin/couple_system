<template>
  <teleport to="body">
    <transition name="rem-fade">
      <div v-if="tip" class="rem-tip" role="button" tabindex="0" aria-label="查看纪念日提醒" @click="open" @keydown.enter.prevent="open" @keydown.space.prevent="open">
        <span class="rem-ico">💝</span>
        <div class="rem-body">
          <div class="rem-title">{{ tip.title }}</div>
          <div class="rem-sub">{{ tip.content }}</div>
        </div>
        <button class="rem-close" aria-label="关闭" @click.stop="dismiss">×</button>
      </div>
    </transition>
  </teleport>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { listMessage, readMessage } from '@/api/message';
import type { SystemMessageDto } from '@/types';

const LS_KEY = 'cl_ann_rem_date';
const tip = ref<SystemMessageDto | null>(null);
const router = useRouter();

/** 拉取未读的「纪念日提醒」类消息（messageType=1 Anniversary），今天首次打开时提示 */
async function checkAnniversaryReminders() {
  try {
    // 当天只提示一次，避免反复打扰
    const today = new Date().toDateString();
    if (localStorage.getItem(LS_KEY) === today) return;
    const page = await listMessage({ page: 1, pageSize: 20 });
    const now = new Date();
    const todayStart = new Date(now.getFullYear(), now.getMonth(), now.getDate()).getTime();
    const ann = (page.items ?? []).find(
      (m) => m.messageType === 1 && !m.isRead && new Date(m.createTime).getTime() >= todayStart,
    );
    if (!ann) return;
    tip.value = ann;
    try {
      localStorage.setItem(LS_KEY, today);
    } catch {
      /* 忽略 */
    }
    // 系统通知（已授权时）
    if (typeof Notification !== 'undefined' && Notification.permission === 'granted') {
      try {
        new Notification(ann.title || '纪念日提醒', { body: ann.content, icon: '/pwa-192x192.png' });
      } catch {
        /* 忽略 */
      }
    }
  } catch {
    /* 弱网/未登录静默 */
  }
}

function dismiss() {
  tip.value = null;
}

async function open() {
  const t = tip.value;
  tip.value = null;
  if (!t) return;
  try {
    await readMessage(t.id); // 标记已读
  } catch {
    /* 忽略 */
  }
  router.push('/message');
}

onMounted(checkAnniversaryReminders);
</script>

<style scoped>
.rem-tip {
  position: fixed; top: 14px; left: 50%; transform: translateX(-50%);
  z-index: 1600; max-width: min(92vw, 420px);
  display: flex; align-items: center; gap: 10px;
  background: var(--color-surface); border: 1px solid var(--color-rose);
  border-radius: 16px; padding: 12px 14px; cursor: pointer;
  box-shadow: 0 12px 32px -8px rgba(255, 111, 125, 0.35);
}
.rem-ico { font-size: 22px; }
.rem-body { flex: 1; min-width: 0; }
.rem-title { font-size: 14px; font-weight: 700; color: var(--color-ink); }
.rem-sub { font-size: 12px; color: var(--color-ink-2); margin-top: 2px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.rem-close { border: none; background: none; color: var(--color-ink-3); font-size: 16px; cursor: pointer; padding: 2px 6px; }
.rem-fade-enter-active, .rem-fade-leave-active { transition: opacity 0.3s var(--ease-love), transform 0.3s var(--ease-love); }
.rem-fade-enter-from, .rem-fade-leave-to { opacity: 0; transform: translateX(-50%) translateY(-12px); }
:global(.reduce-motion) .rem-fade-enter-active,
:global(.reduce-motion) .rem-fade-leave-active { transition: none; }
</style>
