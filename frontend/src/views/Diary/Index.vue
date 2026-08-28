<template>
  <div class="diary-page" ref="container">
    <!-- 顶部：标题 + 写日记 -->
    <header class="page-head">
      <h1>双人日记</h1>
      <n-button type="primary" v-press-bounce @click="openWrite">写日记</n-button>
    </header>

    <!-- 分段：全部 / 我写的 / 对方写的 -->
    <div class="tabs">
      <button :class="['tab', { active: tab === 'all' }]" @click="tab = 'all'">全部</button>
      <button :class="['tab', { active: tab === 'mine' }]" @click="tab = 'mine'">我写的</button>
      <button :class="['tab', { active: tab === 'partner' }]" @click="tab = 'partner'">对方写的</button>
    </div>

    <!-- 加载骨架 -->
    <IndSkeleton v-if="loading" variant="list" :rows="6" />

    <!-- 空态 -->
    <IndEmpty
      v-else-if="!filtered.length"
      :title="tab === 'mine' ? '你还没写过日记' : tab === 'partner' ? '对方还没写日记' : '还没有日记'"
      :desc="tab === 'mine' ? '记下第一篇心情吧～' : tab === 'partner' ? '催 TA 来写写你呀' : '点击右上角写第一篇'"
      actionText="写日记"
      @action="openWrite"
    />

    <!-- 列表 -->
    <div v-else class="cards">
      <div
        v-for="d in displayList"
        :key="d.id"
        class="love-card diary-card"
        @click="openDetail(d)"
      >
        <div class="row1">
          <span class="title title-clamp">{{ d.title }}</span>
          <n-tag :type="permMeta[d.permissionType]?.type ?? 'default'" size="small" round>
            {{ permMeta[d.permissionType]?.label ?? '' }}
          </n-tag>
        </div>
        <div class="row2 sub-text">
          <span v-if="d.diaryDate" class="meta"><Calendar :size="13" :stroke-width="1.8" /> {{ fmtDate(d.diaryDate) }}</span>
          <span v-if="d.weather" class="meta"><CloudSun :size="13" :stroke-width="1.8" /> {{ d.weather }}</span>
          <span class="meta"><Heart :size="13" :stroke-width="1.8" /> 心情 {{ d.moodScore }}/10</span>
          <span class="meta author"><PenLine :size="13" :stroke-width="1.8" /> {{ authorLabel(d.createUserId) }}</span>
        </div>
        <div v-if="d.moodTag" class="mood-tag">#{{ d.moodTag }}</div>
      </div>
    </div>

    <IndPager
      v-if="filtered.length"
      mode="more"
      :page="1"
      :page-size="12"
      :total="filtered.length"
      :loading="loading"
      :has-more="hasMore"
      @load-more="loadMore"
    />

    <!-- 写日记：iOS 风表单（移动端底部抽屉 / 桌面居中卡片） -->
    <LoveSheet v-model="showWrite" title="写日记" subtitle="记录此刻的心情与故事">
      <div class="diary-form">
        <LoveInput
          v-model="form.title"
          label="标题"
          placeholder="今天发生了什么…"
          :maxlength="80"
          counter
          clearable
          :invalid="titleInvalid"
          @update:modelValue="titleInvalid = false"
        />
        <LoveTextarea
          v-model="form.content"
          label="内容"
          placeholder="写下你的心情与故事"
          :rows="4"
          :maxlength="2000"
        />
        <LoveMoodPicker v-model="form.moodScore" label="心情" />
        <LoveChips v-model="form.weather" label="天气" :options="weatherOptions" />
        <LoveSegmented v-model="form.permissionType" label="权限" :options="permOptions" />
        <LoveDateField v-model="form.dateTs" label="日期" />
      </div>
      <template #footer>
        <LoveSaveBar
          :loading="saving"
          :success="saved"
          cancel-text="取消"
          save-text="保存"
          @cancel="closeWrite"
          @save="submit"
        />
      </template>
    </LoveSheet>

    <!-- 详情：NDrawer（移动端全屏） -->
    <n-drawer v-model:show="showDetail" :width="drawerWidth" placement="right">
      <n-drawer-content :title="current?.title || '日记详情'" closable>
        <div v-if="current" class="detail">
          <div class="sub-text detail-meta">
            <span v-if="current.diaryDate" class="meta"><Calendar :size="13" :stroke-width="1.8" /> {{ fmtDate(current.diaryDate) }}</span>
            <span v-if="current.weather" class="meta"><CloudSun :size="13" :stroke-width="1.8" /> {{ current.weather }}</span>
            <span class="meta"><Heart :size="13" :stroke-width="1.8" /> 心情 {{ current.moodScore }}/10</span>
            <n-tag :type="permMeta[current.permissionType]?.type ?? 'default'" size="small" round>
              {{ permMeta[current.permissionType]?.label ?? '' }}
            </n-tag>
          </div>
          <!-- 富文本展示：内容已在后端净化，前端仅呈现 -->
          <div class="diary-content" v-html="current.content" />
        </div>

        <n-divider title-placement="left">评论</n-divider>
        <div v-if="!comments.length" class="sub-text">还没有评论，来抢沙发～</div>
        <div v-for="c in comments" :key="c.id" class="comment">
          <div class="sub-text">{{ authorLabel(c.createUserId) }} · {{ fmtDateTime(c.createTime) }}</div>
          <div class="comment-body"><template v-for="(seg, si) in segments(c.content)" :key="si"><span v-if="seg.isMention" class="mention">@{{ seg.text }}</span><template v-else>{{ seg.text }}</template></template></div>
        </div>

        <template #footer>
          <div class="comment-box">
            <div class="comment-hint sub-text">输入 <kbd>@</kbd> 可提及对方</div>
            <n-input
              ref="commentInput"
              v-model:value="commentText"
              type="textarea"
              placeholder="说点什么…"
              class="comment-input"
              aria-label="评论内容"
              :autosize="{ minRows: 3, maxRows: 8 }"
              @input="onCommentInput"
              @keydown="onCommentKeydown"
              @blur="closeMention"
            />
            <div v-if="mentionOpen" class="mention-pop" role="listbox" aria-label="提及候选">
              <button
                v-for="(cand, ci) in mentionCandidates"
                :key="cand.name"
                type="button"
                class="mention-item"
                :class="{ active: ci === activeMention }"
                role="option"
                :aria-selected="ci === activeMention"
                @mousedown.prevent="applyMention(cand.name)"
                @mouseenter="activeMention = ci"
              >@{{ cand.name }}</button>
              <div v-if="!mentionCandidates.length" class="mention-empty">未绑定伴侣，无法 @ 提及</div>
            </div>
            <n-button type="primary" :loading="sending" :disabled="!commentText.trim()" v-click-burst @click="sendComment">
              发送
            </n-button>
          </div>
        </template>
      </n-drawer-content>
    </n-drawer>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch, nextTick } from 'vue';
