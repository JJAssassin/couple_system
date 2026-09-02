<template>
  <IndSkeleton v-if="loading" variant="grid" :rows="6" :columns="3" />
  <div v-else class="anniv-page" ref="container">
    <!-- 品牌条 -->
    <div class="brand block">
      <IpIcon name="module_anniversary" :size="28" class="brand-icon" alt="纪念日" />
      <h1 class="ind-label">ANNIVERSARY · 纪念日</h1>
      <span class="brand-status"><IndLed color="green" :size="9" /> 已同步</span>
    </div>

    <!-- 顶部「下一个重要的日子」英雄卡 -->
    <section v-if="hero" class="block hero" :class="{ today: heroToday }">
      <div class="hero-main">
        <div class="hero-kicker">距离下一个纪念日</div>
        <div class="hero-name">
          <component :is="typeMeta(hero.anniversaryType).icon" :size="22" class="hero-ico" />
          <span>{{ hero.name }}</span>
          <span v-if="hero.isYearly && occNumber(hero)" class="hero-occ">第 {{ occNumber(hero) }} 周年</span>
        </div>

        <div v-if="heroToday" class="hero-today"><PartyPopper :size="26" /> 就是今天！</div>
        <div v-else class="hero-count">
          <GradientText tag="span" class="hero-days">{{ heroCd?.d }}</GradientText>
          <span class="hero-unit">天</span>
          <span class="hero-hms">{{ pad(heroCd?.h) }}:{{ pad(heroCd?.m) }}:{{ pad(heroCd?.s) }}</span>
        </div>

        <div class="hero-sub">
          下次 <b>{{ fmtDate(hero.nextOccurrence) }}</b>
          <span v-if="hero.lunarDate" class="ac-lunar">{{ hero.lunarDate }}</span>
          <span class="dot-sep">·</span>提前 {{ hero.remindDays }} 天提醒
          <span v-if="isReminderNear(hero)" class="hero-remind">提醒临近</span>
        </div>
      </div>
      <div v-if="hero.isYearly && !heroToday" class="hero-ring">
        <IndProgressRing :value="yearProgress(hero)" :size="112" :stroke="11" color="var(--color-rose)" sublabel="本周年进度" />
      </div>
    </section>

    <!-- 无即将到来时的历史回顾英雄卡 -->
    <section v-else-if="pastHero" class="block hero past">
      <div class="hero-main">
        <div class="hero-kicker">最近的纪念日</div>
        <div class="hero-name">
          <component :is="typeMeta(pastHero.anniversaryType).icon" :size="22" class="hero-ico" />
          <span>{{ pastHero.name }}</span>
        </div>
        <div class="hero-count">
          <span class="hero-past">已过去</span>
          <GradientText tag="span" class="hero-days">{{ daysSince(pastHero) }}</GradientText>
          <span class="hero-unit">天</span>
        </div>
        <div class="hero-sub">目标日 {{ fmtDate(pastHero.targetDate) }}<span v-if="pastHero.lunarDate" class="ac-lunar">{{ pastHero.lunarDate }}</span> · 今年已无更多纪念日</div>
      </div>
    </section>

    <section class="block head-row">
      <IndSectionTitle label="我们的重要日子" :led="true" />
      <button class="add-btn uvi-glow-border" v-press-bounce @click="openCreate">＋ 新增纪念日</button>
    </section>

    <section class="block">
      <div v-if="items.length" class="anniv-grid">
        <TiltCard
          v-for="a in items"
          :key="a.id"
          class="anniv-card-wrap"
        >
          <div
            class="anniv-card"
            :class="{ pop: poppingId === a.id, near: isNear(a), soon: isSoon(a) }"
          >
          <div v-if="a.coverImage" class="ac-cover" :style="{ backgroundImage: `url(${assetUrl(a.coverImage)})` }" />

          <div class="ac-top">
            <component :is="typeMeta(a.anniversaryType).icon" class="ac-type-ico" :size="20" />
            <span class="ac-name">{{ a.name }}</span>
            <NTag v-if="a.isYearly" size="small" type="primary" round class="ac-yearly">每年</NTag>
            <NTag v-else size="small" :bordered="true" class="ac-once">一次性</NTag>
          </div>

          <div class="ac-meta">
            <span>目标日 {{ fmtDate(a.targetDate) }}</span>
            <span v-if="a.lunarDate" class="ac-lunar">· {{ a.lunarDate }}</span>
            <span class="dot-sep">·</span>
            <span>提前 {{ a.remindDays }} 天提醒</span>
          </div>

          <div class="ac-next">
            <template v-if="a.nextOccurrence">
              <span v-if="isToday(a)" class="ac-today"><PartyPopper :size="16" /> 就是今天！</span>
              <template v-else>
                <span>还有 </span>
                <GradientText tag="span" class="ac-days">{{ cd(a.nextOccurrence)?.d }}</GradientText>
                <span> 天</span>
                <span v-if="(cd(a.nextOccurrence)?.d ?? 99) <= 2" class="ac-hms">
                  {{ pad(cd(a.nextOccurrence)?.h) }}:{{ pad(cd(a.nextOccurrence)?.m) }}:{{ pad(cd(a.nextOccurrence)?.s) }}
                </span>
                <div class="ac-next-date">下次 {{ fmtDate(a.nextOccurrence) }}<span v-if="a.lunarDate" class="ac-lunar">· {{ a.lunarDate }}</span></div>
              </template>
            </template>
            <template v-else>
              <span class="ac-expired">这一天已经过去 {{ daysSince(a) }} 天</span>
            </template>
          </div>

          <div class="ac-badges">
            <span v-if="a.isYearly && occNumber(a)" class="badge occ">第 {{ occNumber(a) }} 周年</span>
            <span v-if="isReminderNear(a)" class="badge remind">提醒临近</span>
            <span v-if="isToday(a)" class="badge today">今天</span>
          </div>

          <div class="ac-actions">
            <button class="ac-btn" @click="openPoster(a)"><Palette :size="14" /> 海报</button>
            <button class="ac-btn" @click="openEdit(a)"><Pencil :size="14" /> 编辑</button>
            <n-popconfirm
              positive-text="删除"
              negative-text="取消"
              @positive-click="onDelete(a)"
            >
              <template #trigger>
                <button class="ac-btn danger"><Trash2 :size="14" /> 删除</button>
              </template>
              确定删除「{{ a.name }}」吗？相关提醒也会一并移除。
            </n-popconfirm>
          </div>
        </div>
        </TiltCard>
      </div>
      <IndEmpty v-else title="还没有纪念日" desc="点「新增纪念日」，把恋爱纪念日、生日、初见都记下来，每年自动提醒" />
    </section>

    <!-- 历史回顾统计 -->
    <section v-if="hasHistory" class="block history">
      <IndSectionTitle label="历史回顾" :led="true" />
      <div class="hist-row">
        <div class="hist-stat"><b>{{ yearlyCount }}</b><span>个每年纪念日</span></div>
        <div class="hist-stat"><b>{{ maxOcc }}</b><span>共同走过最久（周年）</span></div>
        <div class="hist-stat"><b>{{ pastCount }}</b><span>已过去的单次纪念</span></div>
      </div>
    </section>

    <!-- 新增 / 编辑 弹窗 -->
    <LoveSheet v-model="showForm" :title="editingId ? '编辑纪念日' : '新增纪念日'">
      <LoveInput
        v-model="form.name"
        label="名称"
        placeholder="例如：恋爱纪念日 / 我的生日 / 初次相遇"
        :maxlength="30"
        :counter="true"
        :invalid="nameInvalid"
      />
      <LoveSegmented v-model="form.anniversaryType" label="类型" :options="typeOptions" />
      <LoveDateField v-model="form.dateTs" label="目标日期" />
      <LoveSegmented v-model="form.remindDays" label="提前提醒" :options="remindOptions" />
      <div class="yearly-row">
        <div class="yearly-text">
          <div class="yearly-title">是否每年重复</div>
          <div class="yearly-hint">{{ form.isYearly ? '每年同一天自动提醒' : '仅此一次，过期不再提醒' }}</div>
        </div>
        <n-switch v-model:value="form.isYearly" aria-label="是否每年重复" />
      </div>
      <div class="lf-field">
        <span class="lf-label">封面图（可选）</span>
        <ImageField v-model="form.coverImage" />
      </div>
      <template #footer>
        <LoveSaveBar
          :loading="submitting"
          :success="saved"
          cancel-text="取消"
          save-text="保存"
          @cancel="showForm = false"
          @save="submit"
        />
      </template>
    </LoveSheet>

    <!-- 纪念日分享海报 -->
    <AnniversaryPoster ref="posterRef" :anniversary="selectedAnniversary" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed, type Component } from 'vue';
