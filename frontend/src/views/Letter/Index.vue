<template>
  <div class="letter-page" ref="container">
    <header class="page-head">
      <h1>悄悄话 & 定时书信</h1>
      <n-tooltip v-if="!receiverId" trigger="hover">
        <template #trigger>
          <n-button type="primary" round disabled>+ 写书信</n-button>
        </template>
        需先在「设置」中绑定对方，才能寄出书信
      </n-tooltip>
      <n-button v-else type="primary" round @click="showForm = true">+ 写书信</n-button>
    </header>

    <n-tabs v-model:value="activeTab" type="segment" class="tabs">
      <n-tab-pane :name="'whisper'" tab="悄悄话" />
      <n-tab-pane :name="'scheduled'" tab="定时书信" />
    </n-tabs>

    <IndSkeleton v-if="loading" variant="list" :rows="6" />
    <IndEmpty
      v-else-if="!visibleLetters.length"
      :title="activeTab === 'whisper' ? '还没有悄悄话' : '还没有定时书信'"
      :desc="activeTab === 'whisper' ? '给 TA 写封悄悄话，把心里话藏进信封里～' : '写一封定时书信，给未来的 TA 一个惊喜'"
    />

      <div v-else class="cards">
        <div
          v-for="l in shownLetters"
          :key="l.id"
          class="love-card letter-card"
          @click="openDetail(l)"
        >
          <LetterItem :letter="l" :current-user-id="currentUserId" />
          <n-popconfirm v-if="l.createUserId === currentUserId" @positive-click="onDelete(l.id)">
            <template #trigger>
              <n-button class="del-btn" size="tiny" tertiary type="error" @click.stop>删除</n-button>
            </template>
            确定删除这封书信吗？
          </n-popconfirm>
        </div>
      </div>

      <IndPager
        v-if="visibleLetters.length"
        mode="more"
        :page="1"
        :page-size="15"
        :total="visibleLetters.length"
        :loading="false"
        :has-more="hasMore"
        @load-more="onLoadMore"
      />

    <!-- 写书信 -->
    <n-modal
      v-model:show="showForm"
      class="letter-modal"
      preset="card"
      title="写一封定时书信"
      style="width: 92%; max-width: 560px;"
    >
      <n-form ref="formRef" :model="form" label-placement="top">
        <n-form-item label="接收人">
          <div class="recv">
            <span class="recv-avatar">{{ partnerName ? partnerName.slice(0, 1) : 'TA' }}</span>
            <span>寄给 <b>{{ partnerName }}</b></span>
          </div>
        </n-form-item>
        <n-form-item label="书信内容" :rule="requiredRule('写点什么吧～')">
          <n-input v-model:value="form.content" type="textarea" placeholder="想对 TA 说的话…" />
        </n-form-item>
        <n-form-item label="配图（可选）">
          <ImageField v-model="form.coverImage" />
        </n-form-item>
        <n-form-item label="解锁时间" :rule="dateRule('选一个解锁时间吧～')">
          <n-date-picker v-model:value="unlockTs" type="datetime" style="width: 100%" />
        </n-form-item>
      </n-form>
      <template #footer>
        <div class="modal-foot">
          <n-button @click="showForm = false">取消</n-button>
          <n-button type="primary" :loading="submitting" @click="submitForm">寄出</n-button>
        </div>
      </template>
    </n-modal>

    <!-- 详情 -->
    <n-drawer v-model:show="showDetail" :width="420" placement="right" class="letter-drawer">
      <n-drawer-content :title="detail?.isUnlocked ? '书信' : '未解锁'">
        <LetterItem v-if="detail" :letter="detail" :current-user-id="currentUserId" />
      </n-drawer-content>
    </n-drawer>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from 'vue';
import {
  NButton, NModal, NDrawer, NDrawerContent, NForm, NFormItem,
  NInput, NDatePicker, NPopconfirm, NTabs, NTabPane, NTooltip,
} from 'naive-ui';
import type { LetterDto, LetterReq } from '@/types';
import { listLetter, getLetter, createLetter, deleteLetter } from '@/api/letter';
import { useAuthStore } from '@/store/authStore';
import { usePartnerStore } from '@/store/partnerStore';
import { useRealtime } from '@/composables/useRealtime';
import { useRouter } from 'vue-router';
import { useStaggerEnter } from '@/composables/useAnimation';
import LetterItem from '@/components/Letter/LetterItem.vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndPager from '@/components/industrial/IndPager.vue';
import ImageField from '@/components/Common/ImageField.vue';
import { feedback } from '@/utils/feedback';
import { requiredRule, dateRule } from '@/utils/formRules';