import {
  NButton, NDrawer, NDrawerContent, NTag, NDivider, NInput,
} from 'naive-ui';
import type { DiaryDto, DiaryReq, DiaryCommentDto, PermissionType } from '@/types';
import { Calendar, CloudSun, Heart, PenLine } from 'lucide-vue-next';
import {
  listDiary, createDiary, listComments, addComment,
} from '@/api/diary';
import { isMobile } from '@/composables/useDevice';
import { useStaggerEnter } from '@/composables/useAnimation';
import { useAuthStore } from '@/store/authStore';
import { usePartnerStore } from '@/store/partnerStore';
import { useRealtime, overlaySyncMap } from '@/composables/useRealtime';
import { useSyncSettle } from '@/composables/useSyncSettle';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import {
  LoveSheet, LoveInput, LoveTextarea, LoveMoodPicker,
  LoveChips, LoveSegmented, LoveDateField, LoveSaveBar,
} from '@/components/loveform';
import { feedback } from '@/utils/feedback';

const auth = useAuthStore();
const partner = usePartnerStore();
const { useModuleSync } = useRealtime();
const myId = computed(() => auth.profile?.id ?? 0);

function authorLabel(uid?: number) {
  if (!uid) return 'TA';
  return uid === myId.value ? '我' : (partner.status?.partner?.nickName || 'TA');
}