import { Heart, Cake, Handshake, Sparkles, PartyPopper, Palette, Pencil, Trash2 } from 'lucide-vue-next';
import { NSwitch, NTag, NPopconfirm } from 'naive-ui';
import { LoveSheet, LoveInput, LoveSegmented, LoveDateField, LoveSaveBar } from '@/components/loveform';
import type { AnniversaryDto, AnniversaryReq } from '@/types';
import {
  listAnniversaries, createAnniversary, updateAnniversary, deleteAnniversary,
} from '@/api/anniversary';
import { useRealtime, overlaySyncMap } from '@/composables/useRealtime';
import { useStaggerEnter } from '@/composables/useAnimation';
import { useSyncSettle } from '@/composables/useSyncSettle';
import IndSectionTitle from '@/components/industrial/IndSectionTitle.vue';
import IndEmpty from '@/components/industrial/IndEmpty.vue';
import { assetUrl } from '@/config/server';
import IndLed from '@/components/industrial/IndLed.vue';
import IndSkeleton from '@/components/industrial/IndSkeleton.vue';
import IndProgressRing from '@/components/industrial/IndProgressRing.vue';
import ImageField from '@/components/Common/ImageField.vue';
import GradientText from '@/components/Common/GradientText.vue';
import TiltCard from '@/components/Common/TiltCard.vue';
import AnniversaryPoster from '@/components/Common/AnniversaryPoster.vue';
import IpIcon from '@/components/Common/IpIcon.vue';
import { feedback } from '@/utils/feedback';

