<template>
  <div class="board-page" ref="container">
    <header class="page-head">
      <div class="head-left">
        <h1>私密留言板</h1>
        <span class="sub">只属于你们两个人的悄悄话墙</span>
      </div>
    </header>

    <!-- 留言编辑区 -->
    <div class="composer love-card">
      <n-input
        v-model:value="draft"
        type="textarea"
        :autosize="{ minRows: 2, maxRows: 5 }"
        placeholder="写点什么给对方吧～（情话、叮嘱、今天的碎碎念）"
        @keydown.ctrl.enter="send"
      />
      <div class="composer-bar">
        <div class="colors">
          <button
            v-for="c in colorPresets"
            :key="c"
            class="swatch"
            :class="{ on: draftColor === c }"
            :style="{ background: c }"
            :aria-label="`颜色 ${c}`"
            @click="draftColor = draftColor === c ? '' : c"
          />
          <button class="swatch none" :class="{ on: !draftColor }" aria-label="无颜色" @click="draftColor = ''">无</button>
        </div>
        <n-button type="primary" round :disabled="!draft.trim()" :loading="sending" @click="send">贴上墙</n-button>
      </div>
    </div>

    <!-- 列表 -->
    <IndSkeleton v-if="loading" variant="list" :rows="6" />
    <IndEmpty
      v-else-if="!messages.length"
      title="留言墙还是空的"
      desc="留下第一条悄悄话，让 TA 一打开就看到你的心意～"
    />
    <div v-else class="wall">
      <div
        v-for="m in displayList"
        :key="m.id"
        class="msg love-card"
        :class="{ mine: m.createUserId === meId, pinned: m.pinned }"
        :style="m.color ? { borderLeftColor: m.color } : {}"
      >
        <div class="msg-top">
          <span v-if="m.pinned" class="pin-tag"><Pin :size="13" :stroke-width="2.2" /> 置顶</span>
          <span class="author">{{ m.authorName || (m.createUserId === meId ? '我' : 'TA') }}</span>
          <span class="time sub-text">{{ fmt(m.createTime) }}</span>
        </div>
        <p class="msg-body" :style="m.color ? { color: m.color } : {}">{{ m.content }}</p>
        <div class="msg-actions">
          <n-button size="small" tertiary @click="onPin(m)">{{ m.pinned ? '取消置顶' : '置顶' }}</n-button>
          <n-button v-if="m.createUserId === meId" size="small" tertiary @click="openEdit(m)">编辑</n-button>
          <n-popconfirm v-if="m.createUserId === meId" @positive-click="onDelete(m.id)">
            <template #trigger>
              <n-button size="small" tertiary type="error">删除</n-button>
            </template>
            确定删除这条留言吗？
          </n-popconfirm>
        </div>
      </div>
    </div>

    <IndPager
      v-if="messages.length"
      mode="more"
      :page="1"
      :page-size="20"
      :total="messages.length"
      :loading="loading"
      :has-more="hasMore"
      @load-more="loadMore"
    />

    <!-- 编辑 模态 -->
    <n-modal
      v-model:show="showEdit"
      class="board-modal"
      preset="card"
      title="编辑留言"
      style="width: 92%; max-width: 520px;"
    >
      <n-input v-model:value="editDraft" type="textarea" :autosize="{ minRows: 2, maxRows: 5 }" />
      <div class="colors edit-colors">
        <button
          v-for="c in colorPresets"
          :key="c"
          class="swatch"
          :class="{ on: editColor === c }"
          :style="{ background: c }"
          @click="editColor = editColor === c ? '' : c"
        />
        <button class="swatch none" :class="{ on: !editColor }" @click="editColor = ''">无</button>
      </div>
      <template #footer>
        <div class="modal-foot">
          <n-button @click="showEdit = false">取消</n-button>
          <n-button type="primary" :loading="sending" @click="submitEdit">保存</n-button>
        </div>
      </template>
    </n-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { NButton, NModal, NInput, NPopconfirm } from 'naive-ui';
import { Pin } from 'lucide-vue-next';
import type { BoardMessageDto, BoardMessageReq } from '@/types';
import {
  listBoard, createBoard, updateBoard, deleteBoard, pinBoard,
} from '@/api/board';
import { useNotifyStore } from '@/store/notifyStore';
import { useStaggerEnter } from '@/composables/useAnimation';
import { useRealtime, overlaySyncMap } from '@/composables/useRealtime';
import { useAuthStore } from '@/store/authStore';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndPager from '@/components/industrial/IndPager.vue';
import { feedback } from '@/utils/feedback';

const auth = useAuthStore();
const notify = useNotifyStore();
const meId = computed(() => auth.profile?.id ?? 0);

