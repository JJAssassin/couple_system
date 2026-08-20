<template>
  <div class="quiz-page" ref="container">
    <header class="page-head">
      <div class="head-left">
        <h1>默契问答</h1>
        <span class="sub">同一道题各选一次，看看你们想的是不是同一个答案</span>
      </div>
    </header>

    <!-- 默契统计 -->
    <div class="stat-card love-card">
      <IndProgressRing
        :value="stats.matchRate"
        :size="104"
        suffix="%"
        sublabel="默契率"
        color="var(--color-rose)"
      />
      <div class="stat-nums">
        <div class="stat-item">
          <span class="n">{{ stats.revealedRounds }}</span>
          <span class="l sub-text">已揭晓</span>
        </div>
        <div class="stat-item">
          <span class="n hit">{{ stats.matchedRounds }}</span>
          <span class="l sub-text">默契达成</span>
        </div>
        <div class="stat-item">
          <span class="n">{{ stats.pendingRounds }}</span>
          <span class="l sub-text">进行中</span>
        </div>
      </div>
      <div class="stat-tip sub-text">
        {{ rateTip }}
      </div>
    </div>

    <!-- 当前进行中的一局 -->
    <IndSkeleton v-if="loading" variant="list" :rows="4" />
    <template v-else>
      <div v-if="activeRound" class="active love-card">
        <div class="active-top">
          <span class="badge"><Sparkles :size="13" :stroke-width="2.2" /> 进行中</span>
          <span v-if="activeRound.category" class="cat">{{ activeRound.category }}</span>
          <n-popconfirm @positive-click="onAbandon(activeRound.id)">
            <template #trigger>
              <n-button size="tiny" tertiary type="error">放弃这局</n-button>
            </template>
            确定放弃这一局吗？记录会被删除。
          </n-popconfirm>
        </div>

        <h2 class="q-text">{{ activeRound.questionText }}</h2>

        <!-- 我还没答：选选项 -->
        <div v-if="!activeRound.myAnswered" class="options">
          <button
            v-for="(o, i) in activeRound.options"
            :key="i"
            class="opt"
            :disabled="answering"
            @click="onAnswer(activeRound!.id, i)"
          >
            <span class="opt-idx">{{ letters[i] }}</span>
            <span class="opt-text">{{ o }}</span>
          </button>
        </div>

        <!-- 我已答，等对方 -->
        <div v-else class="waiting">
          <p class="my-pick">
            你选了
            <strong>{{ letters[activeRound.myAnswer ?? 0] }}. {{ activeRound.options[activeRound.myAnswer ?? 0] }}</strong>
          </p>
          <p class="wait-tip sub-text">
            {{ activeRound.mateAnswered ? '双方都答完了，正在揭晓…' : '已提交，等 TA 作答后自动揭晓（对方看不到你选了什么）' }}
          </p>
        </div>
      </div>

      <!-- 没有进行中的局：发起 -->
      <div v-else class="starter love-card">
        <p class="starter-tip">来一局？随机抽一道题，两个人分别作答。</p>
        <div class="starter-actions">
          <n-button type="primary" round :loading="starting" @click="onStart(null)">
            <template #icon><Dices :size="16" /></template>
            随机抽一题
          </n-button>
          <n-popselect
            v-model:value="pickQuestionId"
            :options="questionOptions"
            trigger="click"
            scrollable
            @update:value="onStart"
          >
            <n-button round :loading="starting">自己选题</n-button>
          </n-popselect>
        </div>
      </div>
    </template>

    <!-- 历史战绩 -->
    <section class="history">
      <div class="sec-head">
        <h3>历史战绩</h3>
        <n-button size="small" tertiary @click="showBank = true">题库管理</n-button>
      </div>

      <IndEmpty
        v-if="!revealedRounds.length"
        title="还没有揭晓过的对局"
        desc="答完第一局，这里就会记录你们的默契瞬间～"
      />
      <div v-else class="rounds">
        <div
          v-for="r in revealedRounds"
          :key="r.id"
          class="round love-card"
          :class="{ matched: r.isMatched }"
        >
          <div class="round-top">
            <span class="result" :class="{ hit: r.isMatched }">
              <component :is="r.isMatched ? Check : X" :size="13" :stroke-width="2.6" />
              {{ r.isMatched ? '默契' : '没对上' }}
            </span>
            <span v-if="r.category" class="cat">{{ r.category }}</span>
            <span class="time sub-text">{{ fmt(r.createTime) }}</span>
          </div>
          <p class="round-q">{{ r.questionText }}</p>
          <div class="picks">
            <div class="pick">
              <span class="who sub-text">{{ nameOf(r.firstUserId) }}</span>
              <span class="ans">{{ optText(r, r.firstAnswer) }}</span>
            </div>
            <div class="pick">
              <span class="who sub-text">{{ nameOf(r.secondUserId) }}</span>
              <span class="ans">{{ optText(r, r.secondAnswer) }}</span>
            </div>
          </div>
          <n-popconfirm @positive-click="onDeleteRound(r.id)">
            <template #trigger>
              <n-button size="tiny" tertiary type="error" class="round-del">删除</n-button>
            </template>
            删除这条战绩？默契率会跟着重算。
          </n-popconfirm>
        </div>
      </div>
    </section>

    <!-- 题库管理 -->
    <n-modal
      v-model:show="showBank"
      class="quiz-modal"
      preset="card"
      title="题库管理"
      style="width: 92%; max-width: 560px;"
    >
      <div class="bank-add">
        <n-input v-model:value="newText" placeholder="题目，例如：TA 最想去的城市是？" />
        <div v-for="(_, i) in newOptions" :key="i" class="opt-row">
          <span class="opt-idx small">{{ letters[i] }}</span>
          <n-input v-model:value="newOptions[i]" :placeholder="`选项 ${letters[i]}`" />
          <n-button
            v-if="newOptions.length > 2"
            size="small"
            tertiary
            type="error"
            @click="newOptions.splice(i, 1)"
          >
            <template #icon><Trash2 :size="14" /></template>
          </n-button>
        </div>
        <div class="bank-add-bar">
          <n-button v-if="newOptions.length < 6" size="small" tertiary @click="newOptions.push('')">
            <template #icon><Plus :size="14" /></template>
            加选项
          </n-button>
          <n-input v-model:value="newCategory" size="small" placeholder="分类（可选）" class="cat-input" />
          <n-button type="primary" size="small" :loading="savingQ" :disabled="!canAddQuestion" @click="onAddQuestion">
            添加题目
          </n-button>
        </div>
      </div>

      <div class="bank-list">
        <div v-for="q in questions" :key="q.id" class="bank-item">
          <div class="bank-main">
            <p class="bank-q">{{ q.text }}</p>
            <p class="bank-opts sub-text">{{ q.options.join(' / ') }}</p>
          </div>
          <span v-if="q.isBuiltin" class="builtin sub-text">内置</span>
          <n-popconfirm v-else @positive-click="onDeleteQuestion(q.id)">
            <template #trigger>
              <n-button size="tiny" tertiary type="error">删除</n-button>
            </template>
            删除这道题？已有战绩不受影响。
          </n-popconfirm>
        </div>
      </div>
    </n-modal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { NButton, NModal, NInput, NPopconfirm, NPopselect } from 'naive-ui';