const { useModuleSync } = useRealtime();
const loading = ref(true);
const items = ref<AnniversaryDto[]>([]);
const container = ref<HTMLElement>();
const showForm = ref(false);
const submitting = ref(false);
const saved = ref(false);
const nameInvalid = ref(false);
const editingId = ref<number | null>(null);
const poppingId = ref<number | null>(null);
const selectedAnniversary = ref<AnniversaryDto | null>(null);
const posterRef = ref<InstanceType<typeof AnniversaryPoster> | null>(null);

function openPoster(a: AnniversaryDto) {
  selectedAnniversary.value = a;
  posterRef.value?.open();
}

/* ---------- 实时倒计时引擎：每秒刷新 now，驱动所有倒计时 ---------- */
const now = ref(Date.now());
let timer: number | undefined;
// 统一管理延迟回调（保存后关弹窗 / 删除动画后再移除），卸载时一次性清理，避免过期定时器
const pendingTimers = new Set<number>();
function later(fn: () => void, ms: number) {
  const id = window.setTimeout(() => { pendingTimers.delete(id); fn(); }, ms);
  pendingTimers.add(id);
}
interface Countdown { d: number; h: number; m: number; s: number; diff: number }
// 仅含日期的字符串（YYYY-MM-DD）按「本地日历日」解析，避免被当 UTC 零点而在负时区差一天
function parseLocalDate(s?: string | null): Date | null {
  if (!s) return null;
  if (/^\d{4}-\d{2}-\d{2}$/.test(s)) {
    const [y, mo, d] = s.split('-').map(Number);
    return new Date(y, mo - 1, d);
  }
  return new Date(s);
}
function cd(iso?: string | null): Countdown | null {
  if (!iso) return null;
  const target = parseLocalDate(iso)!.getTime();
  let diff = target - now.value;
  if (diff < 0) diff = 0;
  return {
    d: Math.floor(diff / 86_400_000),
    h: Math.floor((diff % 86_400_000) / 3_600_000),
    m: Math.floor((diff % 3_600_000) / 60_000),
    s: Math.floor((diff % 60_000) / 1000),
    diff,
  };
}
const pad = (n?: number) => String(n ?? 0).padStart(2, '0');

