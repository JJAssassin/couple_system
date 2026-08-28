<template>
  <IndSkeleton v-if="loading" variant="text" :rows="4" />
  <div v-else class="set-page">
    <header class="page-head"><h1>设置</h1></header>
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
        <NButton type="primary" :loading="saving" v-press-bounce @click="saveProfile">保存修改</NButton>
      </NForm>
    </section>

    <!-- 我们的专属（共享） -->
    <section class="block love-card">
      <h2>我们的专属（双方共享）</h2>
      <p class="sub-text">相恋纪念日 / 情侣昵称由任一方设置，双方首页与设置同步生效。</p>
      <NForm label-placement="top">
        <NFormItem label="相恋纪念日">
          <input type="date" v-model="loveStartInput" class="native-date" :max="todayStr" aria-label="相恋纪念日" />
          <p class="lock-note">
            {{ coupleSetting?.loveStartTime
              ? `已设为 ${coupleSetting.loveStartTime.slice(0, 10)}${coupleSetting.lunarLoveStart ? '（' + coupleSetting.lunarLoveStart + '）' : ''} · 修改后首页「在一起多少天」会同步更新`
              : '设置后首页会显示「在一起多少天」，双方实时同步' }}
          </p>
        </NFormItem>
        <NFormItem label="情侣昵称">
          <NInput v-model:value="coupleName" placeholder="例如：小爱与阿攀" />
        </NFormItem>
      </NForm>
      <NButton type="primary" :loading="savingCS" v-press-bounce @click="saveCouple">保存共同信息</NButton>
    </section>

    <!-- TA 的绑定（双向同步） -->
    <section class="block love-card bind-block">
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
            <NButton size="small" tertiary type="error" v-press-bounce :loading="ui.unbinding">解除绑定</NButton>
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
              <NButton size="small" v-press-bounce @click="copyCode">复制邀请码</NButton>
              <NButton size="small" quaternary @click="resetInvite">重新生成</NButton>
            </div>
          </template>
          <NButton v-else type="primary" :loading="ui.inviting" v-press-bounce @click="doInvite">生成邀请码</NButton>
        </div>

        <!-- 输入邀请码 -->
        <div v-else class="join-box">
          <NInput v-model:value="ui.joinCode" placeholder="输入 TA 发来的 6 位邀请码" maxlength="6" />
          <NButton type="primary" v-press-bounce :loading="ui.joining" :disabled="!ui.joinCode" @click="doJoin">加入并绑定</NButton>
        </div>
      </div>
    </section>

    <!-- 主题 -->
    <section class="block love-card">
      <h2>主题与动效</h2>
      <div class="set-row set-row-top">
        <span>外观主题</span>
        <div class="theme-seg">
          <button :class="{ on: setting.mode === 'light' }" @click="setting.setMode('light')">浅色</button>
          <button :class="{ on: setting.mode === 'dark' }" @click="setting.setMode('dark')">深色</button>
          <button :class="{ on: setting.mode === 'system' }" @click="setting.setMode('system')">跟随系统</button>
        </div>
      </div>
      <p v-if="setting.mode === 'system'" class="theme-hint">
        当前按系统设置：{{ setting.dark ? '深色' : '浅色' }}
      </p>
      <div class="set-row">
        <span>减少动效</span>
        <FinesseSwitch :model-value="setting.reduceMotion" label="减少动效" @update:model-value="(v: boolean) => setting.setReduceMotion(v)" />
      </div>
      <div class="set-row">
        <span>主题色</span>
        <div class="swatches">
          <button
            v-for="(a, key) in ACCENTS"
            :key="key"
            class="sw"
            :class="{ on: setting.accent === key }"
            :style="{ background: a.p }"
            :title="`${a.label} · ${a.desc}`"
            :aria-label="a.label"
            @click="setting.setAccent(key)"
          />
        </div>
      </div>
      <p v-if="setting.accent !== 'rose'" class="theme-hint">
        当前主题色：{{ ACCENTS[setting.accent]?.label }}（双方各自设置，互不强制）
      </p>
    </section>

    <!-- 消息通知 -->
    <section class="block love-card">
      <h2>消息通知</h2>
      <p class="sub-text">开启后，App 在后台时收到 TA 的新消息或动态，会以系统通知提醒你（需先授权通知权限）。</p>
      <div class="set-row">
        <span>系统通知</span>
        <FinesseSwitch :model-value="setting.notifications" :disabled="!notifySupported" label="系统通知" @update:model-value="onToggleNotify" />
      </div>
      <p v-if="!notifySupported" class="theme-hint">
        当前浏览器不支持系统通知；可在手机浏览器「添加到主屏」获得类 App 体验。
      </p>
      <p v-else-if="notifyDenied" class="theme-hint">
        通知权限已被浏览器拒绝，请在站点设置中允许通知后重试。
      </p>
      <p v-else-if="isIOS" class="theme-hint">
        iOS 请在 Safari 中点击「分享 → 添加到主屏幕」安装；通知权限在首次安装后于系统设置中开启。
      </p>
    </section>

    <!-- 数据备份 -->
    <section class="block love-card">
      <h2>数据备份</h2>
      <p class="sub-text">导出当前账号可见的全部数据（纪念日 / 日记 / 愿望 / 矛盾 / 留言 / 记账 / 约会 / 消息）。</p>
      <NButton :loading="exporting" v-press-bounce @click="doExport">导出全部数据</NButton>
      <NButton class="import-ml" :loading="importing" @click="openImport">导入备份</NButton>
    </section>

    <NModal v-model:show="showImport" class="import-modal" preset="card" title="导入备份" style="max-width: 460px">
      <div v-if="!preview">
        <p class="sub-text">选择此前从「导出全部数据」得到的 .zip 或 .json 备份文件。导入将按导出对称范围覆盖当前账号的数据（纪念日 / 日记 / 愿望 / 矛盾 / 记账 / 约会 / 消息）。</p>
        <input class="file-input" type="file" accept=".zip,.json" @change="onFileChosen" />
        <div class="modal-foot">
          <NButton @click="showImport = false">取消</NButton>
          <NButton type="primary" :disabled="!importFile" :loading="importing" @click="doPreview">解析预览</NButton>
        </div>
      </div>
      <div v-else>
        <p class="sub-text">{{ previewMsg }}</p>
        <ul class="import-counts">
          <li>纪念日：{{ previewCounts.anniversaries }}</li>
          <li>日记：{{ previewCounts.diaries }}</li>
          <li>愿望：{{ previewCounts.wishes }}</li>
          <li>矛盾记录：{{ previewCounts.conflicts }}</li>
          <li>记账：{{ previewCounts.accountRecords }}</li>
          <li>约会：{{ previewCounts.dateRecords }}</li>
          <li>消息：{{ previewCounts.systemMessages }}</li>
          <li><b>合计：{{ previewCounts.total ?? sumPreview }}</b></li>
        </ul>
        <div class="modal-foot">
          <NButton @click="resetImport">上一步</NButton>
          <NButton type="warning" :loading="committing" @click="doCommit">确认覆盖导入</NButton>
        </div>
      </div>
    </NModal>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue';
