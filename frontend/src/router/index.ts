import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '@/store/authStore';

// 集中管理懒加载函数，便于空闲时预取（消除点击导航的“等一会才有反应”）
const loaders = {
  Login: () => import('@/views/Login.vue'),
  AppShell: () => import('@/components/layout/AppShell.vue'),
  Home: () => import('@/views/Home/Index.vue'),
  Timeline: () => import('@/views/Timeline/Index.vue'),
  Diary: () => import('@/views/Diary/Index.vue'),
  Wish: () => import('@/views/Wish/Index.vue'),
  Todo: () => import('@/views/Todo/Index.vue'),
  Album: () => import('@/views/Album/Index.vue'),
  Message: () => import('@/views/Message/Index.vue'),
  Conflict: () => import('@/views/Conflict/Index.vue'),
  Letter: () => import('@/views/Letter/Index.vue'),
  Account: () => import('@/views/Account/Index.vue'),
  DatePlan: () => import('@/views/DatePlan/Index.vue'),
  Footprint: () => import('@/views/Footprint/Index.vue'),
  Anniversary: () => import('@/views/Anniversary/Index.vue'),
  Setting: () => import('@/views/Setting/Index.vue'),
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
        { path: 'album', name: 'Album', component: loaders.Album },
        { path: 'message', name: 'Message', component: loaders.Message },
        { path: 'conflict', name: 'Conflict', component: loaders.Conflict },
        { path: 'letter', name: 'Letter', component: loaders.Letter },
        { path: 'account', name: 'Account', component: loaders.Account },
        { path: 'dateplan', name: 'DatePlan', component: loaders.DatePlan },
        { path: 'footprint', name: 'Footprint', component: loaders.Footprint },
        { path: 'anniversary', name: 'Anniversary', component: loaders.Anniversary },
        { path: 'setting', name: 'Setting', component: loaders.Setting },
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