const auth = useAuthStore();
const partner = usePartnerStore();
const router = useRouter();
const { onSync } = useRealtime();
const loading = ref(true);
const submitting = ref(false);
const container = ref<HTMLElement>();
const formRef = ref();
const currentUserId = computed(() => auth.profile?.id ?? 0);
const receiverId = computed(() => partner.status?.partner?.id ?? null);
const partnerName = computed(() => partner.status?.partner?.nickName ?? '');

const letters = ref<LetterDto[]>([]);
const activeTab = ref<'whisper' | 'scheduled'>('whisper');
const shown = ref(15);

// 悄悄话：已解锁且本人为接收人（可读）；定时书信：全部与自己相关的书信
const visibleLetters = computed(() => {
  if (activeTab.value === 'whisper') {
    return letters.value.filter((l) => l.receiverUserId === currentUserId.value && l.isUnlocked);
  }
  return letters.value;
});
const shownLetters = computed(() => visibleLetters.value.slice(0, shown.value));
const hasMore = computed(() => visibleLetters.value.length > shown.value);

watch(activeTab, () => { shown.value = 15; });

// ---- 写书信 ----
const showForm = ref(false);
const unlockTs = ref<number | null>(null);
const form = reactive<{ content: string; coverImage?: string }>({
  content: '', coverImage: undefined,
});
async function submitForm() {
  if (!receiverId.value) { feedback.needPartner(); router.push('/setting'); return; }
  try {
    await formRef.value?.validate();
  } catch { return; }
  submitting.value = true;
  try {
    const req: LetterReq = {
      receiverUserId: receiverId.value,
      content: form.content,
      coverImage: form.coverImage,
      unlockTime: new Date(unlockTs.value as number).toISOString(),
    };
    await createLetter(req);
    feedback.sended('书信');
    showForm.value = false;
    Object.assign(form, { content: '', coverImage: undefined });
    unlockTs.value = null;
    await load();
  } finally { submitting.value = false; }
}

// ---- 详情 ----
const showDetail = ref(false);
const detail = ref<LetterDto | null>(null);
async function openDetail(l: LetterDto) {
  // 触发后端解锁逻辑（若已到时间），再读取最新状态
  detail.value = await getLetter(l.id);
  showDetail.value = true;
}

async function onDelete(id: number) {
  await deleteLetter(id);
  feedback.deleted('书信');
  await load();
}

async function load() {
  loading.value = true;
  try {
    letters.value = await listLetter();
    shown.value = 15;
  } finally { loading.value = false; }
}

function onLoadMore() {
  shown.value += 15;
}

useStaggerEnter(container, '.love-card', { stagger: 0.06, y: 14 });
onMounted(async () => {
  if (!partner.status) await partner.load();
  await load();
  onSync('letter', () => load());
});
</script>

<style scoped>
.letter-page { max-width: 960px; margin: 0 auto; }
.page-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 14px; }
.page-head h1 { font-size: 22px; margin: 0; }
.tabs { margin-bottom: 18px; }
.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 14px; }
.letter-card { display: flex; flex-direction: column; gap: 10px; }
.del-btn { align-self: flex-end; }
.modal-foot { display: flex; justify-content: flex-end; gap: 10px; }
.recv { display: flex; align-items: center; gap: 10px; padding: 8px 0; }
.recv-avatar {
  width: 34px; height: 34px; border-radius: 50%;
  display: grid; place-items: center;
  background: var(--color-rose); color: #fff; font-size: 14px;
}

@media (max-width: 767px) {
  .cards { grid-template-columns: 1fr; }
}
:global(.letter-modal) { padding: 0 !important; }
:global(.letter-drawer) { max-width: 100vw; }
@media (max-width: 767px) {
  :global(.letter-modal) { width: 100vw !important; max-width: 100vw !important; height: 100vh; margin: 0; border-radius: 0; }
}
</style>