/* ---------- 派生信息 ---------- */
// 第 N 周年（仅每年重复）
function occNumber(a: AnniversaryDto): number | null {
  if (!a.isYearly || !a.nextOccurrence) return null;
  return parseLocalDate(a.nextOccurrence)!.getFullYear() - parseLocalDate(a.targetDate)!.getFullYear() + 1;
}
// 已过去天数（用于过期的一次性纪念日）
function daysSince(a: AnniversaryDto): number {
  const t = parseLocalDate(a.targetDate)?.getTime() ?? now.value;
  return Math.max(0, Math.floor((now.value - t) / 86_400_000));
}
// 是否「今天」就是目标日
function isToday(a: AnniversaryDto): boolean {
  return !!a.nextOccurrence && (cd(a.nextOccurrence)?.d ?? 1) === 0;
}
// 提醒临近：目标日落在提前提醒窗口内
function isReminderNear(a: AnniversaryDto): boolean {
  return !!a.nextOccurrence && a.remindDays > 0 && (cd(a.nextOccurrence)?.d ?? 999) <= a.remindDays;
}
// 卡片发光：临近 7 天 / 30 天
function isNear(a: AnniversaryDto): boolean {
  return !!a.nextOccurrence && (cd(a.nextOccurrence)?.d ?? 999) <= 7;
}
function isSoon(a: AnniversaryDto): boolean {
  return !!a.nextOccurrence && (cd(a.nextOccurrence)?.d ?? 999) <= 30;
}
// 本周年进度（仅每年重复）：自上次发生日到下次发生日的占比
function yearProgress(a: AnniversaryDto): number {
  if (!a.isYearly || !a.nextOccurrence) return 0;
  const next = parseLocalDate(a.nextOccurrence)!.getTime();
  const last = parseLocalDate(a.nextOccurrence)!;
  last.setFullYear(last.getFullYear() - 1);
  const lastMs = last.getTime();
  const pct = ((now.value - lastMs) / (next - lastMs)) * 100;
  return Math.max(0, Math.min(100, Math.round(pct)));
}

/* ---------- 顶部英雄卡 ---------- */
const hero = computed<AnniversaryDto | null>(() => {
  const ups = items.value
    .filter((a) => a.nextOccurrence)
    .map((a) => ({ a, c: cd(a.nextOccurrence) }))
    .filter((x) => x.c)
    .sort((x, y) => x.c!.diff - y.c!.diff);
  return ups[0]?.a ?? null;
});
const heroCd = computed(() => (hero.value ? cd(hero.value.nextOccurrence) : null));
const heroToday = computed(() => !!hero.value && (heroCd.value?.d ?? 1) === 0);
const pastHero = computed<AnniversaryDto | null>(() => {
  if (hero.value) return null;
  return items.value
    .filter((a) => !a.nextOccurrence)
    .sort((a, b) => (parseLocalDate(b.targetDate)?.getTime() ?? 0) - (parseLocalDate(a.targetDate)?.getTime() ?? 0))[0] ?? null;
});

