/* 情侣系统 Service Worker —— 手写（不依赖 Workbox），离线 app-shell + 运行时缓存。
 * 注册位置：/sw.js（scope=/）。生产构建由 main.ts 仅在 import.meta.env.PROD 注册。
 * 策略：
 *  - 导航请求 network-first，离线回退缓存的 app shell（/index.html）
 *  - /assets/*（带 hash 的构建产物）stale-while-revalidate
 *  - /uploads/*（用户图片）cache-first
 *  - /api/* GET（幂等读接口）network-first，断网回退缓存；按登录会话(cl_at Cookie)隔离 key，杜绝跨账号串数据；
 *    写操作（POST/PUT/DELETE）与 /hub/*（SignalR）不拦截，保留实时性与鉴权
 *  - 其余同源静态 stale-while-revalidate
 */
const PRECACHE = 'pw-precache-v1';
const ASSETS = 'pw-assets-v1';
const UPLOADS = 'pw-uploads-v1';
const API = 'pw-api-v1';
const KEEP = [PRECACHE, ASSETS, UPLOADS, API];

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
  // 实时通道不拦截
  if (url.pathname.startsWith('/hub/')) return;

  // API 幂等读接口：network-first + 离线回退（按用户隔离）
  if (url.pathname.startsWith('/api/')) {
    event.respondWith(apiNetworkFirst(req));
    return;
  }

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

/* ---------- API 离线缓存（方向④） ---------- */

/**
 * 用户指纹（djb2）用于隔离 API 缓存 key，杜绝同一浏览器切换账号时跨账号串数据。
 * 注意：本系统鉴权走 httponly Cookie（cl_at），前端/Service Worker 拿不到其明文，
 * 也不会在请求里带 Authorization 头——此前用 Authorization 算指纹会得到空串、
 * 导致所有账号共用同一缓存 key（隐私漏洞）。现改为从 Cookie 头提取 cl_at 值作为指纹源；
 * 未登录/提取失败则退化为 'anon'。SW fetch 事件的同域请求 headers 仍含 Cookie，可正常读取。
 */
function userKey(headers) {
  const cookie = headers.get('Cookie') || '';
  const m = /(?:^|;\s*)cl_at=([^;]+)/.exec(cookie);
  const at = m ? m[1] : '';
  let h = 5381;
  for (let i = 0; i < at.length; i++) h = ((h << 5) + h + at.charCodeAt(i)) >>> 0;
  return h.toString(36) || 'anon';
}

/** 缓存条目 URL：原 URL + 用户指纹参数（仅作缓存 key，不参与网络请求） */
function apiKeyUrl(req) {
  const sep = req.url.includes('?') ? '&' : '?';
  return req.url + sep + '__u=' + userKey(req.headers);
}

/** API 读接口：network-first；成功回写缓存（仅 200 + 业务 success），断网回退该用户的缓存副本 */
async function apiNetworkFirst(req) {
  const cache = await caches.open(API);
  const key = apiKeyUrl(req);
  const cached = await cache.match(key);
  try {
    const res = await fetch(req);
    if (res && res.status === 200) {
      // 业务层失败（success=false）不缓存，避免把错误快照当成离线数据
      const clone = res.clone();
      try {
        const body = await clone.json();
        if (body && body.success === true) cache.put(key, res.clone());
      } catch {
        /* 非 JSON（罕见）按可缓存处理 */
        cache.put(key, res.clone());
      }
    }
    return res;
  } catch {
    return cached || Response.error();
  }
}
