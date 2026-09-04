<template>
  <div class="diary-page" ref="container">
    <!-- 品牌条 -->
    <div class="brand block">
      <IpIcon name="module_diary" :size="28" class="brand-icon" alt="双人日记" />
      <h1 class="ind-label">DIARY · 双人日记</h1>
      <span class="brand-status"><IndLed color="green" :size="9" /> 已同步</span>
    </div>

    <!-- 顶部：标题 + 写日记 -->
    <header class="page-head sec-head">
      <IndSectionTitle label="我们的日记" :led="true" />
      <n-button type="primary" class="uvi-glow-border" v-press-bounce @click="openWrite">写日记</n-button>
    </header>

    <!-- 最近心情：把心情日历的概览价值融合进日记页（数据来自每篇日记的 moodScore） -->
    <section v-if="recentMoods.length" class="mood-strip love-card">
      <div class="ms-head">
        <span class="ms-title">最近心情</span>
        <span class="ms-sub">记录里的情绪轨迹</span>
      </div>
      <div class="ms-row">
        <button
          v-for="m in recentMoods"
          :key="m.id"
          type="button"
          class="ms-chip"
          :style="{ background: moodColor(m.moodScore) }"
          :aria-label="`${fmtDate(m.diaryDate)} 心情 ${m.moodScore} 分，点击查看`"
          @click="openDetail(m)"
        >
          <IpIcon :name="moodIconName(m.moodScore)" :size="22" alt="心情" class="ms-face" />
          <span class="ms-score">{{ m.moodScore }}</span>
        </button>
      </div>
    </section>

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
      <TiltCard
        v-for="d in displayList"
        :key="d.id"
        class="diary-card-wrap"
      >
      <div
        class="love-card diary-card"
        role="button"
        tabindex="0"
        :aria-label="`查看日记：${d.title}`"
        @click="openDetail(d)"
        @keydown.enter="openDetail(d)"
        @keydown.space.prevent="openDetail(d)"
      >
        <div class="row1">
          <span class="title title-clamp">{{ d.title }}</span>
          <n-tag :type="permMeta[d.permissionType]?.type ?? 'default'" size="small" round>
            {{ permMeta[d.permissionType]?.label ?? '' }}
          </n-tag>
        </div>
        <div class="row2 sub-text">
          <span v-if="d.diaryDate" class="meta"><Calendar :size="13" :stroke-width="1.8" /> {{ fmtDate(d.diaryDate) }}<span v-if="relDate(d.diaryDate)" class="diary-rel"> · {{ relDate(d.diaryDate) }}</span></span>
          <span v-if="d.weather" class="meta"><CloudSun :size="13" :stroke-width="1.8" /> {{ d.weather }}</span>
          <span class="meta"><IpIcon :name="moodIconName(d.moodScore)" :size="15" :alt="'心情 ' + d.moodScore" /> 心情 {{ d.moodScore }}/10</span>
          <span class="meta author"><PenLine :size="13" :stroke-width="1.8" /> {{ authorLabel(d.createUserId) }}</span>
        </div>
        <div v-if="d.moodTag" class="mood-tag">#{{ d.moodTag }}</div>
      </div>
      </TiltCard>
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
          <div class="detail-actions">
            <n-popconfirm @positive-click="onDelete">
              <template #trigger>
                <n-button size="small" type="error" tertiary>删除</n-button>
              </template>
              确定删除这篇日记吗？此操作不可恢复。
            </n-popconfirm>
          </div>
          <div class="sub-text detail-meta">
            <span v-if="current.diaryDate" class="meta"><Calendar :size="13" :stroke-width="1.8" /> {{ fmtDate(current.diaryDate) }}<span v-if="relDate(current.diaryDate)" class="diary-rel"> · {{ relDate(current.diaryDate) }}</span></span>
            <span v-if="current.weather" class="meta"><CloudSun :size="13" :stroke-width="1.8" /> {{ current.weather }}</span>
            <span class="meta"><IpIcon :name="moodIconName(current.moodScore)" :size="15" :alt="'心情 ' + current.moodScore" /> 心情 {{ current.moodScore }}/10</span>
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
import { ref, reactive, computed, onMounted, onUnmounted, watch, nextTick } from 'vue';
import { useRoute } from 'vue-router';
import {
  NButton, NDrawer, NDrawerContent, NPopconfirm, NTag, NDivider, NInput,
} from 'naive-ui';
import type { DiaryDto, DiaryReq, DiaryCommentDto, PermissionType } from '@/types';
import { Calendar, CloudSun, PenLine } from 'lucide-vue-next';
import {
  listDiary, createDiary, listComments, addComment, deleteDiary,
} from '@/api/diary';
import { isMobile } from '@/composables/useDevice';
import { useStaggerEnter } from '@/composables/useAnimation';
import { useAuthStore } from '@/store/authStore';
import { usePartnerStore } from '@/store/partnerStore';
import { useRealtime, overlaySyncMap } from '@/composables/useRealtime';
import { useSyncSettle } from '@/composables/useSyncSettle';
import { useOptimistic } from '@/composables/useOptimistic';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndSectionTitle from '@/components/industrial/IndSectionTitle.vue';
import IndLed from '@/components/industrial/IndLed.vue';
import TiltCard from '@/components/Common/TiltCard.vue';
import IpIcon from '@/components/Common/IpIcon.vue';
import { moodIconName } from '@/utils/mood';
import {
  LoveSheet, LoveInput, LoveTextarea, LoveMoodPicker,
  LoveChips, LoveSegmented, LoveDateField, LoveSaveBar,
} from '@/components/loveform';
import { feedback } from '@/utils/feedback';