import { useMessage } from 'naive-ui';
import { NForm, NFormItem, NInput, NButton, NPopconfirm, NModal } from 'naive-ui';
import { updateProfile, exportAll, importBackupPreview, importBackupCommit } from '@/api/user';
import * as coupleApi from '@/api/couple';
import * as partnerApi from '@/api/partner';
import type { ApiResult, CoupleSetting, BindStatus, InviteResp, ImportCounts } from '@/types';
import { useAuthStore } from '@/store/authStore';
import { useSettingStore, ACCENTS } from '@/store/settingStore';
import { usePartnerStore } from '@/store/partnerStore';
import { useRealtime } from '@/composables/useRealtime';
import { usePwa, notificationsSupported } from '@/composables/usePwa';
import { feedback } from '@/utils/feedback';
import ImageField from '@/components/Common/ImageField.vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import { FinesseSwitch } from '@/interactions';
import { maxLenRule } from '@/utils/formRules';
import type { FormItemRule } from 'naive-ui';

const auth = useAuthStore();
const setting = useSettingStore();
const partner = usePartnerStore();
const msg = useMessage();
const { onSync, rehandshake } = useRealtime();
const { requestNotificationPermission } = usePwa();
const notifySupported = notificationsSupported();
const notifyDenied = ref(typeof Notification !== 'undefined' && Notification.permission === 'denied');
const isIOS = ref(/iP(hone|od|ad)/.test(navigator.userAgent) || (navigator.userAgent.includes('Mac') && 'ontouchend' in document));

