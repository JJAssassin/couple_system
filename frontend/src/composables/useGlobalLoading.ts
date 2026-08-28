import { ref } from 'vue';

/**
 * 全局「加载反馈」状态机：用计数式 pending 跟踪在途请求数，
 * 供顶部加载条（GlobalLoadingBar）显示/隐藏。
 *
 * 设计要点：
 * - 计数而非布尔，天然支持并发请求（多个请求同时进行时条仍在）。
 * - 首次开始有 120ms 延迟再显示，过滤掉瞬时命中缓存的响应，避免条「闪一下」。
 * - pending 归零后延迟 360ms 再隐藏，给「填满→淡出」收尾动画留时间。
 * - 纯计数，不依赖任何 UI 库；组件与拦截器各取所需。
 */

const pending = { n: 0 };
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

function start() {
  pending.n++;
  if (pending.n === 1) {
    finishing.value = false;
    if (hideTimer) {
      clearTimeout(hideTimer);
      hideTimer = null;
    }
    clearShow();
    showTimer = setTimeout(() => {
      visible.value = true;
    }, 120);
  }
}

function end() {
  if (pending.n <= 0) return;
  pending.n--;
  if (pending.n === 0) {
    clearShow();
    if (visible.value) {
      finishing.value = true;
      hideTimer = setTimeout(() => {
        visible.value = false;
        finishing.value = false;
      }, 360);
    }
  }
}

export function useGlobalLoading() {
  return { visible, finishing, start, end };
}