import { Sparkles, Dices, Check, X, Plus, Trash2 } from 'lucide-vue-next';
import type { QuizQuestionDto, QuizRoundDto, QuizStatsDto } from '@/types';
import {
  listQuizQuestions, createQuizQuestion, deleteQuizQuestion,
  listQuizRounds, startQuizRound, answerQuizRound, deleteQuizRound, getQuizStats,
} from '@/api/quiz';
import { useNotifyStore } from '@/store/notifyStore';
import { useStaggerEnter } from '@/composables/useAnimation';
import { useRealtime } from '@/composables/useRealtime';
import { useAuthStore } from '@/store/authStore';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import IndProgressRing from '@/components/industrial/IndProgressRing.vue';
import { feedback } from '@/utils/feedback';

const auth = useAuthStore();
const notify = useNotifyStore();
const meId = computed(() => auth.profile?.id ?? 0);

const letters = ['A', 'B', 'C', 'D', 'E', 'F'];
const container = ref<HTMLElement>();
const loading = ref(true);
const starting = ref(false);
const answering = ref(false);
const savingQ = ref(false);

const rounds = ref<QuizRoundDto[]>([]);
const questions = ref<QuizQuestionDto[]>([]);
const stats = ref<QuizStatsDto>({
  totalRounds: 0, revealedRounds: 0, matchedRounds: 0, matchRate: 0, pendingRounds: 0,
});

const showBank = ref(false);
const pickQuestionId = ref<number | null>(null);
const newText = ref('');
const newOptions = ref<string[]>(['', '']);
const newCategory = ref('');

/** 进行中的一局（后端保证同时最多一局未揭晓） */
const activeRound = computed(() => rounds.value.find((r) => !r.isRevealed) ?? null);
const revealedRounds = computed(() => rounds.value.filter((r) => r.isRevealed));