const loading = ref(false);
const saving = ref(false);
const exporting = ref(false);
const todayStr = toDateStr(); // YYYY-MM-DD（本地，时区稳定）

// 本地时区稳定的 YYYY-MM-DD，避免 toLocaleDateString 区域/时区漂移（与首页同款）
function toDateStr(d: Date = new Date()): string {
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}

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
    // 令牌 cid 已随绑定更新，重握手让实时推送落到新情侣组（否则仍留在 anon 组收不到对方实时更新）
    await rehandshake();
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
    await rehandshake();
    feedback.unbound();
  } catch {
    /* 拦截器已提示 */
  } finally {
    ui.unbinding = false;
  }
}

// 对方触发绑定/解绑：对方已拿到重签令牌，本方需自行用 refresh 重签（实时信号里不含令牌），
// 再用新 cid 重新握手，确保实时推送落到正确情侣组。
async function onPartnerSignal() {
  await partner.load();
  try {
    await auth.restoreSession();
  } catch {
    /* 刷新失败不阻断，下次请求 401 拦截器会兜底 */
  }
  await rehandshake();
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
  // 实时：对方绑定/解绑 → 刷新绑定状态并重签令牌/重握手；共享信息（恋爱纪念日/昵称）被修改 → 刷新
  onSync('partner', onPartnerSignal);
  onSync('setting', reloadCoupleSetting);
});
onUnmounted(() => {
  if (ui.timer) window.clearTimeout(ui.timer);
});

async function onToggleNotify(v: boolean) {
  if (!v) {
    setting.setNotifications(false);
    return;
  }
  const perm = await requestNotificationPermission();
  if (perm === 'granted') {
    setting.setNotifications(true);
    msg.success('已开启系统通知');
  } else {
    setting.setNotifications(false);
    notifyDenied.value = perm === 'denied';
    msg.warning(perm === 'denied' ? '通知权限被拒绝，请在浏览器设置中开启' : '未获得通知授权');
  }
}

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
    feedback.exported(`couple_export.zip（含 ${resp.mediaCount ?? 0} 个媒体文件）`);
  } catch {
    msg.error('导出下载失败，请稍后重试');
  } finally {
    exporting.value = false;
  }
}

// —— 全量备份导入（与导出配对）——
const showImport = ref(false);
const importing = ref(false);
const committing = ref(false);
const preview = ref(false);
const importFile = ref<File | null>(null);
const previewMsg = ref('');
const previewCounts = ref<ImportCounts>({
  anniversaries: 0, diaries: 0, wishes: 0, conflicts: 0,
  accountRecords: 0, dateRecords: 0, systemMessages: 0,
});
const sumPreview = computed(() =>
  previewCounts.value.anniversaries + previewCounts.value.diaries + previewCounts.value.wishes +
  previewCounts.value.conflicts + previewCounts.value.accountRecords +
  previewCounts.value.dateRecords + previewCounts.value.systemMessages);