const auth = useAuthStore();
const partner = usePartnerStore();
const route = useRoute();
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

// 统一管理延迟回调（保存后收起表单 + 提示），卸载时一次性清理，避免过期定时器在组件销毁后误触发
const pendingTimers = new Set<number>();
function later(fn: () => void, ms: number) {
  const id = window.setTimeout(() => { pendingTimers.delete(id); fn(); }, ms);
  pendingTimers.add(id);
}
onUnmounted(() => {
  pendingTimers.forEach((id) => clearTimeout(id));
  pendingTimers.clear();
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

// 最近心情：心情日历融合进日记页——取带心情分的日记，按日期倒序取最近若干条，作为情绪轨迹概览
// 心情图标映射（mood_*.png）统一走 @/utils/mood
// 1(红/糟糕) → 10(绿/幸福) 柔和渐变，用于心情条底色
function moodColor(s: number): string {
  const hue = ((Math.max(1, Math.min(10, s)) - 1) * 130) / 9;
  return `hsl(${hue.toFixed(0)}, 68%, 92%)`;
}
const recentMoods = computed(() =>
  [...list.value]
    .filter((d) => d.moodScore != null && d.moodScore > 0)
    .sort((a, b) => new Date(b.diaryDate ?? b.createTime).getTime() - new Date(a.diaryDate ?? a.createTime).getTime())
    .slice(0, 18),
);
const { mutate } = useOptimistic(load);
onMounted(async () => {
  if (!partner.status) await partner.load();
  await load();
  useModuleSync('diary', { items: list, getId: i => i.id, load, map: overlaySyncMap });
  // 心情日历点击某天 → /diary?date=YYYY-MM-DD：预填日期并打开写日记，便于补记/回看当天
  const q = route.query.date;
  if (typeof q === 'string' && /^\d{4}-\d{2}-\d{2}$/.test(q)) {
    const d = new Date(`${q}T00:00:00`);
    if (!Number.isNaN(d.getTime())) {
      resetWrite();
      form.dateTs = d.getTime();
      showWrite.value = true;
    }
  }
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
      diaryDate: form.dateTs ? toLocalISO(form.dateTs) : undefined,
    };
    const ok = await mutate({
      label: '写日记',
      apply: () => {
        list.value = [
          {
            id: -Date.now(), title: req.title, content: req.content,
            moodTag: req.moodTag, moodScore: req.moodScore, permissionType: req.permissionType,
            weather: req.weather, diaryDate: req.diaryDate,
          } as DiaryDto,
          ...list.value,
        ];
      },
      api: () => createDiary(req),
    });
    if (ok) {
      saved.value = true;
      // 让「保存」按钮先完成对勾动画，再收起表单；用 later() 跟踪，切页时自动清理
      later(async () => {
        showWrite.value = false;
        feedback.saved('日记');
        await load();
      }, 720);
    }
  } finally {
    saving.value = false;
  }
}

// ---------- 详情 / 评论 ----------
const showDetail = ref(false);
const current = ref<DiaryDto | null>(null);
const { mutate: mutateComment } = useOptimistic(async () => {
  if (current.value) comments.value = await listComments(current.value.id);
});
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
  const diaryId = current.value.id;
  const content = commentText.value;
  const ok = await mutateComment({
    label: '发评论',
    apply: () => {
      comments.value = [
        {
          id: -Date.now(), diaryId, content,
          userId: auth.profile?.id ?? 0, createdAt: new Date().toISOString(),
          createUserId: auth.profile?.id ?? 0, createTime: new Date().toISOString(),
        } as DiaryCommentDto,
        ...comments.value,
      ];
    },
    api: () => addComment({ diaryId, content }),
  });
  if (ok) {
    commentText.value = '';
    comments.value = await listComments(diaryId);
  }
}

