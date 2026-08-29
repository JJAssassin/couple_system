<template>
  <section class="block">
    <IndSectionTitle label="恋爱里程碑" :led="true" />
    <div class="ms-strip">
      <div v-for="m in list" :key="m.days" class="ms" :class="{ reached: m.reached }" role="button" tabindex="0" :aria-label="m.title" @click="go" @keydown.enter.prevent="go" @keydown.space.prevent="go">
        <span class="ms-ico"><component :is="m.icon" :size="20" :stroke-width="1.8" /></span>
        <div class="ms-title">{{ m.title }}</div>
        <div class="ms-days">{{ m.days }} 天</div>
        <div v-if="m.reached" class="ms-tag">已达成 · {{ m.reachedDate }}</div>
        <template v-else>
          <div class="ms-bar"><i :style="{ width: m.progress + '%' }"></i></div>
          <div class="ms-left">还差 {{ m.days - totalDays }} 天</div>
        </template>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRouter } from 'vue-router';
import { Heart, Star, Cake, Gem, Trophy, Sparkles, Leaf, Rainbow } from 'lucide-vue-next';
import IndSectionTitle from '@/components/industrial/IndSectionTitle.vue';

const props = defineProps<{ totalDays: number; loveStartTime?: string | null }>();
const router = useRouter();

const DEFS = [
  { days: 100, title: '百日之好', icon: Leaf },
  { days: 200, title: '二百日', icon: Sparkles },
  { days: 365, title: '一周年', icon: Cake },
  { days: 521, title: '我爱你', icon: Heart },
  { days: 730, title: '两周年', icon: Star },
  { days: 1000, title: '千日相伴', icon: Rainbow },
  { days: 1314, title: '一生一世', icon: Gem },
  { days: 1825, title: '五周年', icon: Trophy },
];

const list = computed(() => {
  const start = props.loveStartTime ? new Date(props.loveStartTime) : null;
  const startValid = start && !isNaN(start.getTime());
  return DEFS.map((d) => {
    const reached = props.totalDays >= d.days;
    let reachedDate = '';
    if (reached && startValid) {
      const r = new Date(start!);
      r.setDate(r.getDate() + d.days);
      reachedDate = `${r.getMonth() + 1}月${r.getDate()}日`;
    }
    return {
      ...d,
      reached,
      reachedDate,
      progress: Math.min(100, Math.round((props.totalDays / d.days) * 100)),
    };
  });
});

function go() {
  router.push('/anniversary');
}
</script>

<style scoped>
.ms-strip {
  display: flex;
  gap: 10px;
  overflow-x: auto;
  /* overflow-x:auto 会让 overflow-y 计算为 auto，悬停 translateY(-3px) 会被裁切；
     顶部留出空间让抬升动效完整显示（底部留白给横向滚动条） */
  padding: 8px 0 6px;
  scroll-snap-type: x mandatory;
}
.ms {
  flex: 0 0 134px;
  scroll-snap-align: start;
  cursor: pointer;
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: 14px 12px;
  text-align: center;
  box-shadow: var(--shadow-card);
  transition: transform var(--dur-pop) var(--ease-love), box-shadow var(--dur-pop) var(--ease-love);
}
.ms:hover {
  transform: translateY(-3px);
}
.ms.reached {
  background: linear-gradient(160deg, var(--color-rose-soft), var(--color-surface));
  border-color: var(--color-rose);
}
.ms-ico {
  color: var(--color-rose-text);
  display: inline-flex;
}
.ms-title {
  font-weight: 600;
  font-size: 13px;
  color: var(--color-ink);
  margin-top: 4px;
}
.ms-days {
  font-size: 12px;
  color: var(--color-ink-3);
  font-family: var(--font-mono);
}
.ms.reached .ms-days {
  color: var(--color-rose-text);
}
.ms-tag {
  font-size: 11px;
  color: var(--color-rose-text);
  margin-top: 8px;
}
.ms-bar {
  height: 6px;
  border-radius: 999px;
  background: var(--color-mist);
  margin-top: 8px;
  overflow: hidden;
}
.ms-bar i {
  display: block;
  height: 100%;
  border-radius: 999px;
  background: linear-gradient(90deg, var(--color-rose-deep), var(--color-rose));
  transition: width var(--dur-page) var(--ease-love);
}
.ms-left {
  font-size: 11px;
  color: var(--color-ink-3);
  margin-top: 6px;
}
</style>
