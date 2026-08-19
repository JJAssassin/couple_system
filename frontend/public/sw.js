/* 情侣系统 Service Worker —— 手写（不依赖 Workbox），离线 app-shell + 运行时缓存。
 * 注册位置：/sw.js（scope=/）。生产构建由 main.ts 仅在 import.meta.env.PROD 注册。
 * 策略：
 *  - 导航请求 network-first，离线回退缓存的 app shell（/index.html）
 *  - /assets/*（带 hash 的构建产物）stale-while-revalidate
 *  - /uploads/*（用户图片）cache-first
 *  - /api/* 与 /hub/* 不拦截（保留鉴权头与 SignalR 实时性）
 *  - 其余同源静态 stale-while-revalidate
 */
const PRECACHE = 'pw-precache-v1';
const ASSETS = 'pw-assets-v1';
const UPLOADS = 'pw-uploads-v1';
const KEEP = [PRECACHE, ASSETS, UPLOADS];

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(PRECACHE).then((c) => c.addAll(['/', '/index.html']).catch(() => {})),
  );
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches
      .keys()
      .then((keys) =>
        Promise.all(
          keys.filter((k) => k.startsWith('pw-') && !KEEP.includes(k)).map((k) => caches.delete(k)),
        ),
      )
      .then(() => self.clients.claim()),
  );
});

self.addEventListener('fetch', (event) => {
  const req = event.request;
  if (req.method !== 'GET') return;
  const url = new URL(req.url);
  // 不拦截 API 与实时通道
  if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/hub/')) return;

  // 导航：network-first，离线回退 app shell
  if (req.mode === 'navigate') {
    event.respondWith(
      fetch(req)
        .then((res) => {
          const copy = res.clone();
          caches.open(PRECACHE).then((c) => c.put('/index.html', copy));
          return res;
        })
        .catch(() => caches.match('/index.html').then((r) => r || caches.match('/'))),
    );
    return;
  }

  // 构建产物（带 hash，immutable 语义）
  if (url.pathname.startsWith('/assets/')) {
    event.respondWith(staleWhileRevalidate(req, ASSETS));
    return;
  }

  // 用户上传图片
  if (url.pathname.startsWith('/uploads/')) {
    event.respondWith(cacheFirst(req, UPLOADS));
    return;
  }

  // 其余同源静态（图标 / manifest / 字体等）
  event.respondWith(staleWhileRevalidate(req, ASSETS));
});

async function cacheFirst(req, cacheName) {
  const cache = await caches.open(cacheName);
  const cached = await cache.match(req);
  if (cached) return cached;
  try {
    const res = await fetch(req);
    if (res && res.status === 200) cache.put(req, res.clone());
    return res;
  } catch {
    return cached || Response.error();
  }
}

async function staleWhileRevalidate(req, cacheName) {
  const cache = await caches.open(cacheName);
  const cached = await cache.match(req);
  const network = fetch(req)
    .then((res) => {
      if (res && res.status === 200 && res.type === 'basic') cache.put(req, res.clone());
      return res;
    })
    .catch(() => cached);
  return cached || network;
}
