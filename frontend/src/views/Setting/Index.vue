<template>
  <div v-if="loading" class="skeleton">加载中…</div>
  <div v-else class="set-page">
    <!-- 帐号 -->
    <section class="block love-card">
      <h2>帐号资料</h2>
      <NForm ref="formRef" :model="form" :rules="profileRules" label-placement="top">
        <NFormItem label="昵称" path="nickName">
          <NInput v-model:value="form.nickName" placeholder="昵称" />
        </NFormItem>
        <NFormItem label="头像">
          <ImageField v-model="form.avatar" :size="96" />
        </NFormItem>
        <NFormItem label="原密码（仅修改密码时填）">
          <NInput v-model:value="form.oldPassword" type="password" placeholder="原密码" show-password-on="click" />
        </NFormItem>
        <NFormItem label="新密码">
          <NInput v-model:value="form.newPassword" type="password" placeholder="新密码" show-password-on="click" />
        </NFormItem>
        <NButton type="primary" :loading="saving" @click="saveProfile">保存修改</NButton>
      </NForm>
    </section>

    <!-- 我们的专属（共享） -->
    <section class="block love-card">
      <h2>我们的专属（双方共享）</h2>
      <p class="sub-text">相恋纪念日 / 情侣昵称由任一方设置，双方首页与设置同步生效。</p>
      <NForm label-placement="top">
        <NFormItem label="相恋纪念日">
          <input type="date" v-model="loveStartInput" class="native-date" :max="todayStr" />
          <p class="lock-note">
            {{ coupleSetting?.loveStartTime
              ? `已设为 ${coupleSetting.loveStartTime.slice(0, 10)} · 修改后首页「在一起多少天」会同步更新`
              : '设置后首页会显示「在一起多少天」，双方实时同步' }}
          </p>
        </NFormItem>
        <NFormItem label="情侣昵称">
          <NInput v-model:value="coupleName" placeholder="例如：小爱与阿攀" />
        </NFormItem>
      </NForm>
      <NButton type="primary" :loading="savingCS" @click="saveCouple">保存共同信息</NButton>
    </section>

    <!-- TA 的绑定（双向同步） -->
    <section class="block love-card">
      <h2>TA 的绑定 · 双向同步</h2>
      <p class="sub-text">绑定后，你和 TA 即组成一对专属情侣，首页、相册、日记、纪念日等所有数据仅你们两人实时互通。</p>

      <!-- 已绑定 -->
      <div v-if="partner.status?.isBound && partner.status.partner" class="bind-card">
        <div class="bind-ava">{{ (partner.status.partner.avatar ? '' : (partner.status.partner.nickName || '?').slice(0, 1)) }}</div>
        <div class="bind-meta">
          <div class="bind-name">已与 <b>{{ partner.status.partner.nickName }}</b> 绑定</div>
          <div class="bind-sub">你们的数据已双向同步</div>
        </div>
        <NPopconfirm
          @positive-click="doUnbind"
          positive-text="解除"
          negative-text="再想想"
        >
          <template #trigger>
            <NButton size="small" tertiary type="error" :loading="ui.unbinding">解除绑定</NButton>
          </template>
          确定解除与 TA 的绑定吗？你们共同的恋爱数据仍会保留。
        </NPopconfirm>
      </div>

      <!-- 未绑定 -->
      <div v-else class="bind-unbound">
        <div class="bind-tabs">
          <button :class="{ on: ui.mode === 'invite' }" @click="ui.mode = 'invite'">我生成邀请码</button>
          <button :class="{ on: ui.mode === 'join' }" @click="ui.mode = 'join'">我输入邀请码</button>
        </div>

        <!-- 生成邀请码 -->
        <div v-if="ui.mode === 'invite'">
          <template v-if="ui.inviteCode">
            <div class="invite-code">{{ ui.inviteCode }}</div>
            <div class="invite-tip">
              把邀请码发给 TA，TA 在「设置 → TA 的绑定」里输入即可{{ ui.countdownText }}。
            </div>
            <div class="invite-actions">
              <NButton size="small" @click="copyCode">复制邀请码</NButton>
              <NButton size="small" quaternary @click="resetInvite">重新生成</NButton>
            </div>
          </template>
          <NButton v-else type="primary" :loading="ui.inviting" @click="doInvite">生成邀请码</NButton>
        </div>

        <!-- 输入邀请码 -->
        <div v-else class="join-box">
          <NInput v-model:value="ui.joinCode" placeholder="输入 TA 发来的 6 位邀请码" maxlength="6" />
          <NButton type="primary" :loading="ui.joining" :disabled="!ui.joinCode" @click="doJoin">加入并绑定</NButton>
        </div>
      </div>
    </section>

    <!-- 主题 -->
    <section class="block love-card">
      <h2>主题与动效</h2>
      <div class="set-row">
        <span>深色模式</span>
        <NSwitch :value="setting.dark" @update:value="setting.toggleDark()" />
      </div>
      <div class="set-row">
        <span>减少动效</span>
        <NSwitch :value="setting.reduceMotion" @update:value="setting.toggleMotion()" />
      </div>
    </section>

    <!-- 数据备份 -->
    <section class="block love-card">
      <h2>数据备份</h2>
      <p class="sub-text">导出当前账号可见的全部数据（纪念日 / 日记 / 愿望 / 矛盾 / 书信 / 记账 / 约会 / 消息）。</p>
      <NButton :loading="exporting" @click="doExport">导出全部数据</NButton>
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, onUnmounted } from 'vue';
import { useMessage } from 'naive-ui';
import { NForm, NFormItem, NInput, NButton, NSwitch, NPopconfirm } from 'naive-ui';
import { updateProfile, exportAll } from '@/api/user';
import * as coupleApi from '@/api/couple';
import * as partnerApi from '@/api/partner';
import type { ApiResult, CoupleSetting, BindStatus, InviteResp } from '@/types';
import { useAuthStore } from '@/store/authStore';
import { useSettingStore } from '@/store/settingStore';
import { usePartnerStore } from '@/store/partnerStore';
import { useRealtime } from '@/composables/useRealtime';
import { feedback } from '@/utils/feedback';
import ImageField from '@/components/Common/ImageField.vue';
import { maxLenRule } from '@/utils/formRules';
import type { FormItemRule } from 'naive-ui';