// ---------- 列表 ----------
const loading = ref(true);
const list = ref<DiaryDto[]>([]);
const container = ref<HTMLElement>();
const tab = ref<'all' | 'mine' | 'partner'>('all');

useStaggerEnter(container, '.diary-card', { stagger: 0.08, y: 16 });
// 实时融合：伴侣在别处写/刷新日记时，本端卡片错落入场（非自己操作、尊重降级）
useSyncSettle('diary', container, list, '.diary-card');

const filtered = computed(() =>
  list.value.filter((d) => {
    if (tab.value === 'mine') return d.createUserId === myId.value;
    if (tab.value === 'partner') return d.createUserId !== myId.value;
    return true;
  })
);

const displayCount = ref(12);
const displayList = computed(() => filtered.value.slice(0, displayCount.value));
const hasMore = computed(() => displayCount.value < filtered.value.length);
function loadMore() {
  displayCount.value += 12;
}
watch(filtered, () => {
  displayCount.value = 12;
});
// 切换 tab 时按 author 重新向后台拉取，保证"对方写的"等分页数据完整
watch(tab, () => {
  void load();
});

async function load() {
  loading.value = true;
  try {
    // tab 过滤下沉到后端：author=mine/partner/all，避免 >50 篇时"对方写的"tab 漏数据
    const p = await listDiary({ page: 1, pageSize: 50, author: tab.value });
    list.value = p.items;
  } finally {
    loading.value = false;
  }
}
onMounted(async () => {
  if (!partner.status) await partner.load();
  await load();
  useModuleSync('diary', { items: list, getId: i => i.id, load, map: overlaySyncMap });
});

// ---------- 写日记 ----------
const showWrite = ref(false);
const saving = ref(false);
const saved = ref(false);
const titleInvalid = ref(false);
const form = reactive({
  title: '',
  content: '',
  moodTag: undefined as string | undefined,
  moodScore: 5,
  permissionType: 1 as PermissionType,
  weather: '',
  dateTs: null as number | null,
});

// 天气快捷选项（单选 chips）
const weatherOptions = ['晴', '多云', '阴', '小雨', '大雨', '雪', '风', '雾'];
// 权限用分段控件呈现，更贴近 iOS 原生选择
const permOptions = [
  { label: '公开', value: 1 },
  { label: '仅自己', value: 2 },
  { label: '对方可读', value: 3 },
];

function resetWrite() {
  form.title = '';
  form.content = '';
  form.moodTag = undefined;
  form.moodScore = 5;
  form.permissionType = 1;
  form.weather = '';
  form.dateTs = null;
  titleInvalid.value = false;
  saving.value = false;
  saved.value = false;
}

function openWrite() {
  resetWrite();
  showWrite.value = true;
}

function closeWrite() {
  showWrite.value = false;
}

async function submit() {
  if (!form.title.trim()) {
    titleInvalid.value = true;
    feedback.warn('给日记起个标题吧～');
    return;
  }
  saving.value = true;
  try {
    const req: DiaryReq = {
      title: form.title,
      content: form.content,
      moodTag: form.moodTag,
      moodScore: form.moodScore,
      permissionType: form.permissionType,
      weather: form.weather || undefined,
      diaryDate: form.dateTs ? new Date(form.dateTs).toISOString() : undefined,
    };
    await createDiary(req);
    saved.value = true;
    // 让「保存」按钮先完成对勾动画，再收起表单
    window.setTimeout(async () => {
      showWrite.value = false;
      feedback.saved('日记');
      await load();
    }, 720);
  } finally {
    saving.value = false;
  }
}