/* ---------- 历史回顾统计 ---------- */
const yearlyCount = computed(() => items.value.filter((a) => a.isYearly).length);
const maxOcc = computed(() => {
  const ns = items.value.map(occNumber).filter((n): n is number => n != null);
  return ns.length ? Math.max(...ns) : 0;
});
const pastCount = computed(() => items.value.filter((a) => !a.nextOccurrence).length);
const hasHistory = computed(() => yearlyCount.value > 0 || pastCount.value > 0);

/* ---------- 类型元数据 ---------- */
const typeOptions = [
  { label: '恋爱纪念日', value: 1 },
  { label: '生日', value: 2 },
  { label: '初见', value: 3 },
  { label: '自定义', value: 4 },
];
const remindOptions = [
  { label: '当天（不提前）', value: 0 },
  { label: '提前 1 天', value: 1 },
  { label: '提前 3 天', value: 3 },
  { label: '提前 7 天', value: 7 },
  { label: '提前 15 天', value: 15 },
];
const typeIcon: Record<number, Component> = { 1: Heart, 2: Cake, 3: Handshake, 4: Sparkles };
function typeMeta(t: number) {
  return { icon: typeIcon[t] ?? Sparkles, label: typeOptions.find((o) => o.value === t)?.label ?? '自定义' };
}

/* ---------- 数据加载与表单 ---------- */
const emptyForm = () => ({
  name: '', anniversaryType: 1, dateTs: null as number | null, remindDays: 3, isYearly: true, coverImage: '',
});
const form = ref(emptyForm());

async function load() {
  loading.value = true;
  try {
    const r = await listAnniversaries(1, 100);
    items.value = r.items.sort((a, b) => (a.nextOccurrence ?? '9999').localeCompare(b.nextOccurrence ?? '9999'));
  } catch { /* 拦截器已提示 */ }
  finally { loading.value = false; }
}

