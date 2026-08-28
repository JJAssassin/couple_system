/**
 * 零依赖情感化微交互：碎纸屑（fireConfetti）/ 心形爆破（fireHearts）。
 * - 单例 canvas 覆盖层 + rAF 驱动，无外部依赖、可离线运行，契合项目便携环境约定。
 * - 尊重 reduce-motion（用户设置 / 系统偏好）时直接跳过，避免眩晕。
 * - 粒子耗尽后自动回收画布与 resize 监听，无内存泄漏。
 */

function reduceMotion(): boolean {
  if (typeof document === 'undefined') return true;
  if (document.documentElement.classList.contains('reduce-motion')) return true;
  return window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
}

const COLORS = ['#ff6f7d', '#D88593', '#F4A9B8', '#ff9f6e', '#ffc46b', '#7ec8a4', '#6ba7d6'];

type Shape = 'rect' | 'circle' | 'heart';
interface Particle {
  x: number; y: number; vx: number; vy: number;
  size: number; color: string; rot: number; vr: number;
  life: number; maxLife: number; shape: Shape; gravity: number;
}

let canvas: HTMLCanvasElement | null = null;
let ctx: CanvasRenderingContext2D | null = null;
let rafId: number | null = null;
let particles: Particle[] = [];

function resize() {
  if (!canvas || !ctx) return;
  const dpr = Math.min(window.devicePixelRatio || 1, 2);
  canvas.width = window.innerWidth * dpr;
  canvas.height = window.innerHeight * dpr;
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
}

function ensureCanvas() {
  if (canvas) return;
  canvas = document.createElement('canvas');
  canvas.style.cssText = 'position:fixed;inset:0;width:100%;height:100%;pointer-events:none;z-index:9999;';
  document.body.appendChild(canvas);
  ctx = canvas.getContext('2d');
  resize();
  window.addEventListener('resize', resize);
}

function drawHeart(c: CanvasRenderingContext2D, x: number, y: number, s: number, rot: number, color: string, alpha: number) {
  c.save();
  c.translate(x, y);
  c.rotate(rot);
  c.scale(s / 16, s / 16);
  c.globalAlpha = alpha;
  c.fillStyle = color;
  c.beginPath();
  c.moveTo(0, 4);
  c.bezierCurveTo(-8, -4, -8, -12, 0, -8);
  c.bezierCurveTo(8, -12, 8, -4, 0, 4);
  c.closePath();
  c.fill();
  c.restore();
}

function spawn(shape: 'confetti' | 'heart', count: number, origin?: { x: number; y: number }) {
  if (reduceMotion()) return;
  const W = window.innerWidth;
  const H = window.innerHeight;
  const ox = origin?.x ?? W / 2;
  const oy = origin?.y ?? (shape === 'heart' ? H * 0.45 : H * 0.32);
  for (let i = 0; i < count; i++) {
    const angle = -Math.PI / 2 + (Math.random() - 0.5) * (shape === 'heart' ? Math.PI * 0.9 : Math.PI * 1.2);
    const speed = (shape === 'heart' ? 4 : 6) + Math.random() * (shape === 'heart' ? 5 : 8);
    particles.push({
      x: ox + (Math.random() - 0.5) * 40,
      y: oy,
      vx: Math.cos(angle) * speed,
      vy: Math.sin(angle) * speed - (shape === 'heart' ? 2 : 0),
      size: shape === 'heart' ? 16 + Math.random() * 14 : 6 + Math.random() * 6,
      color: shape === 'heart' ? COLORS[(Math.random() * 3) | 0] : COLORS[(Math.random() * COLORS.length) | 0],
      rot: Math.random() * Math.PI,
      vr: (Math.random() - 0.5) * 0.3,
      life: 0,
      maxLife: (shape === 'heart' ? 80 : 90) + Math.random() * 40,
      shape: shape === 'heart' ? 'heart' : (Math.random() < 0.5 ? 'rect' : 'circle'),
      gravity: shape === 'heart' ? -0.02 : 0.18 + Math.random() * 0.1,
    });
  }
  ensureCanvas();
  start();
}

function start() {
  if (rafId != null || !ctx) return;
  const loop = () => {
    if (!ctx || !canvas) { rafId = null; return; }
    ctx.clearRect(0, 0, window.innerWidth, window.innerHeight);
    for (let i = particles.length - 1; i >= 0; i--) {
      const p = particles[i];
      p.life++;
      p.vy += p.gravity;
      p.vx *= 0.99;
      p.x += p.vx;
      p.y += p.vy;
      p.rot += p.vr;
      const t = p.life / p.maxLife;
      const alpha = t < 0.8 ? 1 : Math.max(0, 1 - (t - 0.8) / 0.2);
      if (p.shape === 'heart') {
        drawHeart(ctx, p.x, p.y, p.size, p.rot, p.color, alpha);
      } else {
        ctx.save();
        ctx.translate(p.x, p.y);
        ctx.rotate(p.rot);
        ctx.globalAlpha = alpha;
        ctx.fillStyle = p.color;
        if (p.shape === 'rect') ctx.fillRect(-p.size / 2, -p.size / 2, p.size, p.size * 0.6);
        else { ctx.beginPath(); ctx.arc(0, 0, p.size / 2, 0, Math.PI * 2); ctx.fill(); }
        ctx.restore();
      }
      if (p.life >= p.maxLife || p.y > window.innerHeight + 60) particles.splice(i, 1);
    }
    if (particles.length > 0) {
      rafId = requestAnimationFrame(loop);
    } else {
      ctx.clearRect(0, 0, window.innerWidth, window.innerHeight);
      rafId = null;
      if (canvas) { canvas.remove(); canvas = null; ctx = null; }
      window.removeEventListener('resize', resize);
    }
  };
  rafId = requestAnimationFrame(loop);
}

export function fireConfetti(opts?: { count?: number; x?: number; y?: number }) {
  spawn('confetti', opts?.count ?? 90, opts?.x != null && opts?.y != null ? { x: opts.x, y: opts.y } : undefined);
}

export function fireHearts(opts?: { count?: number; x?: number; y?: number }) {
  spawn('heart', opts?.count ?? 28, opts?.x != null && opts?.y != null ? { x: opts.x, y: opts.y } : undefined);
}
