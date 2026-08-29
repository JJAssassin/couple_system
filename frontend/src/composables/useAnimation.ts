import { onMounted, nextTick, type Ref } from 'vue';
import { gsap } from 'gsap';
import { useSettingStore } from '@/store/settingStore';

/**
 * 列表交错入场（遵循设计 §1.2 stagger 80ms）。
 * 移动端降到 40ms；reduceMotion 时直接显示，不做动画。
 *
 * 健壮性增强：若挂载瞬间目标元素尚未渲染（骨架屏 v-else / 异步数据列表），
 * 不立即跳过，而是在 nextTick 后重试若干次（含 MutationObserver 监听子节点出现），
 * 直到元素出现或超时——保证「内容后到」的视图也能正确错峰入场。
 * 对已挂载即有元素的视图（Home / Diary / Wish 等）行为不变：立即动画且不进入重试。
 */
export function useStaggerEnter(
  container: Ref<HTMLElement | undefined>,
  selector: string,
  opts?: { stagger?: number; y?: number; maxWaitMs?: number }
) {
  const setting = useSettingStore();

  function animate() {
    if (!container.value) return false;
    const items = container.value.querySelectorAll(selector);
    if (items.length === 0) return false;
    if (setting.reduceMotion) {
      gsap.set(items, { opacity: 1, y: 0 });
      return true;
    }
    const stagger = window.matchMedia('(max-width: 767px)').matches
      ? Math.min(opts?.stagger ?? 0.08, 0.04)
      : (opts?.stagger ?? 0.08);
    gsap.fromTo(
      items,
      { opacity: 0, y: opts?.y ?? 16 },
      { opacity: 1, y: 0, duration: 0.4, ease: 'power2.out', stagger }
    );
    return true;
  }

  onMounted(() => {
    if (animate()) return; // 挂载即有元素：立即动画，不进入重试
    // 非浏览器环境（纯 node 测试）无 DOM / rAF，直接跳过动画，避免抛错
    if (typeof window === 'undefined' || typeof document === 'undefined') return;
    // 内容后到：监听子节点出现，最多等待 maxWaitMs（默认 1200ms）
    const maxWait = opts?.maxWaitMs ?? 1200;
    const start = performance.now();
    let raf = 0;
    let observer: MutationObserver | undefined;
    const cleanup = () => {
      observer?.disconnect();
      if (raf) cancelAnimationFrame(raf);
    };
    const tick = () => {
      if (animate()) { cleanup(); return; }
      if (performance.now() - start > maxWait) { cleanup(); return; }
      raf = requestAnimationFrame(tick);
    };
    if (typeof MutationObserver !== 'undefined' && container.value) {
      observer = new MutationObserver(() => { if (animate()) cleanup(); });
      observer.observe(container.value, { childList: true, subtree: true });
    }
    nextTick(tick);
  });
}