const loading = ref(true);
const sending = ref(false);
const container = ref<HTMLElement>();
const messages = ref<BoardMessageDto[]>([]);

const draft = ref('');
const draftColor = ref('');
const colorPresets = ['#ff6f7d', '#ff9a76', '#7c83fd', '#43c6ac', '#f6c453', '#c77dff'];

const showEdit = ref(false);
const editId = ref<number>(0);
const editDraft = ref('');
const editColor = ref('');

const displayCount = ref(20);
const displayList = computed(() => messages.value.slice(0, displayCount.value));
const hasMore = computed(() => displayCount.value < messages.value.length);
function loadMore() { displayCount.value += 20; }

function fmt(s: string) {
  const d = new Date(s);
  return `${d.getMonth() + 1}/${d.getDate()} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
}

async function send() {
  const content = draft.value.trim();
  if (!content) return;
  sending.value = true;
  try {
    const req: BoardMessageReq = { content, color: draftColor.value || undefined };
    await createBoard(req);
    feedback.created('留言');
    draft.value = '';
    draftColor.value = '';
    await load();
  } finally { sending.value = false; }
}

function openEdit(m: BoardMessageDto) {
  editId.value = m.id;
  editDraft.value = m.content;
  editColor.value = m.color ?? '';
  showEdit.value = true;
}
async function submitEdit() {
  if (!editDraft.value.trim()) return;
  sending.value = true;
  try {
    await updateBoard(editId.value, { content: editDraft.value.trim(), color: editColor.value || undefined });
    feedback.updated('留言');
    showEdit.value = false;
    await load();
  } finally { sending.value = false; }
}

async function onPin(m: BoardMessageDto) {
  await pinBoard({ id: m.id });
  notify.success(m.pinned ? '已取消置顶' : '已置顶');
  await load();
}
async function onDelete(id: number) {
  await deleteBoard(id);
  feedback.deleted('留言');
  await load();
}

async function load() {
  loading.value = true;
  try {
    const p = await listBoard({ page: 1, pageSize: 300 });
    messages.value = p.items;
  } finally { loading.value = false; }
}

useStaggerEnter(container, '.love-card', { stagger: 0.05, y: 12 });
const { useModuleSync } = useRealtime();
onMounted(async () => {
  await load();
  useModuleSync('board', { items: messages, getId: (i) => i.id, load, map: overlaySyncMap });
});
</script>

<style scoped>
.board-page { max-width: 880px; margin: 0 auto; }
.page-head { display: flex; align-items: center; gap: 14px; margin-bottom: 16px; }
.page-head h1 { font-size: 22px; margin: 0; }
.sub { font-size: 13px; color: var(--color-ink-3); }

.composer { padding: 14px; margin-bottom: 18px; }
.composer-bar { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-top: 12px; }
.colors { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.swatch {
  width: 22px; height: 22px; border-radius: 50%; cursor: pointer; border: 2px solid transparent;
  box-shadow: 0 0 0 1px var(--color-border); transition: transform var(--dur-micro) var(--ease-love);
}
.swatch.on { border-color: var(--color-ink); transform: scale(1.12); }
.swatch.none { background: var(--color-surface-2); color: var(--color-ink-3); font-size: 11px; display: grid; place-items: center; }

.wall { display: flex; flex-direction: column; gap: 12px; }
.msg { padding: 14px 16px; border-left: 4px solid var(--color-rose-soft); }
.msg.mine { border-left-color: var(--color-cocoa); }
.msg.pinned { background: color-mix(in srgb, var(--color-rose-soft) 40%, var(--color-surface)); }
.msg-top { display: flex; align-items: center; gap: 10px; }
.pin-tag { display: inline-flex; align-items: center; gap: 3px; font-size: 11px; color: var(--color-rose); background: var(--color-rose-soft); padding: 2px 8px; border-radius: 999px; }
.author { font-weight: 600; color: var(--color-ink); font-size: 13px; }
.time { margin-left: auto; font-size: 11px; font-family: var(--font-mono); }
.msg-body { margin: 8px 0 0; white-space: pre-wrap; line-height: 1.6; color: var(--color-ink); }
.msg-actions { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 10px; }
.edit-colors { margin-top: 12px; }
.modal-foot { display: flex; justify-content: flex-end; gap: 10px; }

@media (max-width: 767px) { .composer-bar { flex-direction: column; align-items: stretch; } }
:global(.board-modal) { padding: 0 !important; }
@media (max-width: 767px) {
  :global(.board-modal) { width: 100vw !important; max-width: 100vw !important; height: 100vh; margin: 0; border-radius: 0; }
}
</style>