const auth = useAuthStore();
const setting = useSettingStore();
const partner = usePartnerStore();
const msg = useMessage();
const { onSync } = useRealtime();

const loading = ref(false);
const saving = ref(false);
const exporting = ref(false);
const todayStr = new Date().toLocaleDateString('en-CA'); // YYYY-MM-DD（本地）

// 情侣级共享信息（任一方设置，双方生效）
const coupleSetting = ref<CoupleSetting | null>(null);
const loveStartInput = ref('');
const coupleName = ref('');
const savingCS = ref(false);

// 绑定 UI 状态
const ui = reactive({
  mode: 'invite' as 'invite' | 'join',
  inviting: false,
  inviteCode: '',
  expireAt: 0,
  countdownText: '',
  joining: false,
  joinCode: '',
  unbinding: false,
  timer: undefined as number | undefined,
});

async function reloadCoupleSetting() {
  try {
    const { data } = await coupleApi.getCoupleSetting();
    const s = (data as ApiResult<CoupleSetting>).data;
    coupleSetting.value = s;
    loveStartInput.value = s?.loveStartTime ? s.loveStartTime.slice(0, 10) : '';
    coupleName.value = s?.coupleName ?? '';
  } catch { /* 忽略 */ }
}

async function saveCouple() {
  savingCS.value = true;
  try {
    await coupleApi.updateCoupleSetting({
      coupleName: coupleName.value || undefined,
      loveStartTime: loveStartInput.value || undefined,
    });
    await reloadCoupleSetting();
    msg.success(loveStartInput.value ? '已保存，恋爱时长已更新' : '已保存你们的共同信息');
  } catch {
    /* 拦截器已提示 */
  } finally {
    savingCS.value = false;
  }
}

async function doInvite() {
  ui.inviting = true;
  try {
    const { data } = await partnerApi.createInvite();
    const r = (data as ApiResult<InviteResp>).data;
    ui.inviteCode = r.code;
    ui.expireAt = new Date(r.expiresAt).getTime();
    startCountdown();
    msg.success('邀请码已生成，发给 TA 吧');
  } catch {
    /* 拦截器已提示 */
  } finally {
    ui.inviting = false;
  }
}
function startCountdown() {
  const tick = () => {
    const left = Math.max(0, ui.expireAt - Date.now());
    const m = Math.floor(left / 60000);
    const s = Math.floor((left % 60000) / 1000);
    ui.countdownText = left > 0 ? `（${m}:${String(s).padStart(2, '0')} 后过期）` : '（已过期，请重新生成）';
    if (left > 0) ui.timer = window.setTimeout(tick, 1000);
  };
  tick();
}
function resetInvite() {
  ui.inviteCode = '';
  ui.countdownText = '';
  if (ui.timer) window.clearTimeout(ui.timer);
}
async function copyCode() {
  try {
    await navigator.clipboard.writeText(ui.inviteCode);
    feedback.copied('邀请码已复制');
  } catch {
    msg.warning('复制失败，请手动选择文本');
  }
}
async function doJoin() {
  if (!ui.joinCode) return;
  ui.joining = true;
  try {
    await partnerApi.joinPartner(ui.joinCode.trim().toUpperCase());
    await partner.load();
    ui.joinCode = '';
    feedback.bound();
  } catch {
    /* 拦截器已提示 */
  } finally {
    ui.joining = false;
  }
}
async function doUnbind() {
  ui.unbinding = true;
  try {
    await partnerApi.unbindPartner();
    await partner.load();
    resetInvite();
    ui.joinCode = '';
    feedback.unbound();
  } catch {
    /* 拦截器已提示 */
  } finally {
    ui.unbinding = false;
  }
}

