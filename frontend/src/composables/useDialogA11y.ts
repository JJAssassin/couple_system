import { watch, onBeforeUnmount, computed, type Ref, type ComputedRef } from 'vue';

/**
 * useDialogA11y —— 自研模态/抽屉/灯箱的无障碍基座。
 *
 * 统一补齐 WCAG 2.1 AA 对「自定义对话框」的硬性要求：
 *  - role="dialog" + aria-modal="true"（由 dialogAttrs 注入模板）
 *  - 名称：优先 aria-labelledby（指向可见标题），否则 aria-label
 *  - 焦点陷阱：Tab / Shift+Tab 在对话框内循环，不会逃逸到背景
 *  - Esc 关闭
 *  - 打开时记住触发元素，关闭后把焦点还回去（2.4.3 Focus Order）
 *
 * 用法：
 *   const { dialogAttrs } = useDialogA11y({ isOpen, close, dialogRef, ariaLabel })
 *   <div ref="panel" v-bind="dialogAttrs" ...> ... </div>
 */

const FOCUSABLE_SELECTOR = [
  'a[href]',
  'area[href]',
  'button:not([disabled])',
  'input:not([disabled]):not([type="hidden"])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  'iframe',
  'object',
  'embed',
  '[contenteditable="true"]',
  '[tabindex]:not([tabindex="-1"])',
].join(',');

export interface DialogA11yOptions {
  /** 当前是否打开（传入 show/visible/open 等响应式布尔） */
  isOpen: Ref<boolean> | ComputedRef<boolean>;
  /** 关闭函数（Esc / 点击遮罩等触发） */
  close: () => void;
  /** 指向 role="dialog" 根元素的 ref */
  dialogRef: Ref<HTMLElement | null | undefined>;
  /** 标签元素 id（用于 aria-labelledby）；缺省时回退到 aria-label */
  labelId?: string;
  /** 缺省 labelId 时使用的无障碍名称（可为函数，实时求值） */
  ariaLabel?: string | (() => string);
  /** 初始聚焦元素选择器（相对 dialog 内部）；缺省聚焦首个可聚焦元素或 dialog 本身 */
  initialFocus?: string;
}

function resolveLabel(opts: DialogA11yOptions): string {
  if (!opts.ariaLabel) return '';
  return typeof opts.ariaLabel === 'function' ? opts.ariaLabel() : opts.ariaLabel;
}

export function useDialogA11y(opts: DialogA11yOptions) {
  let previouslyFocused: HTMLElement | null = null;
  let active = false;

  function getFocusable(): HTMLElement[] {
    const root = opts.dialogRef.value;
    if (!root) return [];
    return Array.from(root.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR)).filter(
      (el) => el.offsetParent !== null || el === document.activeElement,
    );
  }

  function focusInitial() {
    const root = opts.dialogRef.value;
    if (!root) return;
    let target: HTMLElement | null = null;
    if (opts.initialFocus) {
      const f = root.querySelector<HTMLElement>(opts.initialFocus);
      if (f && f.offsetParent !== null) target = f;
    }
    if (!target) {
      const f = getFocusable();
      target = f[0] ?? root;
    }
    if (target === root && !root.hasAttribute('tabindex')) {
      root.setAttribute('tabindex', '-1');
    }
    (target as HTMLElement).focus({ preventScroll: true });
  }

  function onKeydown(e: KeyboardEvent) {
    if (!active) return;
    if (e.key === 'Escape') {
      e.stopPropagation();
      opts.close();
      return;
    }
    if (e.key !== 'Tab') return;
    const focusable = getFocusable();
    const root = opts.dialogRef.value;
    if (focusable.length === 0) {
      e.preventDefault();
      if (root && !root.hasAttribute('tabindex')) root.setAttribute('tabindex', '-1');
      root?.focus({ preventScroll: true });
      return;
    }
    const first = focusable[0];
    const last = focusable[focusable.length - 1];
    const cur = document.activeElement as HTMLElement | null;
    const inRoot = !!root && !!cur && root.contains(cur);
    if (e.shiftKey) {
      if (cur === first || !inRoot) {
        e.preventDefault();
        last.focus();
      }
    } else {
      if (cur === last || !inRoot) {
        e.preventDefault();
        first.focus();
      }
    }
  }

  function activate() {
    if (active) return;
    active = true;
    previouslyFocused = (document.activeElement as HTMLElement) ?? null;
    document.addEventListener('keydown', onKeydown, true);
    // 等 DOM 真正渲染后再聚焦（Transition + v-if 下 ref 可能在下一帧才就绪）
    requestAnimationFrame(() => requestAnimationFrame(focusInitial));
  }

  function deactivate() {
    if (!active) return;
    active = false;
    document.removeEventListener('keydown', onKeydown, true);
    if (previouslyFocused && document.contains(previouslyFocused)) {
      previouslyFocused.focus({ preventScroll: true });
    }
    previouslyFocused = null;
  }

  watch(
    opts.isOpen,
    (open) => {
      if (open) activate();
      else deactivate();
    },
    { immediate: true },
  );

  onBeforeUnmount(deactivate);

  const dialogAttrs = computed(() => {
    const a: Record<string, string> = { role: 'dialog', 'aria-modal': 'true' };
    if (opts.labelId) a['aria-labelledby'] = opts.labelId;
    else {
      const label = resolveLabel(opts);
      if (label) a['aria-label'] = label;
    }
    return a;
  });

  return { dialogAttrs };
}
