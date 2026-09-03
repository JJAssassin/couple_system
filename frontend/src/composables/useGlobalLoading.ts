import { ref, computed, watch } from 'vue';

/**
 * 全局「加载反馈」状态机，供顶部加载条（GlobalLoadingBar）显示/隐藏。
 *
 * 设计要点：
 * - reqPending：在途请求计数（计数式，天然支持并发请求，多个请求并行时条仍在）。
 * - navActive：路由导航是否进行中（布尔式）。用布尔而非计数，是为了规避
 *   「守卫重定向」场景下 beforeEach 触发的 start 没有对应 afterEach 配对，
 *   若用计数会永久泄漏、加载条卡死。
 * - active = reqPending>0 || navActive，对请求层与路由层统一驱动顶栏显隐。
 * - 首次进入加载态延迟 120ms 再显示，过滤瞬时命中缓存的响应，避免条「闪一下」；
 *   离开加载态后延迟 360ms 再隐藏，给「填满→淡出」收尾动画留时间。
 * - 纯信号，不依赖任何 UI 库；组件与拦截器各取所需。
 */

const reqPending = { n: 0 };
const navActive = ref(false);
const active = computed(() => reqPending.n > 0 || navActive.value);

const visible = ref(false);
const finishing = ref(false);
let showTimer: ReturnType<typeof setTimeout> | null = null;
let hideTimer: ReturnType<typeof setTimeout> | null = null;

function clearShow() {
  if (showTimer) {
    clearTimeout(showTimer);
    showTimer = null;
  }
}
function clearHide() {
  if (hideTimer) {
    clearTimeout(hideTimer);
    hideTimer = null;
  }
}

watch(active, (now, was) => {
  if (now && !was) {
    // 进入加载态：取消可能的收尾，延迟显示避免闪烁
    finishing.value = false;
    clearHide();
    clearShow();
    showTimer = setTimeout(() => {
      visible.value = true;
    }, 120);
  } else if (!now && was) {
    // 离开加载态：填满整条并淡出（由 .done 类承接动画）
    clearShow();
    if (visible.value) {
      finishing.value = true;
      hideTimer = setTimeout(() => {
        visible.value = false;
        finishing.value = false;
      }, 360);
    }
  }
});

/** 请求开始（请求拦截器调用，计数式） */
function start() {
  reqPending.n++;
}
/** 请求结束（响应/错误拦截器调用，计数式） */
function end() {
  if (reqPending.n > 0) reqPending.n--;
}
/** 路由导航开始（router.beforeEach 调用，布尔式） */
function startNav() {
  navActive.value = true;
}
/** 路由导航结束（router.afterEach / onError 调用，布尔式） */
function endNav() {
  navActive.value = false;
}

export function useGlobalLoading() {
  return { visible, finishing, start, end, startNav, endNav };
}