function fmtDate(s?: string | null) {
  if (!s) return '—';
  const d = parseLocalDate(s);
  if (!d) return '—';
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

function openCreate() {
  editingId.value = null;
  form.value = emptyForm();
  saved.value = false;
  nameInvalid.value = false;
  showForm.value = true;
}
function openEdit(a: AnniversaryDto) {
  editingId.value = a.id;
  form.value = {
    name: a.name,
    anniversaryType: a.anniversaryType,
    dateTs: parseLocalDate(a.targetDate)!.getTime(),
    remindDays: a.remindDays,
    isYearly: a.isYearly,
    coverImage: a.coverImage ?? '',
  };
  saved.value = false;
  nameInvalid.value = false;
  showForm.value = true;
}

function toReq(): AnniversaryReq {
  return {
    name: form.value.name.trim(),
    anniversaryType: form.value.anniversaryType,
    targetDate: form.value.dateTs
      ? (() => {
          const dd = new Date(form.value.dateTs!);
          return `${dd.getFullYear()}-${String(dd.getMonth() + 1).padStart(2, '0')}-${String(dd.getDate()).padStart(2, '0')}`;
        })()
      : '',
    remindDays: form.value.remindDays,
    isYearly: form.value.isYearly,
    coverImage: form.value.coverImage.trim() || undefined,
  };
}

async function submit() {
  if (!form.value.name.trim()) {
    nameInvalid.value = true;
    feedback.warn('给纪念日起个名字吧～');
    return;
  }
  if (!form.value.dateTs) {
    feedback.warn('选个目标日期吧～');
    return;
  }
  submitting.value = true;
  saved.value = false;
  try {
    if (editingId.value) {
      const updated = await updateAnniversary(editingId.value, toReq());
      const i = items.value.findIndex((x) => x.id === updated.id);
      if (i >= 0) items.value[i] = updated;
      feedback.updated('纪念日');
    } else {
      const created = await createAnniversary(toReq());
      items.value.unshift(created);
      feedback.created('纪念日');
    }
    saved.value = true;
    later(() => { showForm.value = false; }, 680);
  } finally { submitting.value = false; }
}

async function onDelete(a: AnniversaryDto) {
  try {
    await deleteAnniversary(a.id);
    // 先标记 pop 动画，等 0.3s 动画播完再真正移除；修复原「先移除导致动画永不触发」
    poppingId.value = a.id;
    later(() => {
      items.value = items.value.filter((x) => x.id !== a.id);
      poppingId.value = null;
    }, 300);
    feedback.deleted('纪念日');
  } catch { /* 忽略 */ }
}

useStaggerEnter(container, '.block', { stagger: 0.1, y: 16 });

onMounted(async () => {
  timer = window.setInterval(() => (now.value = Date.now()), 1000);
  await load();
  loading.value = false;
  useModuleSync('anniversary', { items, getId: i => i.id, load, map: overlaySyncMap });
  // 伴侣在另一台设备新增/刷新纪念日时，卡片错落入场
  useSyncSettle('anniversary', container, items, '.anniv-card');
});
onUnmounted(() => {
  if (timer) clearInterval(timer);
  pendingTimers.forEach((id) => clearTimeout(id));
  pendingTimers.clear();
});
</script>

<style scoped>
.anniv-page { max-width: 880px; margin: 0 auto; }
.brand {
  display: flex; align-items: center; gap: 14px; padding: 12px 16px; margin-bottom: 8px;
  background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg);
  box-shadow: var(--shadow-card);
}
.brand-icon { margin-right: 2px; flex: 0 0 auto; }
.brand-status {
  margin-left: auto; display: inline-flex; align-items: center; gap: 6px;
  font-size: 12px; font-weight: 500;
  color: var(--color-ink-2);
  padding: 4px 12px; border-radius: 999px;
  background: var(--color-surface-2); border: 1px solid var(--color-border);
}
.ind-label { font-family: var(--font-mono); font-weight: 500; letter-spacing: 0.1em; font-size: 13px; color: var(--color-ink); margin: 0; }

.block { margin: 22px 0; }

/* ---------- 英雄卡 ---------- */
.hero {
  position: relative; display: flex; align-items: center; gap: 18px;
  padding: 22px 24px; border-radius: var(--radius-lg);
  background: linear-gradient(135deg, var(--color-rose-soft), var(--color-surface));
  border: 1px solid var(--color-rose-soft);
  box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04), 0 18px 44px -16px rgba(214, 100, 120, 0.34);
  overflow: hidden;
}
.hero.past { background: linear-gradient(135deg, var(--color-surface-2), var(--color-surface)); border-color: var(--color-border); box-shadow: var(--shadow-card); }
.hero.today { background: linear-gradient(135deg, var(--color-rose-soft), var(--color-surface)); animation: heroGlow 2.4s ease-in-out infinite; }
@keyframes heroGlow { 0%,100% { box-shadow: 0 1px 2px rgba(31,41,55,.04), 0 18px 44px -16px rgba(214,100,120,.34); } 50% { box-shadow: 0 1px 2px rgba(31,41,55,.04), 0 22px 60px -14px rgba(214,100,120,.6); } }
.hero-main { flex: 1; min-width: 0; }
.hero-kicker { font-size: 12px; letter-spacing: 0.12em; color: var(--color-ink-2); margin-bottom: 6px; text-transform: uppercase; }
.hero-name { display: flex; align-items: center; gap: 8px; font-family: var(--font-serif); font-size: 20px; font-weight: 800; color: var(--color-ink); margin-bottom: 10px; }
.hero-ico { color: var(--color-rose-text); flex: 0 0 auto; }
.hero-occ { font-size: 12px; font-weight: 600; color: var(--color-rose-text); background: var(--color-surface); border: 1px solid var(--color-rose-soft); padding: 2px 10px; border-radius: 999px; }
.hero-count { display: flex; align-items: baseline; gap: 6px; }
.hero-days { font-weight: 900; font-size: 44px; line-height: 1; font-variant-numeric: tabular-nums; font-feature-settings: "tnum" 1; letter-spacing: -0.03em; }
.hero-unit { font-size: 16px; color: var(--color-ink-2); font-weight: 600; }
.hero-hms { margin-left: 8px; font-family: var(--font-mono); font-size: 18px; font-weight: 600; color: var(--color-accent-text); letter-spacing: 0.02em; }
.hero-today { font-size: 30px; font-weight: 900; color: var(--color-rose-text); font-variant-numeric: tabular-nums; font-feature-settings: "tnum" 1; letter-spacing: -0.02em; }
.hero-past { font-size: 14px; color: var(--color-ink-2); margin-right: 4px; }
.hero-sub { margin-top: 10px; font-size: 13px; color: var(--color-ink-2); }
.hero-sub b { color: var(--color-ink); }
.hero-remind { margin-left: 8px; font-size: 11px; font-weight: 700; color: var(--color-rose-text); background: var(--color-rose-soft); border: 1px solid var(--color-rose-soft); padding: 2px 8px; border-radius: 999px; }
.hero-ring { flex: 0 0 auto; }
.dot-sep { margin: 0 6px; color: var(--color-ink-3); }

