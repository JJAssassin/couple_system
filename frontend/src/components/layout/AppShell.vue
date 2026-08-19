<template>
  <div class="shell">
    <!-- 全局极淡柔光背景：固定定位，置于所有内容之下、画布之上，贯穿全站 -->
    <teleport to="body">
      <AuroraBackdrop global />
      <RouteProgress />
    </teleport>

    <!-- 桌面 / 平板：常驻侧边栏 -->
    <Sidebar
      v-if="!isMobile()"
      class="shell-side"
      :collapsed="isTablet"
    />

    <!-- 移动端：汉堡抽屉 -->
    <teleport to="body">
      <transition name="drawer-fade">
        <div v-if="isMobile() && drawerOpen" class="drawer-mask" @click="drawerOpen = false" />
      </transition>
      <transition name="drawer-slide">
        <aside v-if="isMobile() && drawerOpen" class="drawer">
          <Sidebar :collapsed="false" @navigated="drawerOpen = false" />
        </aside>
      </transition>
    </teleport>

    <div class="shell-main">
      <!-- 顶部磨砂导航条：面包屑 + 双人头像 + 主题切换 + 退出 -->
      <header class="topbar">
        <button v-if="isMobile()" class="tb-icon" aria-label="菜单" @click="drawerOpen = true">
          <Menu :size="20" :stroke-width="1.8" />
        </button>
        <nav class="crumb">
          <span class="crumb-root">我们的小世界</span>
          <ChevronRight v-if="currentTitle" class="crumb-sep" :size="15" />
          <span v-if="currentTitle" class="crumb-cur">{{ currentTitle }}</span>
        </nav>

        <div class="tb-right">
          <button class="tb-icon" :aria-label="setting.dark ? '切换浅色' : '切换深色'" @click="setting.toggleDark()">
            <component :is="setting.dark ? Sun : Moon" :size="18" :stroke-width="1.8" />
          </button>

          <div class="avatars" :title="partnerName ? `你 & ${partnerName}` : '你'">
            <span class="avatar" :class="{ 'has-img': !!meAvatar }">
              <img v-if="meAvatar" :src="meAvatar" alt="" />
              <template v-else>{{ meInitial }}</template>
            </span>
            <span class="avatar avatar-mate" :class="{ 'has-img': !!mateAvatar, online: partnerOnline }">
              <img v-if="mateAvatar" :src="mateAvatar" alt="" />
              <template v-else>{{ mateInitial }}</template>
            </span>
          </div>

          <button class="tb-icon tb-logout" aria-label="退出登录" @click="logout">
            <LogOut :size="18" :stroke-width="1.8" />
          </button>
        </div>
      </header>

      <!-- 内容区（URL 驱动，淡入淡出转场） -->
      <div class="content">
        <router-view v-slot="{ Component }">
          <PageTransition>
            <component :is="Component" />
          </PageTransition>
        </router-view>
      </div>
    </div>

    <!-- 移动端底部 TabBar -->
    <nav class="tabbar" v-if="isMobile()">
      <router-link
        v-for="t in tabItems"
        :key="t.to"
        :to="t.to"
        class="tab"
        @click="drawerOpen = false"
      >
        <component :is="t.icon" :size="22" :stroke-width="1.8" />
        <span class="tab-lbl">{{ t.label }}</span>
      </router-link>
      <button class="tab" aria-label="更多" @click="drawerOpen = true">
        <Menu :size="22" :stroke-width="1.8" />
        <span class="tab-lbl">更多</span>
      </button>
    </nav>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  Menu, Moon, Sun, LogOut, ChevronRight,
  Home, Wallet, MessageCircle, Sparkles,
} from 'lucide-vue-next';
import Sidebar from './Sidebar.vue';
import PageTransition from '@/components/Common/PageTransition.vue';
import AuroraBackdrop from '@/components/Common/AuroraBackdrop.vue';
import RouteProgress from '@/components/Common/RouteProgress.vue';
import { prefetchRoutes } from '@/router';
import { isMobile, useDevice } from '@/composables/useDevice';
import { useAuthStore } from '@/store/authStore';
import { usePartnerStore } from '@/store/partnerStore';
import { useSettingStore } from '@/store/settingStore';
import { useRealtime } from '@/composables/useRealtime';

