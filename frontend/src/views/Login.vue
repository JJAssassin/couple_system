<template>
  <div class="login-wrap">
    <!-- 极淡氛围光，呼应全站浪漫柔光语言 -->
    <div class="login-aurora" aria-hidden="true" />
    <div class="login-card">
      <div class="brand-mark"><Heart :size="20" :stroke-width="1.8" /></div>
      <h1>我们的小世界</h1>
      <p class="sub-text">登录，开启专属回忆</p>
      <form @submit.prevent="onSubmit">
        <label for="login-user">账号</label>
        <span class="uvi-input-draw">
          <input id="login-user" v-model="userName" placeholder="partner_a / partner_b" autocomplete="username" />
        </span>
        <label for="login-pass">密码</label>
        <span class="uvi-input-draw">
          <input id="login-pass" v-model="password" type="password" placeholder="默认 123456" autocomplete="current-password" />
        </span>
        <GlowButton type="submit" class="login-submit" :disabled="loading">{{ loading ? '登录中…' : '登 录' }}</GlowButton>
      </form>
      <p class="hint">初始账号 partner_a / partner_b，密码 123456</p>
    </div>
  </div>
</template>
<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { Heart } from 'lucide-vue-next';
import { useAuthStore } from '@/store/authStore';
import { useNotifyStore } from '@/store/notifyStore';
import { feedback } from '@/utils/feedback';
import GlowButton from '@/components/Common/GlowButton.vue';

const router = useRouter();
const auth = useAuthStore();
const notify = useNotifyStore();

const userName = ref('partner_a');
const password = ref('123456');
const loading = ref(false);

async function onSubmit() {
  if (!userName.value || !password.value) {
    feedback.error('请输入账号和密码');
    return;
  }
  loading.value = true;
  try {
    await auth.login(userName.value, password.value);
    notify.success('登录成功');
    router.push('/home');
  } catch {
    // 错误已由 request 拦截器提示
  } finally {
    loading.value = false;
  }
}
</script>
<style scoped>
.login-wrap { position: relative; min-height: 100dvh; display: flex;
  background: linear-gradient(135deg, var(--color-cream), var(--color-mist));
  padding: calc(env(safe-area-inset-top) + 16px) 16px calc(env(safe-area-inset-bottom) + 16px);
  overflow-x: hidden; overflow-y: auto; -webkit-overflow-scrolling: touch; }
.login-aurora { position: absolute; inset: -20% -10% auto -10%; height: 60vh; pointer-events: none;
  background:
    radial-gradient(50% 100% at 28% 0%, color-mix(in srgb, var(--color-rose) 16%, transparent), transparent 70%),
    radial-gradient(46% 100% at 78% 12%, color-mix(in srgb, var(--color-cocoa) 12%, transparent), transparent 70%);
  filter: blur(18px); opacity: .9; }
.login-card {
  position: relative; z-index: 1; margin: auto; width: min(340px, 100%); background: var(--color-surface); border-radius: 20px; padding: 36px 28px 30px;
  box-shadow: 0 20px 60px rgba(122,100,98,.12); text-align: center; border: 1px solid var(--color-border);
  animation: login-pop var(--dur-page) var(--ease-love) both;
}
@keyframes login-pop { from { opacity: 0; transform: translateY(14px) scale(.97); } to { opacity: 1; transform: none; } }
.reduce-motion .login-card { animation: none; }
.brand-mark {
  width: 52px; height: 52px; border-radius: 16px; margin: 0 auto 14px; display: grid; place-items: center;
  color: var(--color-on-primary); background: linear-gradient(135deg, var(--color-rose) 0%, var(--color-rose-vivid) 100%);
  box-shadow: 0 8px 22px -8px rgba(255, 111, 125, 0.5);
}
h1 { color: var(--color-ink); font-size: 22px; margin: 0 0 4px; letter-spacing: -0.01em; }
label { display: block; text-align: left; font-size: 13px; color: var(--color-ink-2); margin: 18px 0 6px; }
input { width: 100%; box-sizing: border-box; padding: 11px 12px; border: 1px solid var(--color-border);
  border-radius: 10px; font-size: 14px; outline: none; background: var(--color-surface-2); color: var(--color-ink);
  transition: border-color var(--dur-micro) var(--ease-love), box-shadow var(--dur-micro) var(--ease-love); }
input:focus { border-color: var(--color-rose); box-shadow: 0 0 0 3px var(--color-rose-soft); }
.login-submit { margin-top: 22px; }
.hint { margin-top: 16px; font-size: 12px; color: var(--color-ink-3); }
</style>