const questionOptions = computed(() =>
  questions.value.map((q) => ({ label: q.text, value: q.id })));

const canAddQuestion = computed(() =>
  newText.value.trim().length > 0
  && newOptions.value.filter((o) => o.trim().length > 0).length >= 2);

const rateTip = computed(() => {
  if (stats.value.revealedRounds === 0) return '还没有揭晓的对局，先来一局试试';
  const r = stats.value.matchRate;
  if (r >= 80) return '心有灵犀，这默契有点犯规了';
  if (r >= 60) return '默契不错，还有惊喜空间';
  if (r >= 40) return '一半一半，多聊聊会更懂对方';
  return '答案经常错开，正好是了解彼此的机会';
});

function fmt(s: string) {
  const d = new Date(s);
  return `${d.getMonth() + 1}/${d.getDate()} ${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`;
}
function nameOf(uid?: number | null) {
  if (uid == null) return '—';
  return uid === meId.value ? '我' : 'TA';
}
function optText(r: QuizRoundDto, idx?: number | null) {
  if (idx == null || idx < 0 || idx >= r.options.length) return '—';
  return `${letters[idx]}. ${r.options[idx]}`;
}

async function onStart(questionId: number | null) {
  starting.value = true;
  try {
    await startQuizRound({ questionId: questionId ?? undefined });
    notify.success('新的一局开始了');
    pickQuestionId.value = null;
    await load();
  } finally { starting.value = false; }
}

async function onAnswer(roundId: number, answer: number) {
  answering.value = true;
  try {
    const r = await answerQuizRound({ roundId, answer });
    if (r.isRevealed) {
      notify.success(r.isMatched ? '默契达成！你们选了同一个 🎉' : '这次没对上，聊聊看？');
    } else {
      notify.success('已提交，等 TA 作答');
    }
    await load();
  } finally { answering.value = false; }
}

async function onAbandon(id: number) {
  await deleteQuizRound(id);
  notify.success('已放弃这局');
  await load();
}
async function onDeleteRound(id: number) {
  await deleteQuizRound(id);
  feedback.deleted('战绩');
  await load();
}

async function onAddQuestion() {
  const options = newOptions.value.map((o) => o.trim()).filter((o) => o.length > 0);
  savingQ.value = true;
  try {
    await createQuizQuestion({
      text: newText.value.trim(),
      options,
      category: newCategory.value.trim() || undefined,
    });
    feedback.created('题目');
    newText.value = '';
    newOptions.value = ['', ''];
    newCategory.value = '';
    questions.value = await listQuizQuestions();
  } finally { savingQ.value = false; }
}
async function onDeleteQuestion(id: number) {
  await deleteQuizQuestion(id);
  feedback.deleted('题目');
  questions.value = await listQuizQuestions();
}

async function load() {
  loading.value = true;
  try {
    const [p, s, qs] = await Promise.all([
      listQuizRounds({ page: 1, pageSize: 200 }),
      getQuizStats(),
      listQuizQuestions(),
    ]);
    rounds.value = p.items;
    stats.value = s;
    questions.value = qs;
  } finally { loading.value = false; }
}

useStaggerEnter(container, '.love-card', { stagger: 0.05, y: 12 });
const { onSync } = useRealtime();
onMounted(async () => {
  await load();
  // 对局需要重算默契率/揭晓态，且未揭晓前后端会屏蔽对方选项，
  // 因此不做就地 upsert，收到 quiz 信号统一整体重载。
  onSync('quiz', load);
});
</script>

<style scoped>
.quiz-page { max-width: 880px; margin: 0 auto; }
.page-head { display: flex; align-items: center; gap: 14px; margin-bottom: 16px; }
.page-head h1 { font-size: 22px; margin: 0; }
.sub { font-size: 13px; color: var(--color-ink-3); }

.stat-card {
  display: flex; align-items: center; gap: 20px; padding: 16px 18px; margin-bottom: 16px;
  flex-wrap: wrap;
}
.stat-nums { display: flex; gap: 22px; }
.stat-item { display: flex; flex-direction: column; align-items: center; gap: 2px; }
.stat-item .n { font-size: 22px; font-weight: 700; font-family: var(--font-mono); color: var(--color-ink); }
.stat-item .n.hit { color: var(--color-rose); }
.stat-item .l { font-size: 11px; }
.stat-tip { flex: 1 1 180px; font-size: 12px; line-height: 1.5; text-align: right; }