.head-row { display: flex; align-items: center; justify-content: space-between; }
.add-btn {
  border: 1px solid var(--color-border); cursor: pointer; padding: 9px 16px; border-radius: 999px;
  color: var(--color-rose-text); font-size: 13px; background: var(--color-rose-soft);
  box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04);
  transition: all var(--dur-micro) var(--ease-love);
}
.add-btn:active { transform: scale(0.97); }

/* ---------- 卡片网格 ---------- */
.anniv-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 16px; }
.anniv-card-wrap { display: block; transform-style: preserve-3d; }
.anniv-card {
  position: relative; height: 100%; padding: 18px 18px 14px; border-radius: var(--radius-lg);
  background: var(--color-surface); border: 1px solid var(--color-border);
  box-shadow: var(--shadow-card);
  transition: transform var(--dur-pop) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love), border-color var(--dur-pop) var(--ease-love);
}
html:not(.reduce-motion) .anniv-card:hover { box-shadow: 0 4px 12px rgba(31, 41, 55, 0.06), 0 18px 44px -12px rgba(122, 100, 98, 0.22); }
.anniv-card.near { border-color: var(--color-rose-soft); box-shadow: 0 4px 12px rgba(31,41,55,.06), 0 16px 40px -12px rgba(214,100,120,.28); }
.anniv-card.soon { border-color: color-mix(in srgb, var(--color-rose-soft) 55%, var(--color-border)); }
.anniv-card.pop { animation: acPop 0.3s var(--ease-love); }
@keyframes acPop { 0% { opacity: 1; } 50% { opacity: 0.3; } 100% { opacity: 1; } }

.ac-cover { height: 92px; margin: -18px -18px 14px; border-radius: var(--radius-lg) var(--radius-lg) 0 0; background-size: cover; background-position: center; }

.ac-top { display: flex; align-items: center; gap: 8px; margin-bottom: 12px; }
.ac-type-ico { color: var(--color-rose-text); }
.ac-name { font-family: var(--font-serif); font-size: 15px; font-weight: 700; color: var(--color-ink); }
.ac-yearly { margin-left: auto; }
.ac-once { margin-left: auto; color: var(--color-ink-3); }

.ac-meta { font-size: 12px; color: var(--color-ink-2); margin-bottom: 8px; font-family: var(--font-mono); }
.dot-sep { margin: 0 6px; color: var(--color-ink-3); }

