import { ref, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';

const EDGE_THRESHOLD = 30; // 左边缘触发区域 px
const SWIPE_THRESHOLD = 80; // 最小滑动距离 px
const MAX_VERTICAL = 60; // 允许的最大垂直偏移 px

export function useSwipeBack() {
  const router = useRouter();
  let startX = 0;
  let startY = 0;
  let swiping = false;

  function onTouchStart(e: TouchEvent) {
    if (e.touches.length !== 1) return;
    const x = e.touches[0].clientX;
    const y = e.touches[0].clientY;
    // 仅在左边缘触发
    if (x > EDGE_THRESHOLD) return;
    startX = x;
    startY = y;
    swiping = true;
  }

  function onTouchMove(e: TouchEvent) {
    if (!swiping) return;
    const dx = e.touches[0].clientX - startX;
    const dy = e.touches[0].clientY - startY;
    // 垂直偏移过大则放弃手势，避免干扰页面滚动
    if (Math.abs(dy) > MAX_VERTICAL && Math.abs(dx) < Math.abs(dy)) {
      swiping = false;
      return;
    }
    // 向右滑动才触发返回
    if (dx > SWIPE_THRESHOLD) {
      swiping = false;
      router.back();
    }
  }

  function onTouchEnd() {
    swiping = false;
  }

  onMounted(() => {
    document.addEventListener('touchstart', onTouchStart, { passive: true });
    document.addEventListener('touchmove', onTouchMove, { passive: true });
    document.addEventListener('touchend', onTouchEnd);
  });

  onUnmounted(() => {
    document.removeEventListener('touchstart', onTouchStart);
    document.removeEventListener('touchmove', onTouchMove);
    document.removeEventListener('touchend', onTouchEnd);
  });
}