const form = reactive({
  nickName: '',
  avatar: '',
  oldPassword: '',
  newPassword: '',
});
const formRef = ref<InstanceType<typeof NForm>>();
const profileRules: { nickName: FormItemRule[] } = {
  nickName: [maxLenRule(20, '昵称最多 20 个字哦')],
};

onMounted(async () => {
  if (auth.profile) {
    form.nickName = auth.profile.nickName;
    form.avatar = auth.profile.avatar ?? '';
  }
  try {
    const { data } = await coupleApi.getCoupleSetting();
    const s = (data as ApiResult<CoupleSetting>).data;
    coupleSetting.value = s;
    loveStartInput.value = s?.loveStartTime ? s.loveStartTime.slice(0, 10) : '';
    coupleName.value = s?.coupleName ?? '';
  } catch { /* 忽略 */ }
  await partner.load();
  // 实时：对方绑定/解绑 → 刷新绑定状态；共享信息（恋爱纪念日/昵称）被修改 → 刷新
  onSync('partner', () => partner.load());
  onSync('setting', reloadCoupleSetting);
});
onUnmounted(() => {
  if (ui.timer) window.clearTimeout(ui.timer);
});

async function saveProfile() {
  try {
    await formRef.value?.validate();
  } catch {
    return;
  }
  if (form.oldPassword && !form.newPassword) {
    msg.warning('填写了原密码就需要同时填写新密码');
    return;
  }
  saving.value = true;
  try {
    const updated = await updateProfile({
      nickName: form.nickName || undefined,
      avatar: form.avatar || undefined,
      oldPassword: form.oldPassword || undefined,
      newPassword: form.newPassword || undefined,
    });
    auth.profile = updated;
    form.oldPassword = '';
    form.newPassword = '';
    feedback.updated('资料');
  } catch {
    /* 错误已由响应拦截器提示 */
  } finally {
    saving.value = false;
  }
}

async function doExport() {
  exporting.value = true;
  try {
    const resp = await exportAll();
    feedback.exported(resp.fileName);
  } catch {
    /* 错误已由响应拦截器提示 */
  } finally {
    exporting.value = false;
  }
}
</script>

<style scoped>
.set-page { max-width: 720px; margin: 0 auto; }
.block { margin-bottom: 18px; }
.block h2 { font-size: 16px; margin: 0 0 14px; }
.set-row { display: flex; align-items: center; justify-content: space-between; padding: 10px 0; }
.set-row + .set-row { border-top: 1px solid var(--color-ink-soft); }
.native-date {
  padding: 8px 10px; border-radius: var(--radius-md); border: 1px solid var(--color-border);
  background: var(--color-surface); color: var(--color-ink); font-size: 14px; width: 100%;
}
.lock-note { color: var(--color-ink-3); font-size: 12px; margin: 6px 0 0; line-height: 1.5; }

/* 绑定块 */
.bind-card {
  display: flex; align-items: center; gap: 14px;
  padding: 14px; border-radius: var(--radius-lg);
  background: var(--color-surface); border: 1px solid var(--color-border);
  box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04), 0 10px 28px -10px rgba(122, 100, 98, 0.16);
}
.bind-ava {
  width: 46px; height: 46px; border-radius: 50%; flex: 0 0 auto;
  display: flex; align-items: center; justify-content: center;
  background: var(--color-accent); color: #fff; font-size: 20px; font-weight: 700;
}
.bind-meta { flex: 1; }
.bind-name { font-size: 15px; }
.bind-sub { font-size: 12px; color: var(--color-ink-3); margin-top: 2px; }

.bind-unbound { padding-top: 6px; }
.bind-tabs { display: flex; gap: 8px; margin-bottom: 12px; }
.bind-tabs button {
  flex: 1; padding: 10px; border-radius: var(--radius-md); cursor: pointer;
  border: 1px solid var(--color-border); background: var(--color-surface); color: var(--color-ink);
  font-size: 14px; transition: all .2s;
}
.bind-tabs button.on { background: var(--color-accent); color: #fff; border-color: var(--color-accent); }

.invite-code {
  font-size: 34px; letter-spacing: 8px; font-weight: 800; text-align: center;
  padding: 16px; border-radius: var(--radius-lg); color: var(--color-rose);
  background: var(--color-surface-2); border: 1px dashed var(--color-border);
}
.invite-tip { font-size: 12px; color: var(--color-ink-3); text-align: center; margin: 10px 0; }
.invite-actions { display: flex; gap: 10px; justify-content: center; }

.join-box { display: flex; gap: 10px; }
.join-box :deep(.n-input) { flex: 1; }
</style>
