import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '@/store/authStore';
import { useGlobalLoading } from '@/composables/useGlobalLoading';

// 集中管理懒加载函数，便于空闲时预取（消除点击导航的“等一会才有反应”）
const loaders = {
  Login: () => import('@/views/Login.vue'),
  AppShell: () => import('@/components/layout/AppShell.vue'),
  Home: () => import('@/views/Home/Index.vue'),
  Timeline: () => import('@/views/Timeline/Index.vue'),
  Diary: () => import('@/views/Diary/Index.vue'),
  Wish: () => import('@/views/Wish/Index.vue'),
  Todo: () => import('@/views/Todo/Index.vue'),
  Board: () => import('@/views/Board/Index.vue'),
  Quiz: () => import('@/views/Quiz/Index.vue'),
  Album: () => import('@/views/Album/Index.vue'),
  Message: () => import('@/views/Message/Index.vue'),
  Conflict: () => import('@/views/Conflict/Index.vue'),
  Account: () => import('@/views/Account/Index.vue'),
  DatePlan: () => import('@/views/DatePlan/Index.vue'),
  Footprint: () => import('@/views/Footprint/Index.vue'),
  Anniversary: () => import('@/views/Anniversary/Index.vue'),
  Stats: () => import('@/views/Stats/Index.vue'),
  Setting: () => import('@/views/Setting/Index.vue'),
  FinesseShowcase: () => import('@/interactions/Showcase.vue'),
};

const router = createRouter({
  history: createWebHistory(),
  scrollBehavior: () => ({ top: 0 }),
  routes: [
    {
      path: '/login',
      name: 'Login',
      component: loaders.Login,
      meta: { public: true },
    },
    {
      path: '/',
      component: loaders.AppShell,
      meta: { requiresAuth: true },
      children: [
        { path: '', redirect: '/home' },
        { path: 'home', name: 'Home', component: loaders.Home },
        { path: 'timeline', name: 'Timeline', component: loaders.Timeline },
        { path: 'diary', name: 'Diary', component: loaders.Diary },
        { path: 'wish', name: 'Wish', component: loaders.Wish },
        { path: 'todo', name: 'Todo', component: loaders.Todo },
        { path: 'board', name: 'Board', component: loaders.Board },
        { path: 'quiz', name: 'Quiz', component: loaders.Quiz },
        { path: 'album', name: 'Album', component: loaders.Album },
        { path: 'message', name: 'Message', component: loaders.Message },
        { path: 'conflict', name: 'Conflict', component: loaders.Conflict },
        { path: 'account', name: 'Account', component: loaders.Account },
        { path: 'dateplan', name: 'DatePlan', component: loaders.DatePlan },
        { path: 'footprint', name: 'Footprint', component: loaders.Footprint },
        { path: 'anniversary', name: 'Anniversary', component: loaders.Anniversary },
        { path: 'stats', name: 'Stats', component: loaders.Stats },
        { path: 'setting', name: 'Setting', component: loaders.Setting },
        { path: 'finesse', name: 'FinesseShowcase', component: loaders.FinesseShowcase, meta: { hidden: true } },
      ],
    },
    { path: '/:pathMatch(.*)*', redirect: '/home' },
  ],
});

router.beforeEach((to) => {
  const auth = useAuthStore();
  if (to.meta.requiresAuth && !auth.accessToken) return '/login';
  if (to.path === '/login' && auth.accessToken) return '/home';
});

// 路由切换期间显示顶部加载条：与请求层加载计数互补，构成「加载→失败」完整体验链。
// 用布尔信号（startNav/endNav）而非计数，规避守卫重定向导致的 start/end 不配对泄漏。
router.beforeEach(() => {
  useGlobalLoading().startNav();
});
router.afterEach(() => {
  useGlobalLoading().endNav();
});
router.onError(() => {
  useGlobalLoading().endNav();
});

// 空闲时预取所有页面 chunk：首屏后后台静默加载，后续点击导航即瞬时切换
export function prefetchRoutes() {
  const run = () => {
    Object.values(loaders).forEach((load) => {
      try { load(); } catch { /* 忽略预取异常 */ }
    });
  };
  if (typeof window !== 'undefined' && 'requestIdleCallback' in window) {
    (window as unknown as { requestIdleCallback: (cb: () => void) => void }).requestIdleCallback(run);
  } else {
    setTimeout(run, 1500);
  }
}

export default router;
