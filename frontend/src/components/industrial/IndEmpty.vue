<template>
  <div class="ind-empty" role="status" aria-live="polite">
    <!-- 装饰插画：柔和的线描圆环 + 漂浮小心心，给空态一点"被设计过"的质感 -->
    <div class="art" :class="{ 'no-art': !showArt }">
      <svg v-if="showArt" class="art-svg" viewBox="0 0 120 120" aria-hidden="true">
        <circle class="ring" cx="60" cy="60" r="46" />
        <circle class="ring2" cx="60" cy="60" r="34" />
        <!-- 两颗心：一大一小、微微交叠，寓意"彼此/双人" -->
        <path
          class="heart heart-a"
          d="M60 80c-15-10-24-19-24-30a12 12 0 0 1 24-3 12 12 0 0 1 24 3c0 11-9 20-24 30z"
        />
        <path
          class="heart heart-b"
          d="M86 70c-7-4.5-11-8.5-11-13.5a5.5 5.5 0 0 1 11-1.5 5.5 5.5 0 0 1 11 1.5c0 5-4 9-11 13.5z"
        />
        <g class="spark">
          <path d="M20 26l2 6 6 2-6 2-2 6-2-6-6-2 6-2z" />
          <path d="M100 40l1.4 4.2 4.2 1.4-4.2 1.4L100 51l-1.4-4.2L94.4 45l4.2-1.4z" />
        </g>
      </svg>
      <div v-if="emoji" class="halo">{{ emoji }}</div>
    </div>
    <div class="title">{{ title }}</div>
    <div v-if="desc" class="desc">{{ desc }}</div>
    <div v-if="$slots.action || actionText" class="action">
      <slot name="action">
        <IndButton v-if="actionText" @click="$emit('action')">{{ actionText }}</IndButton>
      </slot>
    </div>
  </div>
</template>

<script setup lang="ts">
import IndButton from './IndButton.vue';

withDefaults(
  defineProps<{
    emoji?: string;
    title?: string;
    desc?: string;
    actionText?: string;
    showArt?: boolean;
  }>(),
  { emoji: '', title: '这里还是空的', desc: '', showArt: true }
);
defineEmits<{ (e: 'action'): void }>();
</script>

<style scoped>
.ind-empty {
  text-align: center; padding: 40px 20px; border-radius: var(--radius-lg);
  background: var(--color-surface-2);
  border: 1px dashed var(--color-border);
}
.art { position: relative; width: 96px; height: 96px; margin: 0 auto 16px; }
.art.no-art { width: auto; height: auto; margin-bottom: 4px; }
.art-svg { position: absolute; inset: 0; width: 100%; height: 100%; opacity: 0.5; }
.ring { fill: none; stroke: var(--color-accent); stroke-width: 2; opacity: 0.35; }
.ring2 { fill: none; stroke: var(--color-accent); stroke-width: 1.5; opacity: 0.22; transform-origin: 60px 60px; animation: spin 26s linear infinite; }
.heart { fill: var(--color-accent); opacity: 0.18; }
.heart-a { fill: var(--color-accent); opacity: 0.18; }
.heart-b { fill: var(--module-accent, var(--color-rose-deep)); opacity: 0.34; }
.spark { fill: var(--color-accent); opacity: 0.5; animation: spark 3.2s var(--ease-love) infinite; }
.halo {
  position: absolute; inset: 0; display: grid; place-items: center;
  font-size: 44px; line-height: 1; filter: drop-shadow(2px 2px 3px rgba(0, 0, 0, 0.18));
}
.title { font-size: 15px; font-weight: 600; color: var(--color-ink); }
.desc { font-size: 13px; color: var(--color-ink-3); margin-top: 6px; }
.action { margin-top: 16px; display: flex; justify-content: center; }

@keyframes spin { to { transform: rotate(360deg); } }
@keyframes spark { 0%, 100% { opacity: 0.25; transform: scale(0.9); } 50% { opacity: 0.6; transform: scale(1.1); } }
.reduce-motion .ring2, .reduce-motion .spark { animation: none; }
</style>