useDevice();
const route = useRoute();
const router = useRouter();
const auth = useAuthStore();
const partner = usePartnerStore();
const setting = useSettingStore();
const { partnerOnline } = useRealtime();

const drawerOpen = ref(false);

// 移动端底部 TabBar：4 个主入口 + 「更多」打开抽屉（完整导航）
const tabItems = [
  { to: '/home', label: '首页', icon: Home },
  { to: '/account', label: '记账', icon: Wallet },
  { to: '/board', label: '留言板', icon: MessageCircle },
  { to: '/quiz', label: '默契', icon: Sparkles },
];

/* 平板断点：768–1023 收起为图标栏 */
const isTablet = ref(false);
function evalTablet() {
  isTablet.value = window.matchMedia('(min-width: 768px) and (max-width: 1023px)').matches;
}
onMounted(() => {
  evalTablet();
  window.addEventListener('resize', evalTablet);
  if (!partner.status) partner.load();
  // 首屏后空闲预取所有页面 chunk，消除点击导航的等待感
  prefetchRoutes();
  // 离线数据预取（方向④）：空闲静默拉取核心模块读接口，SW 会写入 pw-api-v1 缓存，
  // 弱网/离线时前端自动读缓存降级（见 request.ts readApiCache）
  prefetchData();
});
onUnmounted(() => window.removeEventListener('resize', evalTablet));

/* 离线数据预取：核心模块读接口（GET），供 SW 缓存；失败静默忽略 */
function prefetchData() {
  if (!('caches' in window) || !auth.accessToken) return;
  const year = new Date().getFullYear();
  const urls = [
    '/api/home/dashboard',
    '/api/home/loveinfo',
    '/api/wish/list',
    '/api/album/list?page=1&pageSize=5',
    '/api/anniversary/list?page=1&pageSize=50',
    '/api/timeline/list',
    `/api/account/list?year=${year}&month=8`,
  ];
  const h = { Authorization: `Bearer ${auth.accessToken}` };
  const run = () => urls.forEach((u) => fetch(u, { headers: h }).catch(() => {}));
  if ('requestIdleCallback' in window) {
    (window as unknown as { requestIdleCallback: (cb: () => void) => void }).requestIdleCallback(run);
  } else {
    setTimeout(run, 2500);
  }
}

/* 面包屑当前页标题（与 Sidebar 导航项对应） */
const TITLE_MAP: Record<string, string> = {
  home: '首页', timeline: '时间轴', diary: '日记', wish: '愿望', todo: '待办', board: '留言板', quiz: '默契问答', album: '相册',
  conflict: '矛盾', letter: '书信', account: '记账', dateplan: '约会',
  footprint: '足迹', anniversary: '纪念日', message: '消息', setting: '设置', stats: '我们的一年',
};
const currentTitle = computed(() => {
  const name = (route.name as string) || (route.path.split('/')[1] || '');
  return TITLE_MAP[name] || '';
});

/* 双人头像 */
const meAvatar = computed(() => auth.profile?.avatar || '');
const mateAvatar = computed(() => partner.status?.partner?.avatar || '');
const meInitial = computed(() => (auth.profile?.nickName || '我').slice(0, 1).toUpperCase());
const mateInitial = computed(() => (partner.status?.partner?.nickName || 'TA').slice(0, 1).toUpperCase());
const partnerName = computed(() => partner.status?.partner?.nickName || '');

function logout() {
  auth.logout();
  router.push('/login');
}
</script>

<style scoped>
.shell { display: flex; min-height: 100vh; background: transparent; }
.shell-side { flex: 0 0 auto; }
.shell-main { flex: 1; min-width: 0; display: flex; flex-direction: column; }