// 删除当前日记：后端 DELETE /api/diary/delete 已存在，前端 api 已有 deleteDiary(id)，
// 仅补 UI 入口 + 二次确认弹窗（对齐基准「删除弹窗」）。删除后本地移除并收起抽屉。
async function onDelete() {
  if (!current.value) return;
  const id = current.value.id;
  const ok = await mutate({
    label: '删除日记',
    apply: () => { list.value = list.value.filter((d) => d.id !== id); },
    api: () => deleteDiary(id),
  });
  if (ok) {
    showDetail.value = false;
    feedback.deleted('日记');
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
// 日记日期相对时间（date-only，无时分）：fmtDate 已是 YYYY-MM-DD 精确日期，附「今天/昨天/N天前」补足"多久前的回忆"语义
function relDate(s?: string): string {
  if (!s) return '';
  const d = new Date(s);
  const now = new Date();
  const diff = now.getTime() - d.getTime();
  const day = 86400000;
  if (diff < 0) return '未来';
  if (diff < day && now.getDate() === d.getDate()) return '今天';
  if (diff < 2 * day) return '昨天';
  if (diff < 30 * day) return `${Math.floor(diff / day)} 天前`;
  if (diff < 365 * day) return `${Math.floor(diff / (30 * day))} 个月前`;
  return `${Math.floor(diff / (365 * day))} 年前`;
}
// 把本地时间戳格式化为「本地时刻」ISO（不带 Z），避免 toISOString() 转 UTC 导致东八区等正偏移时区日期前移一天
function toLocalISO(ts: number): string {
  const d = new Date(ts);
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}T${p(d.getHours())}:${p(d.getMinutes())}:${p(d.getSeconds())}`;
}
function fmtDateTime(s?: string) {
  return s ? s.replace('T', ' ').slice(0, 16) : '';
}

// 响应式宽度
const drawerWidth = computed(() => (isMobile() ? '100%' : 460));
</script>

<style scoped>
.diary-page { max-width: 880px; margin: 0 auto; }
.brand {
  display: flex; align-items: center; gap: 16px; padding: 12px 16px; margin-bottom: 8px;
  background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg);
  box-shadow: var(--shadow-card);
}
.brand-status {
  margin-left: auto; display: inline-flex; align-items: center; gap: 6px;
  font-size: 12px; font-weight: 500;
  color: var(--color-ink-2);
  padding: 4px 12px; border-radius: 999px;
  background: var(--color-surface-2); border: 1px solid var(--color-border);
}
.brand-icon { margin-right: 2px; flex: 0 0 auto; }
.diary-rel { color: var(--color-rose); opacity: 0.85; }
.ind-label { font-family: var(--font-mono); font-weight: 500; letter-spacing: 0.1em; font-size: 13px; color: var(--color-ink); margin: 0; }
.page-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 16px; }
.page-head h1 { font-size: 22px; margin: 0; }
.sec-head { gap: 12px; }
.sec-head :deep(.ind-sec-title) { flex: 1 1 auto; min-width: 0; margin: 0; }

.tabs { display: flex; gap: 8px; margin-bottom: 16px; }
.tab {
  border: none; background: transparent; cursor: pointer;
  padding: 6px 12px; border-radius: 999px; color: var(--color-ink-3);
  font-size: 14px; transition: all var(--dur-micro) var(--ease-love);
}
.tab.active { background: var(--color-rose); color: var(--color-on-primary); }

.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 12px; }
.diary-card { cursor: pointer; transition: box-shadow var(--dur-pop) var(--ease-love), border-color var(--dur-pop) var(--ease-love); }
.diary-card-wrap { display: block; transform-style: preserve-3d; }
.diary-card:hover { box-shadow: var(--elev-3); border-color: var(--color-rose-soft); }
.diary-card:focus-visible { outline: 2px solid var(--color-rose); outline-offset: 2px; }
.row1 { display: flex; align-items: flex-start; justify-content: space-between; gap: 8px; }
.title { font-weight: 500; flex: 1; }
.row2 { display: flex; flex-wrap: wrap; gap: 10px; margin-top: 8px; font-size: 13px; }
.meta { display: inline-flex; align-items: center; gap: 4px; color: var(--color-ink-3); }
.meta :deep(svg) { color: var(--color-rose-text); flex: 0 0 auto; }
.mood-tag { color: var(--color-rose-text); font-size: 12px; margin-top: 6px; }

.mood-strip { padding: 16px 16px; margin-bottom: 16px; }
.ms-head { display: flex; align-items: baseline; gap: 8px; margin-bottom: 10px; }
.ms-title { font-weight: 600; font-size: 15px; }
.ms-sub { font-size: 12px; color: var(--color-ink-3); }
.ms-row { display: flex; flex-wrap: wrap; gap: 8px; }
.ms-chip {
  display: inline-flex; flex-direction: column; align-items: center; justify-content: center;
  width: 44px; height: 44px; border: none; border-radius: 12px; cursor: pointer;
  transition: transform var(--dur-micro) var(--ease-love), box-shadow var(--dur-micro) var(--ease-love);
}
.ms-chip:hover { box-shadow: var(--elev-2); }
.ms-chip:active { transform: scale(0.92); }
.ms-chip:focus-visible { outline: 2px solid var(--color-rose); outline-offset: 2px; }
.ms-face { width: 22px; height: 22px; line-height: 1; filter: drop-shadow(0 1px 1px rgba(0, 0, 0, 0.08)); }
.ms-score { font-size: 11px; color: var(--color-ink-2); margin-top: 2px; font-weight: 600; }

.detail-actions { display: flex; justify-content: flex-end; margin-bottom: 10px; }
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
  padding: 12px 16px;
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
  background: var(--color-rose-soft, rgba(214, 51, 108, 0.10)); color: var(--color-rose-text);
}
.mention-empty { padding: 8px 10px; font-size: 13px; color: var(--color-ink-3); }
.mention { color: var(--color-rose-text); font-weight: 600; }

.diary-form { display: flex; flex-direction: column; gap: 16px; }

@media (max-width: 767px) {
  .brand { padding: 10px 16px; }
  .brand .ind-label { font-size: 12px; }
  .brand-status { padding: 3px 8px; font-size: 11px; }
}
</style>
