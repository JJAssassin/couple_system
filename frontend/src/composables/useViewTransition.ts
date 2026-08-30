/**
 * useViewTransition —— View Transitions API 共享元素过渡（P2-13）。
 *
 * 适用场景：同页内「列表 → 详情」等状态切换（如相册封面放大进入详情）。
 * 跨页（懒加载路由）时新页快照早于组件挂载，共享元素不可靠，不做。
 *
 * 设计约束：
 * - 仅在浏览器支持 + 非 reduce-motion + 提供 source 时启用，否则直接执行 swap（优雅降级）
 * - swap 需是「同步/快速」的状态切换（如 currentAlbum.value = a），异步数据加载放到 swap 之后
 * - 回调内等一帧 rAF 让 Vue flush 后再给目标元素挂同名，浏览器据此配对旧/新元素做 morph
 * - 过渡结束后清理两侧的 view-transition-name，避免泄漏影响后续页面
 */
export interface SharedTransitionOptions {
  /** 旧快照中的源元素（如列表封面 img）；为空则跳过过渡直接 swap */
  source: HTMLElement | null;
  /** view-transition-name（须唯一、且过渡期间不与页面内其他元素撞名） */
  name: string;
  /** 新状态渲染后返回目标元素（如详情封面），可返回 null 自动降级为普通淡入 */
  applyTarget: () => HTMLElement | null;
  /** 状态切换（同步或快速 async 均可） */
  swap: () => void | Promise<void>;
}

export async function startSharedTransition(opts: SharedTransitionOptions): Promise<void> {
  const reduced =
    document.documentElement.classList.contains('reduce-motion') ||
    (typeof window.matchMedia === 'function' && window.matchMedia('(prefers-reduced-motion: reduce)').matches);
  const canVT = typeof document !== 'undefined' && 'startViewTransition' in document;

  if (!canVT || !opts.source || reduced) {
    await opts.swap();
    return;
  }

  const src = opts.source;
  let target: HTMLElement | null = null;
  src.style.viewTransitionName = opts.name;

  const vt = (document as any).startViewTransition(async () => {
    await opts.swap();
    // 等一帧，确保 Vue flush 完新状态
    await new Promise<void>((r) => requestAnimationFrame(() => r()));
    target = opts.applyTarget();
    if (target) target.style.viewTransitionName = opts.name;
  });

  try {
    await vt.finished;
  } catch {
    /* 过渡被中断（如用户触发新导航） */
  } finally {
    src.style.removeProperty('view-transition-name');
    // 闭包内赋值导致 CFA 收窄异常，显式断言回联合类型再清理
    const t = target as HTMLElement | null;
    t?.style.removeProperty('view-transition-name');
  }
}