// ---------- 详情 / 评论 ----------
const showDetail = ref(false);
const current = ref<DiaryDto | null>(null);
const comments = ref<DiaryCommentDto[]>([]);
const commentText = ref('');
const sending = ref(false);

// ---------- 评论 @提及 (#11) ----------
const commentInput = ref<{ $el: HTMLElement } | null>(null);
const mentionOpen = ref(false);
const mentionQuery = ref('');
const mentionStart = ref(0);
const activeMention = ref(0);

const partnerName = computed(() => partner.status?.partner?.nickName || '');
const mentionCandidates = computed(() => {
  const n = partnerName.value;
  if (!n) return [];
  if (mentionQuery.value && !n.includes(mentionQuery.value)) return [];
  return [{ name: n }];
});

function getCommentTextarea(): HTMLTextAreaElement | null {
  const el = commentInput.value as unknown as { $el?: HTMLElement } | null;
  return (el?.$el?.querySelector('textarea') as HTMLTextAreaElement) ?? null;
}

function onCommentInput(e: string) {
  const ta = getCommentTextarea();
  if (!ta) return;
  const val = e ?? commentText.value;
  const pos = ta.selectionStart;
  const before = val.slice(0, pos);
  const m = before.match(/(^|\s)@([^\s@]*)$/);
  if (m) {
    mentionStart.value = pos - m[2].length - 1;
    mentionQuery.value = m[2];
    activeMention.value = 0;
    mentionOpen.value = true;
  } else {
    mentionOpen.value = false;
  }
}

function closeMention() {
  mentionOpen.value = false;
  mentionQuery.value = '';
}

function applyMention(name: string) {
  const ta = getCommentTextarea();
  const pos = ta ? ta.selectionStart : commentText.value.length;
  const before = commentText.value.slice(0, mentionStart.value);
  const after = commentText.value.slice(pos);
  commentText.value = `${before}@${name} ${after}`;
  closeMention();
  nextTick(() => {
    if (ta) {
      const np = before.length + name.length + 2;
      ta.focus();
      ta.setSelectionRange(np, np);
    }
  });
}

function onCommentKeydown(e: KeyboardEvent) {
  if (!mentionOpen.value || !mentionCandidates.value.length) return;
  const list = mentionCandidates.value;
  if (e.key === 'ArrowDown') {
    e.preventDefault();
    activeMention.value = (activeMention.value + 1) % list.length;
  } else if (e.key === 'ArrowUp') {
    e.preventDefault();
    activeMention.value = (activeMention.value - 1 + list.length) % list.length;
  } else if (e.key === 'Enter' || e.key === 'Tab') {
    e.preventDefault();
    applyMention(list[activeMention.value].name);
  } else if (e.key === 'Escape') {
    e.preventDefault();
    closeMention();
  }
}

// 渲染评论正文中的 @提及 高亮（仅匹配已绑定伴侣昵称，避免误高亮）
const mentionRe = computed(() => {
  const n = partnerName.value;
  return n ? new RegExp(`@${escapeRegExp(n)}`, 'g') : null;
});
function segments(text: string): { text: string; isMention: boolean }[] {
  const re = mentionRe.value;
  if (!re || !text) return [{ text, isMention: false }];
  const out: { text: string; isMention: boolean }[] = [];
  let last = 0;
  let m: RegExpExecArray | null;
  re.lastIndex = 0;
  while ((m = re.exec(text))) {
    if (m.index > last) out.push({ text: text.slice(last, m.index), isMention: false });
    out.push({ text: m[0].slice(1), isMention: true });
    last = m.index + m[0].length;
  }
  if (last < text.length) out.push({ text: text.slice(last), isMention: false });
  return out;
}
function escapeRegExp(s: string) {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

async function openDetail(d: DiaryDto) {
  current.value = d;
  showDetail.value = true;
  comments.value = await listComments(d.id);
}

async function sendComment() {
  if (!current.value || !commentText.value.trim()) return;
  sending.value = true;
  try {
    await addComment({ diaryId: current.value.id, content: commentText.value });
    commentText.value = '';
    comments.value = await listComments(current.value.id);
  } finally {
    sending.value = false;
  }
}

// ---------- 展示辅助 ----------
const permMeta: Record<PermissionType, { label: string; type: 'success' | 'warning' | 'info' }> = {
  1: { label: '公开', type: 'success' },
  2: { label: '仅自己', type: 'warning' },
  3: { label: '对方可读', type: 'info' },
};

function fmtDate(s?: string) {
  return s ? s.slice(0, 10) : '';
}
function fmtDateTime(s?: string) {
  return s ? s.replace('T', ' ').slice(0, 16) : '';
}

// 响应式宽度
const drawerWidth = computed(() => (isMobile() ? '100%' : 460));
</script>

<style scoped>
.diary-page { max-width: 880px; margin: 0 auto; }
.page-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 14px; }
.page-head h1 { font-size: 22px; margin: 0; }