function openImport() { resetImport(); showImport.value = true; }
function resetImport() { preview.value = false; importFile.value = null; previewMsg.value = ''; }
function onFileChosen(e: Event) {
  const t = e.target as HTMLInputElement;
  importFile.value = t.files && t.files.length ? t.files[0] : null;
}
async function doPreview() {
  if (!importFile.value) return;
  importing.value = true;
  try {
    const r = await importBackupPreview(importFile.value);
    if (r.valid) {
      preview.value = true;
      previewMsg.value = r.message;
      previewCounts.value = r.counts;
    } else {
      msg.error(r.message);
    }
  } catch {
    msg.error('解析失败，请确认文件为有效的备份');
  } finally {
    importing.value = false;
  }
}
async function doCommit() {
  if (!importFile.value) return;
  committing.value = true;
  try {
    const r = await importBackupCommit(importFile.value);
    feedback.imported(r.importedTotal, 0, 0);
    if (r.warnings && r.warnings.length) msg.warning(r.warnings.join('；'));
    showImport.value = false;
  } catch {
    msg.error('导入失败，请稍后重试');
  } finally {
    committing.value = false;
  }
}
</script>

<style scoped>
.set-page { max-width: 720px; margin: 0 auto; padding-top: 4px; }
.page-head { margin-bottom: 18px; }
.page-head h1 { margin: 0; font-size: 22px; font-weight: 800; color: var(--color-ink); }
.block { margin-bottom: 18px; }
.block h2 { font-size: 16px; margin: 0 0 14px; }
.set-row { display: flex; align-items: center; justify-content: space-between; padding: 10px 0; }
.set-row + .set-row { border-top: 1px solid var(--color-ink-soft); }
.set-row-top { align-items: flex-start; }
.theme-seg { display: inline-flex; gap: 6px; flex-wrap: wrap; justify-content: flex-end; }
.theme-seg button {
  padding: 7px 14px; border-radius: var(--radius-md); cursor: pointer; font-size: 13px; font-weight: 600;
  border: 1px solid var(--color-border); background: var(--color-surface); color: var(--color-ink-2);
  transition: all var(--dur-micro) var(--ease-love);
}
.theme-seg button.on {
  background: var(--color-rose); color: #fff; border-color: var(--color-rose);
  box-shadow: 0 4px 14px -4px rgba(255, 111, 125, 0.5);
}
.theme-seg button:not(.on):hover { color: var(--color-rose); border-color: var(--color-rose-soft); background: var(--color-rose-soft); }
.theme-hint { font-size: 12px; color: var(--color-ink-3); margin: -4px 0 8px; text-align: right; }
.swatches { display: inline-flex; gap: 8px; flex-wrap: wrap; justify-content: flex-end; }
.sw {
  width: 26px; height: 26px; border-radius: 999px; cursor: pointer; padding: 0;
  border: 2px solid var(--color-surface); box-shadow: 0 0 0 1px var(--color-border);
  transition: transform var(--dur-micro) var(--ease-love);
}
.sw:hover { transform: scale(1.12); }
.sw.on { box-shadow: 0 0 0 2px var(--color-surface), 0 0 0 4px var(--color-rose); }
.native-date {
  padding: 8px 10px; border-radius: var(--radius-md); border: 1px solid var(--color-border);
  background: var(--color-surface); color: var(--color-ink); font-size: 14px; width: 100%;
}
.lock-note { color: var(--color-ink-3); font-size: 12px; margin: 6px 0 0; line-height: 1.5; }

/* 绑定块（关系核心，视觉重点） */
.bind-block {
  border-left: 3px solid var(--color-rose);
  background: var(--color-rose-soft);
}
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

.import-ml { margin-left: 8px; }
</style>

<style>
.import-modal .sub-text { margin: 0 0 12px; }
.import-modal .file-input { display: block; width: 100%; margin: 8px 0 4px; }
.import-modal .modal-foot { display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; }
.import-modal .import-counts { margin: 10px 0; padding-left: 20px; line-height: 1.95; }
</style>
