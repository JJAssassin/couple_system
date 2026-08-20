<template>
  <div class="login-wrap">
    <div class="login-card">
      <h1>我们的小世界</h1>
      <p class="sub-text">登录，开启专属回忆</p>
      <form @submit.prevent="onSubmit">
        <label>账号</label>
        <input v-model="userName" placeholder="partner_a / partner_b" autocomplete="username" />
        <label>密码</label>
        <input v-model="password" type="password" placeholder="默认 123456" autocomplete="current-password" />
        <button type="submit" :disabled="loading">{{ loading ? '登录中…' : '登 录' }}</button>
      </form>
      <p class="hint">初始账号 partner_a / partner_b，密码 123456</p>
    </div>
  </div>
</template>
<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/store/authStore';
import { useNotifyStore } from '@/store/notifyStore';
import { feedback } from '@/utils/feedback';

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
.login-wrap { min-height: 100dvh; display: flex; align-items: center; justify-content: center;
  background: linear-gradient(135deg, var(--color-cream), var(--color-mist)); padding: 16px; }
.login-card { width: 340px; background: var(--color-surface); border-radius: 20px; padding: 32px 28px;
  box-shadow: 0 20px 60px rgba(122,100,98,.12); text-align: center; border: 1px solid var(--color-border); }
h1 { color: var(--color-rose); font-size: 22px; margin: 0 0 4px; }
label { display: block; text-align: left; font-size: 13px; color: var(--color-ink-2); margin: 16px 0 6px; }
input { width: 100%; box-sizing: border-box; padding: 11px 12px; border: 1px solid var(--color-border);
  border-radius: 10px; font-size: 14px; outline: none; background: var(--color-surface-2); color: var(--color-ink);
  transition: border-color var(--dur-micro) var(--ease-love); }
input:focus { border-color: var(--color-rose); }
button { width: 100%; margin-top: 22px; padding: 12px; border: none; border-radius: 10px;
  background: var(--color-rose); color: #fff; font-size: 15px; cursor: pointer; transition: opacity var(--dur-micro); }
button:disabled { opacity: .6; cursor: default; }
.hint { margin-top: 16px; font-size: 12px; color: var(--color-ink-3); }
</style>
