<template>
  <transition name="ob-fade">
    <div v-if="visible" class="ob-mask">
      <div ref="card" v-bind="dialogAttrs" class="ob-card">
        <div class="ob-grip" />
        <div class="ob-stage">
          <section v-for="(s, i) in slides" :key="i" class="ob-slide" :class="{ active: i === step }">
            <component :is="s.icon" class="ob-ico" :size="56" />
            <h2>{{ s.title }}</h2>
            <p>{{ s.desc }}</p>
          </section>
        </div>

        <div v-if="step === slides.length - 1" class="ob-pair">
          <div class="ob-pair-label">把配对码发给 TA</div>
          <div class="ob-code">{{ pairCode }}</div>
          <div class="ob-pair-tip">TA 在「设置」中输入此码即可绑定你们的小世界（演示用，可忽略）</div>
        </div>

        <div class="ob-dots">
          <span v-for="(s, i) in slides" :key="i" :class="['dot', { on: i === step }]" />
        </div>

        <div class="ob-actions">
          <button v-if="step < slides.length - 1" class="ob-skip" @click="finish">跳过</button>
          <button class="ob-next" @click="next">{{ step === slides.length - 1 ? '开始使用' : '下一步' }}</button>
        </div>
      </div>
    </div>
  </transition>
</template>

<script setup lang="ts">
import { ref, onMounted, type Component } from 'vue';
import { useDialogA11y } from '@/composables/useDialogA11y';
import { Heart, PenLine, Camera, Link } from 'lucide-vue-next';
import { useAuthStore } from '@/store/authStore';

const KEY = 'cl_onboarded';
const auth = useAuthStore();
const visible = ref(false);
const step = ref(0);
const pairCode = ref('');

const slides = [
  { icon: Heart, title: '欢迎来到你们的小世界', desc: '这里记录你们的恋爱点滴：日记、愿望、纪念日、相册、足迹……一切关于「我们」的事。' },
  { icon: PenLine, title: '写下每一天', desc: '用双人日记记录心情，用愿望清单收藏想一起做的事，用纪念日倒数每一个重要日子。' },
  { icon: Camera, title: '留住每个瞬间', desc: '上传本地照片到双人相册，用足迹记录小确幸，记账看看共同小金库涨了多少。' },
  { icon: Link, title: '邀请 TA 一起', desc: '把下方的配对码发给对方，绑定属于你们两个人的空间，所有记录双方共享。' },
];

onMounted(() => {
  if (!localStorage.getItem(KEY) && auth.profile) {
    pairCode.value = Math.random().toString(36).slice(2, 8).toUpperCase();
    visible.value = true;
  }
});

function next() {
  if (step.value < slides.length - 1) step.value += 1;
  else finish();
}
function finish() {
  localStorage.setItem(KEY, '1');
  visible.value = false;
}

const card = ref<HTMLElement>();

// 无障碍：对话框语义 + 焦点陷阱 + Esc + 焦点归还
const { dialogAttrs } = useDialogA11y({
  isOpen: visible,
  close: finish,
  dialogRef: card,
  ariaLabel: '新手引导',
});
</script>

<style scoped>
.ob-mask {
  position: fixed; inset: 0; z-index: 1000;
  display: flex;
  background: rgba(20, 18, 22, 0.5); backdrop-filter: blur(4px);
  padding: calc(env(safe-area-inset-top) + 20px) 20px calc(env(safe-area-inset-bottom) + 20px);
  overflow-y: auto; -webkit-overflow-scrolling: touch;
}
.ob-card {
  width: min(440px, 94vw); margin: auto; background: var(--color-cream); border-radius: 24px;
  padding: 16px 24px 24px; box-shadow: 0 20px 50px rgba(0, 0, 0, 0.3);
  text-align: center; overflow: hidden;
}
.ob-grip { width: 40px; height: 5px; border-radius: 999px; background: var(--color-ink-soft); margin: 0 auto 16px; }
.ob-stage { position: relative; height: 210px; }
.ob-slide {
  position: absolute; inset: 0; display: flex; flex-direction: column; align-items: center; justify-content: center;
  gap: 12px; opacity: 0; transform: translateX(24px); transition: opacity 0.35s var(--ease-love), transform 0.35s var(--ease-love);
  pointer-events: none;
}
.ob-slide.active { opacity: 1; transform: none; pointer-events: auto; }
.ob-ico { color: var(--color-rose-text); }
.ob-slide h2 { margin: 0; font-size: 19px; color: var(--color-ink); }
.ob-slide p { margin: 0; color: var(--color-ink-2); font-size: 14px; line-height: 1.7; padding: 0 6px; }
.ob-pair { margin: 16px 0 4px; }
.ob-pair-label { font-size: 13px; color: var(--color-ink-3); margin-bottom: 8px; }
.ob-code {
  font-family: var(--font-mono); font-size: 30px; font-weight: 700; letter-spacing: 0.18em; color: var(--color-accent-text);
  background: var(--color-mist); border-radius: var(--radius-md); padding: 10px 0;
  box-shadow: inset 3px 3px 7px var(--color-ink-3), inset -3px -3px 7px #ffffff;
}
.ob-pair-tip { font-size: 12px; color: var(--color-ink-3); margin-top: 10px; }
.ob-dots { display: flex; gap: 8px; justify-content: center; margin: 16px 0; }
.dot { width: 8px; height: 8px; border-radius: 999px; background: var(--color-ink-soft); transition: all var(--dur-micro) var(--ease-love); }
.dot.on { width: 22px; background: var(--color-accent); }
.ob-actions { display: flex; gap: 10px; }
.ob-skip { flex: 0 0 auto; border: none; background: transparent; color: var(--color-ink-3); cursor: pointer; padding: 12px; font-size: 14px; }
.ob-next {
  flex: 1; border: none; cursor: pointer; padding: 12px; border-radius: var(--radius-md);
  background: var(--color-accent); color: var(--color-on-primary); font-size: 15px; font-weight: 600;
  box-shadow: 4px 4px 8px rgba(226, 90, 104, 0.4);
}
.ob-next:active { transform: translateY(2px); }
.ob-fade-enter-active, .ob-fade-leave-active { transition: opacity 0.3s ease; }
.ob-fade-enter-from, .ob-fade-leave-to { opacity: 0; }
</style>
