<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { listAnniversaries } from '@/api/anniversary'
import { useSettingStore } from '@/store/settingStore'

const setting = useSettingStore()
const visible = ref(false)
const title = ref('')
const subtitle = ref('')

const pad = (n: number) => String(n).padStart(2, '0')
const todayMMDD = (() => { const d = new Date(); return pad(d.getMonth() + 1) + pad(d.getDate()) })()
const dayKey = (() => { const d = new Date(); return `${d.getFullYear()}-${todayMMDD}` })()

// 固定节日（公历，避免农历推算误差）；可按需增删
const FESTIVALS: { mmdd: string; label: string }[] = [
  { mmdd: '0101', label: '元旦' },
  { mmdd: '0214', label: '情人节' },
  { mmdd: '0314', label: '白色情人节' },
  { mmdd: '0520', label: '520' },
  { mmdd: '1111', label: '双十一' },
  { mmdd: '1225', label: '圣诞节' },
]

const HEARTS = ['💗', '💕', '🌸', '♥', '🌹', '✨']
const hearts = Array.from({ length: 20 }, (_, i) => ({
  left: (i * 53) % 100,
  delay: (i * 0.37) % 4,
  dur: 4 + (i % 5),
  emoji: HEARTS[i % HEARTS.length],
  size: 18 + (i % 4) * 6,
}))

function dismissed() { try { return localStorage.getItem('cl_egg_' + dayKey) === '1' } catch { return false } }
function dismiss() { try { localStorage.setItem('cl_egg_' + dayKey, '1') } catch { /* ignore */ } visible.value = false }

async function check() {
  if (dismissed()) return
  // 未登录（无访问令牌）不打接口，避免登录页出现 401 噪声
  let token: string | null = null
  try { token = localStorage.getItem('cl_at') } catch { /* ignore */ }
  if (!token) return

  let annivName: string | null = null
  try {
    const res = await listAnniversaries(1, 50)
    const hit = res.items.find(a => a.daysLeft === 0)
    if (hit) annivName = hit.name
  } catch { /* 网络异常则静默，不打扰 */ }

  const f = FESTIVALS.find(x => x.mmdd === todayMMDD)
  if (annivName) {
    title.value = `🎉 今天是你们的「${annivName}」！`
    subtitle.value = '愿这一天被温柔填满'
  } else if (f) {
    title.value = `🎉 ${f.label}快乐！`
    subtitle.value = '今天也要好好相爱呀'
  }
  if (title.value) visible.value = true
}

onMounted(check)
</script>

<template>
  <transition name="egg-fade">
    <div v-if="visible" class="egg-mask" :class="{ 'no-anim': setting.reduceMotion }" role="button" tabindex="0" aria-label="关闭" @click.self="dismiss" @keydown.enter.prevent="dismiss" @keydown.space.prevent="dismiss">
      <div class="egg-card">
        <div class="egg-hearts" aria-hidden="true">
          <span
            v-for="(h, i) in hearts"
            :key="i"
            class="egg-heart"
            :style="{
              left: h.left + '%',
              animationDelay: h.delay + 's',
              animationDuration: h.dur + 's',
              fontSize: h.size + 'px',
            }"
          >{{ h.emoji }}</span>
        </div>
        <div class="egg-body">
          <div class="egg-title">{{ title }}</div>
          <div class="egg-sub">{{ subtitle }}</div>
          <button class="egg-btn" @click="() => dismiss()">开心收下 💞</button>
        </div>
      </div>
    </div>
  </transition>
</template>

<style scoped>
.egg-mask {
  position: fixed;
  inset: 0;
  z-index: 2000;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(40, 30, 35, 0.28);
  backdrop-filter: blur(2px);
}
.egg-card {
  position: relative;
  overflow: hidden;
  width: min(86vw, 360px);
  padding: 30px 26px 26px;
  border-radius: 22px;
  text-align: center;
  background: var(--color-surface);
  box-shadow: 0 24px 70px rgba(122, 100, 98, 0.28);
  border: 1px solid var(--color-ink-soft);
}
.egg-hearts {
  position: absolute;
  inset: 0;
  pointer-events: none;
  overflow: hidden;
}
.egg-heart {
  position: absolute;
  top: -12%;
  opacity: 0;
  animation-name: egg-fall;
  animation-timing-function: linear;
  animation-iteration-count: infinite;
}
.egg-body { position: relative; z-index: 1; }
.egg-title { font-size: 19px; font-weight: 700; color: var(--color-ink-1); line-height: 1.5; }
.egg-sub { margin-top: 10px; font-size: 13px; color: var(--color-ink-3); }
.egg-btn {
  margin-top: 20px;
  padding: 10px 22px;
  border: none;
  border-radius: 999px;
  cursor: pointer;
  font-size: 14px;
  color: #fff;
  background: var(--color-rose, #e06a8b);
  box-shadow: 0 8px 20px rgba(224, 106, 139, 0.35);
  transition: transform 0.15s var(--ease-love, ease);
}
.egg-btn:hover { transform: translateY(-1px) scale(1.02); }
.no-anim .egg-heart { animation: none !important; opacity: 0.5; }
@keyframes egg-fall {
  0% { transform: translateY(-12%) rotate(0deg); opacity: 0; }
  10% { opacity: 1; }
  100% { transform: translateY(120vh) rotate(360deg); opacity: 0; }
}
.egg-fade-enter-active,
.egg-fade-leave-active { transition: opacity 0.3s ease; }
.egg-fade-enter-from,
.egg-fade-leave-to { opacity: 0; }
</style>
