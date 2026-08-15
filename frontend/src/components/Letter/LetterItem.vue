<template>
  <div class="letter-item">
    <div class="letter-head">
      <span class="letter-when">{{ fmt(letter.unlockTime) }} 解锁</span>
      <n-tag v-if="letter.receiverUserId === currentUserId" size="small" type="info" round>收件</n-tag>
      <n-tag v-else size="small" round>寄出</n-tag>
    </div>

    <!-- 已解锁：展示正文（content 非接收人时后端已置空） -->
    <div v-if="letter.isUnlocked" class="letter-body">
      <img v-if="letter.coverImage" :src="letter.coverImage" class="letter-cover" alt="配图" />
      <div class="letter-content" v-html="letter.content"></div>
    </div>

    <!-- 未解锁：锁呼吸动画 -->
    <div v-else class="letter-locked">
      <Lock class="letter-lock" :size="34" />
      <span class="letter-tip">将于 {{ fmt(letter.unlockTime) }} 解锁</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { NTag } from 'naive-ui';
import { Lock } from 'lucide-vue-next';
import type { LetterDto } from '@/types';

const props = defineProps<{ letter: LetterDto; currentUserId: number }>();

function fmt(s: string) {
  const d = new Date(s);
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}.${d.getMonth() + 1}.${d.getDate()} ${p(d.getHours())}:${p(d.getMinutes())}`;
}
</script>

<style scoped>
.letter-item { display: flex; flex-direction: column; gap: 10px; }
.letter-head { display: flex; align-items: center; justify-content: space-between; gap: 8px; }
.letter-when { color: var(--color-ink-3); font-size: 12px; }
.letter-body { display: flex; flex-direction: column; gap: 10px; }
.letter-cover { width: 100%; border-radius: 12px; object-fit: cover; max-height: 220px; }
.letter-content { line-height: 1.8; white-space: pre-wrap; word-break: break-word; }
.letter-locked { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 10px; padding: 24px 0; }
.letter-lock { font-size: 34px; }
.letter-tip { color: var(--color-ink-3); font-size: 13px; }
</style>
