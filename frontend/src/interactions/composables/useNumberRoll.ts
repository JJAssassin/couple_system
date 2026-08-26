import { ref, watch, type Ref, onBeforeUnmount } from 'vue';

/** 系统是否降级动效 */
function reduceMotion(): boolean {
  if (typeof document === 'undefined') return false;
  if (document.documentElement.classList.contains('reduce-motion')) return true;
  return window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
}

/** 减速收尾缓动：起步快、末尾缓，避免中途生硬骤停（对应「数字滚动」核心） */
function easeOutQuart(t: number): number {
  return 1 - Math.pow(1 - t, 4);
}

/**
 * 数字滚动：监听 source 变化，把显示值从当前缓动到目标。
 * 关键：缓慢减速停下，不在中途硬切；reduce-motion 时直接跳到目标。
 * @param source 目标值（响应式）
 * @param opts.duration 时长 ms（默认 720，给足“减速”余地）
 */
export function useNumberRoll(source: Ref<number>, opts?: { duration?: number }): Ref<number> {
  const display = ref(source.value);
  let raf = 0;
  let from = source.value;
  let start = 0;
  const duration = opts?.duration ?? 720;

  function tick(now: number) {
    const p = Math.min(1, (now - start) / duration);
    const eased = easeOutQuart(p);
    display.value = from + (source.value - from) * eased;
    if (p < 1) raf = requestAnimationFrame(tick);
    else display.value = source.value;
  }

  watch(source, () => {
    if (reduceMotion()) {
      display.value = source.value;
      return;
    }
    from = display.value;
    start = performance.now();
    cancelAnimationFrame(raf);
    raf = requestAnimationFrame(tick);
  });

  onBeforeUnmount(() => cancelAnimationFrame(raf));
  return display;
}