.active, .starter { padding: 18px; margin-bottom: 18px; }
.active-top { display: flex; align-items: center; gap: 10px; margin-bottom: 10px; }
.badge {
  display: inline-flex; align-items: center; gap: 4px; font-size: 11px;
  color: var(--color-rose); background: var(--color-rose-soft);
  padding: 2px 8px; border-radius: 999px;
}
.cat {
  font-size: 11px; color: var(--color-ink-3); background: var(--color-surface-2);
  padding: 2px 8px; border-radius: 999px;
}
.active-top .n-button { margin-left: auto; }

.q-text { font-size: 17px; margin: 0 0 14px; line-height: 1.5; color: var(--color-ink); }

.options { display: flex; flex-direction: column; gap: 10px; }
.opt {
  display: flex; align-items: center; gap: 12px; width: 100%; text-align: left;
  padding: 12px 14px; border-radius: var(--radius-md, 12px); cursor: pointer;
  background: var(--color-surface-2); border: 1.5px solid var(--color-border);
  transition: transform var(--dur-micro) var(--ease-love), border-color var(--dur-micro) var(--ease-love);
}
.opt:hover:not(:disabled) { border-color: var(--color-rose); transform: translateY(-1px); }
.opt:disabled { opacity: .6; cursor: not-allowed; }
.opt-idx {
  flex: 0 0 26px; height: 26px; display: grid; place-items: center; border-radius: 50%;
  background: var(--color-rose-soft); color: var(--color-rose);
  font-size: 12px; font-weight: 700; font-family: var(--font-mono);
}
.opt-idx.small { flex-basis: 22px; height: 22px; font-size: 11px; }
.opt-text { color: var(--color-ink); font-size: 14px; }

.waiting { text-align: center; padding: 8px 0; }
.my-pick { margin: 0 0 6px; font-size: 14px; color: var(--color-ink); }
.wait-tip { font-size: 12px; }

.starter-tip { margin: 0 0 14px; font-size: 14px; color: var(--color-ink-2); }
.starter-actions { display: flex; gap: 10px; flex-wrap: wrap; }

.history { margin-top: 6px; }
.sec-head { display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px; }
.sec-head h3 { margin: 0; font-size: 15px; }

.rounds { display: flex; flex-direction: column; gap: 12px; }
.round { padding: 14px 16px; position: relative; border-left: 4px solid var(--color-border); }
.round.matched { border-left-color: var(--color-rose); }
.round-top { display: flex; align-items: center; gap: 10px; }
.result {
  display: inline-flex; align-items: center; gap: 4px; font-size: 11px; font-weight: 600;
  padding: 2px 8px; border-radius: 999px;
  color: var(--color-ink-3); background: var(--color-surface-2);
}
.result.hit { color: var(--color-rose); background: var(--color-rose-soft); }
.round-top .time { margin-left: auto; font-size: 11px; font-family: var(--font-mono); }
.round-q { margin: 8px 0 10px; font-size: 14px; color: var(--color-ink); line-height: 1.5; }
.picks { display: flex; gap: 12px; flex-wrap: wrap; }
.pick {
  flex: 1 1 140px; display: flex; flex-direction: column; gap: 2px;
  background: var(--color-surface-2); border-radius: 10px; padding: 8px 10px;
}
.pick .who { font-size: 11px; }
.pick .ans { font-size: 13px; color: var(--color-ink); }
.round-del { position: absolute; right: 12px; bottom: 12px; }

.bank-add { display: flex; flex-direction: column; gap: 10px; }
.opt-row { display: flex; align-items: center; gap: 8px; }
.bank-add-bar { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.cat-input { max-width: 140px; }
.bank-list { margin-top: 16px; display: flex; flex-direction: column; gap: 8px; max-height: 40vh; overflow-y: auto; }
.bank-item {
  display: flex; align-items: center; gap: 10px;
  padding: 8px 10px; background: var(--color-surface-2); border-radius: 10px;
}
.bank-main { flex: 1; min-width: 0; }
.bank-q { margin: 0; font-size: 13px; color: var(--color-ink); }
.bank-opts { margin: 2px 0 0; font-size: 11px; }
.builtin { font-size: 11px; }

@media (max-width: 767px) {
  .stat-card { justify-content: center; }
  .stat-tip { text-align: center; }
  .starter-actions { flex-direction: column; }
  .round-del { position: static; align-self: flex-end; margin-top: 8px; }
}
:global(.quiz-modal) { padding: 0 !important; }
@media (max-width: 767px) {
  :global(.quiz-modal) { width: 100vw !important; max-width: 100vw !important; height: 100dvh; margin: 0; border-radius: 0; }
}
</style>