/* 顶部磨砂条 */
.topbar {
  position: sticky; top: 0; z-index: 40;
  display: flex; align-items: center; gap: 14px;
  height: 60px; padding: 0 24px;
  background: color-mix(in srgb, var(--color-surface) 78%, transparent);
  backdrop-filter: saturate(180%) blur(12px);
  -webkit-backdrop-filter: saturate(180%) blur(12px);
  border-bottom: 1px solid var(--color-border);
}
.crumb { display: flex; align-items: center; gap: 8px; min-width: 0; }
.crumb-root { font-size: 13px; color: var(--color-ink-3); white-space: nowrap; }
.crumb-sep { color: var(--color-ink-3); flex: 0 0 auto; }
.crumb-cur { font-size: 15px; font-weight: 600; color: var(--color-ink); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.tb-right { margin-left: auto; display: flex; align-items: center; gap: 10px; }
.tb-icon {
  display: inline-flex; align-items: center; justify-content: center;
  width: 38px; height: 38px; border-radius: 10px; cursor: pointer;
  border: 1px solid transparent; background: transparent; color: var(--color-ink-2);
  transition: all var(--dur-micro) var(--ease-love);
}
.tb-icon:hover { color: var(--color-rose); background: var(--color-rose-soft); }
.tb-logout:hover { color: var(--color-rose); }

.avatars { display: flex; align-items: center; }
.avatar {
  width: 34px; height: 34px; border-radius: 50%; display: grid; place-items: center;
  font-size: 13px; font-weight: 600; color: #fff; overflow: hidden;
  background: linear-gradient(135deg, var(--color-rose), var(--color-rose-deep));
  border: 2px solid var(--color-surface);
}
.avatar img { width: 100%; height: 100%; object-fit: cover; }
.avatar-mate { margin-left: -10px; background: linear-gradient(135deg, var(--color-cocoa), var(--color-ink-2)); }
.avatar-mate.online { box-shadow: 0 0 0 2px var(--color-surface), 0 0 0 4px #43d17a; }

.content { flex: 1; width: 100%; max-width: 1200px; margin: 0 auto; padding: 32px 24px 48px; }
@media (max-width: 767px) {
  .topbar { padding: 0 14px; gap: 10px; }
  .content { padding: 20px 16px 84px; }
  .crumb-root { display: none; }
  .crumb-cur { display: none; }
  .tb-right { gap: 6px; }
  .avatar { width: 30px; height: 30px; font-size: 12px; }
  .avatar-mate { margin-left: -8px; }
  .avatar-mate.online { box-shadow: 0 0 0 2px var(--color-surface), 0 0 0 3.5px #43d17a; }
}

/* 移动端抽屉 */
.drawer-mask { position: fixed; inset: 0; z-index: 70; background: rgba(31, 41, 55, 0.4); }
.drawer {
  position: fixed; top: 0; left: 0; bottom: 0; z-index: 71; width: 248px;
  box-shadow: 8px 0 28px rgba(31, 41, 55, 0.18);
}

/* 移动端底部 TabBar */
.tabbar {
  position: fixed; left: 0; right: 0; bottom: 0; z-index: 50;
  display: flex; align-items: stretch; justify-content: space-around;
  height: 58px; padding-bottom: env(safe-area-inset-bottom);
  background: color-mix(in srgb, var(--color-surface) 88%, transparent);
  backdrop-filter: saturate(180%) blur(12px);
  -webkit-backdrop-filter: saturate(180%) blur(12px);
  border-top: 1px solid var(--color-border);
}
.tab {
  flex: 1; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 3px;
  color: var(--color-ink-3); text-decoration: none; cursor: pointer; background: none; border: none;
  font-size: 11px; padding: 4px 0; transition: color var(--dur-micro) var(--ease-love);
}
.tab.router-link-active { color: var(--color-rose); }
.tab:active { transform: scale(0.94); }
.tab-lbl { line-height: 1; }
.drawer-fade-enter-active, .drawer-fade-leave-active { transition: opacity var(--dur-pop) var(--ease-love); }
.drawer-fade-enter-from, .drawer-fade-leave-to { opacity: 0; }
.drawer-slide-enter-active, .drawer-slide-leave-active { transition: transform var(--dur-pop) var(--ease-love); }
.drawer-slide-enter-from, .drawer-slide-leave-to { transform: translateX(-100%); }
</style>