.tabs { display: flex; gap: 8px; margin-bottom: 16px; }
.tab {
  border: none; background: transparent; cursor: pointer;
  padding: 6px 12px; border-radius: 999px; color: var(--color-ink-3);
  font-size: 14px; transition: all var(--dur-micro) var(--ease-love);
}
.tab.active { background: var(--color-rose); color: #fff; }

.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 12px; }
.diary-card { cursor: pointer; }
.row1 { display: flex; align-items: flex-start; justify-content: space-between; gap: 8px; }
.title { font-weight: 500; flex: 1; }
.row2 { display: flex; flex-wrap: wrap; gap: 10px; margin-top: 8px; font-size: 13px; }
.meta { display: inline-flex; align-items: center; gap: 4px; color: var(--color-ink-3); }
.meta :deep(svg) { color: var(--color-rose); flex: 0 0 auto; }
.mood-tag { color: var(--color-rose); font-size: 12px; margin-top: 6px; }

.detail-meta { display: flex; flex-wrap: wrap; gap: 10px; align-items: center; margin-bottom: 8px; }
.diary-content { line-height: 1.8; word-break: break-word; }
.diary-content :deep(img) { max-width: 100%; border-radius: 8px; }

.comment { padding: 10px 0; border-top: 1px solid var(--color-ink-soft); }
.comment-body { margin-top: 4px; }
.comment-box { position: relative; display: flex; flex-direction: column; gap: 10px; }
.comment-input :deep(.n-input__textarea),
.comment-input :deep(textarea) {
  font-size: 15px;
  line-height: 1.65;
  padding: 12px 14px;
  resize: vertical;
}
.comment-box .n-button { align-self: flex-end; min-width: 92px; }
.comment-hint { font-size: 12px; }
.comment-hint kbd {
  background: var(--color-surface-2); border: 1px solid var(--color-border);
  border-radius: 4px; padding: 0 5px; font-family: var(--font-mono); font-size: 11px;
}
.mention-pop {
  position: absolute; left: 0; right: 0; bottom: 100%; margin-bottom: 6px;
  background: var(--color-surface); border: 1px solid var(--color-border);
  border-radius: 10px; box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
  padding: 4px; z-index: 20; max-height: 160px; overflow: auto;
}
.mention-item {
  display: block; width: 100%; text-align: left; cursor: pointer;
  border: none; background: transparent; color: var(--color-ink);
  padding: 7px 10px; border-radius: 7px; font-size: 14px;
}
.mention-item.active, .mention-item:hover {
  background: var(--color-rose-soft, rgba(214, 51, 108, 0.10)); color: var(--color-rose);
}
.mention-empty { padding: 8px 10px; font-size: 13px; color: var(--color-ink-3); }
.mention { color: var(--color-rose); font-weight: 600; }

.diary-form { display: flex; flex-direction: column; gap: 18px; }
</style>