.ac-next { font-size: 13px; color: var(--color-ink); margin-bottom: 8px; }
.ac-next b { color: var(--color-accent-text); }
.ac-days { font-weight: 800; font-size: 16px; }
.ac-hms { margin-left: 6px; font-family: var(--font-mono); font-size: 13px; font-weight: 600; color: var(--color-accent-text); }
.ac-next-date { font-size: 12px; color: var(--color-ink-3); margin-top: 2px; }
.ac-lunar { color: var(--color-rose-text); font-weight: 600; margin-left: 6px; white-space: nowrap; }
.ac-today { color: var(--color-rose-text); font-weight: 800; font-size: 15px; }
.ac-expired { color: var(--color-ink-3); }

.ac-badges { display: flex; flex-wrap: wrap; gap: 6px; margin-bottom: 12px; }
.badge { font-size: 11px; font-weight: 700; padding: 2px 9px; border-radius: 999px; }
.badge.occ { color: var(--color-rose-text); background: var(--color-rose-soft); border: 1px solid var(--color-rose-soft); }
.badge.remind { color: var(--color-rose-text); background: var(--color-rose-soft); border: 1px solid var(--color-rose-soft); }
.badge.today { color: var(--color-on-primary); background: var(--color-rose); }

.ac-actions { display: flex; gap: 10px; }
.ac-btn {
  flex: 1; display: inline-flex; align-items: center; justify-content: center; gap: 5px;
  border: 1px solid var(--color-border); cursor: pointer; padding: 8px 0; border-radius: var(--radius-md); font-size: 13px;
  background: var(--color-surface-2); color: var(--color-ink-2);
  box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04);
  transition: all var(--dur-micro) var(--ease-love);
}
.ac-btn:active { transform: scale(0.98); }
.ac-btn:hover { color: var(--color-rose-text); border-color: var(--color-rose-soft); background: var(--color-rose-soft); }
.ac-btn.danger { color: var(--color-rose-text); }

/* ---------- 历史回顾 ---------- */
.history .hist-row { display: grid; grid-template-columns: repeat(3, 1fr); gap: 12px; }
.hist-stat {
  text-align: center; padding: 16px 10px; border-radius: var(--radius-lg);
  background: var(--color-surface); border: 1px solid var(--color-border);
  box-shadow: 0 1px 2px rgba(31, 41, 55, 0.04);
}
.hist-stat b { display: block; font-size: 26px; font-weight: 900; color: var(--color-rose-text); font-family: var(--font-mono); }
.hist-stat span { font-size: 12px; color: var(--color-ink-3); }

.yearly-row { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 4px 2px; }
.yearly-text { min-width: 0; }
.yearly-title { font-size: 14px; font-weight: 500; color: var(--color-ink); }
.yearly-hint { font-size: 12px; color: var(--color-ink-3); margin-top: 2px; }
@media (max-width: 767px) {
  .brand { padding: 10px 14px; }
  .brand .ind-label { font-size: 12px; }
  .brand-status { padding: 3px 9px; font-size: 11px; }
  .hero { flex-direction: column; align-items: flex-start; gap: 14px; padding: 18px; }
  .hero-ring { align-self: center; }
  .hero-name { font-size: 17px; }
  .hero-count { flex-wrap: wrap; }
  .hero-days { font-size: 38px; }
  .hero-hms { margin-left: 0; margin-top: 6px; font-size: 16px; }
  .hero-remind { margin-left: 0; margin-top: 6px; }
  .hero-sub { display: flex; flex-wrap: wrap; gap: 4px; }
  .anniv-grid { grid-template-columns: 1fr; gap: 14px; }
  .history .hist-row { grid-template-columns: 1fr; }
  .hist-stat { padding: 14px 10px; }
  .ac-cover { height: 120px; }
  .head-row { gap: 12px; flex-wrap: wrap; }
  .add-btn { flex: 0 0 auto; }
}
</style>
