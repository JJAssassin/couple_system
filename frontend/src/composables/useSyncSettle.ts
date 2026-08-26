import { onUnmounted, watch, type Ref } from 'vue';
import { useRealtime, type SyncSignal } from '@/composables/useRealtime';
import { useAuthStore } from '@/store/authStore';

function prefersReduced(): boolean {
  if (typeof document === 'undefined') return false;
  if (document.documentElement.classList.contains('reduce-motion')) return true;
  return (
    typeof window !== 'undefined' &&
    typeof window.matchMedia === 'function' &&
    window.matchMedia('(prefers-reduced-motion: reduce)').matches
  );
}

/**
 * 实时同步 × 微交互融合：
 * 当伴侣在另一台设备「新增 / 刷新」了本模块（created / reload），
 * 本端对应卡片以 fx-settle-in 错落入场，而非静默出现。
 *
 * 设计要点：
 * - 仅对 senderId 为「伴侣」的变更触发；自己的行为已有本地乐观更新反馈，避免重复播动画。
 * - 收到信号只"武装"标记，真正播放等底层列表 ref 变更（watch flush:'post'，DOM 已更新）后，
 *   避免与 load() 的异步请求竞速导致动画打到旧节点上。
 * - 尊重 html.reduce-motion / prefers-reduced-motion。
 * - 只动 opacity + transform（fx-settle-in 关键帧），不引发重排。
 *
 * @param module   模块名（与后端 SyncSignal.module 对齐，如 'wish' / 'todo' / 'diary'）
 * @param container 列表容器 ref（用于 querySelectorAll 定位卡片）
 * @param items     底层列表 ref（load() 重新赋值即视为变更）
 * @param selector  卡片选择器
 */
export function useSyncSettle<T>(
  module: string,
  container: Ref<HTMLElement | null | undefined>,
  items: Ref<T[]>,
  selector = '.love-card',
) {
  const auth = useAuthStore();
  const { onSync } = useRealtime();
  let armed = false;

  const off = onSync(module, (sig: SyncSignal) => {
    const myId = auth.profile?.id;
    const isPartner = sig.senderId != null && myId != null && sig.senderId !== myId;
    if (!isPartner) return;
    const changes = sig.changes ?? [];
    // 仅有"新内容出现"语义的变更才落位；纯 updated/deleted 不整列重播
    if (!changes.some((c) => c.kind === 'created' || c.kind === 'reload')) return;
    armed = true;
  });

  const stop = watch(
    items,
    () => {
      if (!armed) return;
      armed = false;
      if (prefersReduced()) return;
      requestAnimationFrame(() => {
        const el = container.value;
        if (!el) return;
        const nodes = Array.from(el.querySelectorAll<HTMLElement>(selector));
        nodes.forEach((n, i) => {
          n.style.animation = 'none';
          void n.offsetWidth; // 强制 reflow，确保动画可重复触发
          n.style.animation = 'fx-settle-in var(--fx-dur-settle, 420ms) var(--fx-ease-out) both';
          n.style.animationDelay = `${Math.min(i, 12) * 45}ms`;
        });
      });
    },
    { flush: 'post' },
  );

  onUnmounted(() => {
    off();
    stop();
  });
}
