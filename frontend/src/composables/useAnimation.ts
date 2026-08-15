import { onMounted, type Ref } from 'vue';
import { gsap } from 'gsap';
import { useSettingStore } from '@/store/settingStore';

/**
 * 列表交错入场（遵循设计 §1.2 stagger 80ms）。
 * 移动端降到 40ms；reduceMotion 时直接显示，不做动画。
 */
export function useStaggerEnter(
  container: Ref<HTMLElement | undefined>,
  selector: string,
  opts?: { stagger?: number; y?: number }
) {
  const setting = useSettingStore();
  onMounted(() => {
    if (!container.value) return;
    const items = container.value.querySelectorAll(selector);
    if (items.length === 0) return; // 列表项尚未渲染（异步数据），跳过动画，避免 GSAP 空目标告警
    if (setting.reduceMotion) {
      gsap.set(items, { opacity: 1, y: 0 });
      return;
    }
    const stagger = window.matchMedia('(max-width: 767px)').matches
      ? Math.min(opts?.stagger ?? 0.08, 0.04)
      : (opts?.stagger ?? 0.08);
    gsap.fromTo(
      items,
      { opacity: 0, y: opts?.y ?? 16 },
      { opacity: 1, y: 0, duration: 0.4, ease: 'power2.out', stagger }
    );
  });
}
