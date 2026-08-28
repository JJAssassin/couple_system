import type { Directive, App } from 'vue';
import { hapticForAction } from '@/composables/useHaptic';

/** 系统是否要求降级动效（用户设置或系统偏好） */
function reduceMotion(): boolean {
  if (typeof document === 'undefined') return false;
  if (document.documentElement.classList.contains('reduce-motion')) return true;
  return window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
}

/* ============ 3. 水波按钮：波纹从点击位置扩散，反馈跟随手指 ============ */
export const vRipple: Directive<HTMLElement> = {
  mounted(el) {
    el.classList.add('fx-ripple-host');
    const handler = (e: PointerEvent) => {
      if (reduceMotion()) return;
      const rect = el.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;
      // 以到最远角的距离作为直径，保证覆盖全元素
      const r = Math.hypot(Math.max(x, rect.width - x), Math.max(y, rect.height - y));
      const span = document.createElement('span');
      span.className = 'fx-ripple';
      span.style.left = `${x - r}px`;
      span.style.top = `${y - r}px`;
      span.style.width = span.style.height = `${r * 2}px`;
      span.addEventListener('animationend', () => span.remove(), { once: true });
      el.appendChild(span);
    };
    el.__rippleHandler = handler;
    el.addEventListener('pointerdown', handler);
  },
  unmounted(el) {
    el.removeEventListener('pointerdown', el.__rippleHandler!);
    delete el.__rippleHandler;
  },
};

/* ============ 1. 按压回弹：下压过基准再回弹，赋予重量手感 ============ */
export const vPressBounce: Directive<HTMLElement> = {
  mounted(el) {
    const down = () => {
      if (reduceMotion()) return;
      el.style.transition = 'transform var(--fx-dur-micro, 140ms) var(--fx-ease-soft, ease)';
      el.style.transform = 'scale(0.96)';
      el.style.willChange = 'transform';
      // 按压即触发轻触反馈：所有 v-press-bounce 按钮（全站主操作）一次性获得 Web Haptic 触感
      hapticForAction('tap');
    };
    const up = () => {
      if (reduceMotion()) {
        el.style.transform = '';
        return;
      }
      el.style.transform = '';
      el.classList.remove('fx-press-bounce');
      // 强制 reflow 让动画重新触发
      void el.offsetWidth;
      el.classList.add('fx-press-bounce');
    };
    const onEnd = () => el.classList.remove('fx-press-bounce');
    el.__pressDown = down;
    el.__pressUp = up;
    el.__pressEnd = onEnd;
    el.addEventListener('pointerdown', down);
    el.addEventListener('pointerup', up);
    el.addEventListener('pointercancel', up);
    el.addEventListener('pointerleave', up);
    el.addEventListener('animationend', onEnd);
  },
  unmounted(el) {
    el.removeEventListener('pointerdown', el.__pressDown!);
    el.removeEventListener('pointerup', el.__pressUp!);
    el.removeEventListener('pointercancel', el.__pressUp!);
    el.removeEventListener('pointerleave', el.__pressUp!);
    el.removeEventListener('animationend', el.__pressEnd!);
  },
};

/* ============ 4. 点击爆散：动效力度匹配操作，强化操作感知 ============ */
export const vClickBurst: Directive<HTMLElement, { count?: number } | undefined> = {
  mounted(el) {
    el.classList.add('fx-ripple-host');
    const handler = (e: PointerEvent) => {
      if (reduceMotion()) return;
      const rect = el.getBoundingClientRect();
      const cx = e.clientX - rect.left;
      const cy = e.clientY - rect.top;
      const count = el.__burstCount ?? 7;
      for (let i = 0; i < count; i++) {
        const angle = (Math.PI * 2 * i) / count + Math.random() * 0.5;
        const dist = 26 + Math.random() * 22;
        const p = document.createElement('span');
        p.className = 'fx-burst';
        p.style.left = `${cx - 3.5}px`;
        p.style.top = `${cy - 3.5}px`;
        p.style.background = 'var(--color-rose, #ff6f7d)';
        p.style.setProperty('--bx', `${Math.cos(angle) * dist}px`);
        p.style.setProperty('--by', `${Math.sin(angle) * dist}px`);
        p.addEventListener('animationend', () => p.remove(), { once: true });
        el.appendChild(p);
      }
    };
    el.__burstHandler = handler;
    el.addEventListener('pointerdown', handler);
  },
  updated(el, binding) {
    el.__burstCount = binding.value?.count ?? 7;
  },
  unmounted(el) {
    el.removeEventListener('pointerdown', el.__burstHandler!);
    delete el.__burstHandler;
  },
};

/* 给 HTMLElement 挂自定义属性，避免 TS 报错 */
declare global {
  interface HTMLElement {
    __rippleHandler?: (e: PointerEvent) => void;
    __pressDown?: () => void;
    __pressUp?: () => void;
    __pressEnd?: () => void;
    __burstHandler?: (e: PointerEvent) => void;
    __burstCount?: number;
  }
}

/** 在 main.ts 中调用：把三个微交指令注册为全局可用 */
export function registerFinesseDirectives(app: App) {
  app.directive('ripple', vRipple);
  app.directive('press-bounce', vPressBounce);
  app.directive('click-burst', vClickBurst);
}
