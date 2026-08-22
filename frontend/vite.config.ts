import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import tailwindcss from '@tailwindcss/vite';
import Components from 'unplugin-vue-components/vite';
import { NaiveUiResolver } from 'unplugin-vue-components/resolvers';
import { fileURLToPath, URL } from 'node:url';
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig({
  plugins: [
    vue(),
    tailwindcss(),
    // NaiveUI 按需引入：编译期自动注入模板中用到的 n-* 组件（含 App.vue 的 Provider），
    // 配合 main.ts 去掉全量 app.use(NaiveUi)，把 naive chunk 从整库 ~1.3MB 砍到仅用到的组件。
    Components({ resolvers: [NaiveUiResolver()], dts: false }),
    VitePWA({
      registerType: 'autoUpdate',
      disable: true,
      manifest: {
        name: '我们的小世界',
        short_name: '小世界',
        description: '情侣专属情感陪伴 Web 系统',
        lang: 'zh-CN',
        dir: 'ltr',
        start_url: '/',
        scope: '/',
        display: 'standalone',
        display_override: ['standalone', 'minimal-ui'],
        orientation: 'portrait',
        background_color: '#fff5f6',
        theme_color: '#ff6f7d',
        categories: ['lifestyle', 'social'],
        icons: [
          { src: '/pwa-192x192.png', sizes: '192x192', type: 'image/png', purpose: 'any' },
          { src: '/pwa-512x512.png', sizes: '512x512', type: 'image/png', purpose: 'any' },
          { src: '/pwa-maskable-512x512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
        shortcuts: [
          { name: '首页', url: '/' },
          { name: '记账', url: '/account' },
          { name: '纪念日', url: '/anniversary' },
        ],
      },
      workbox: {
        globPatterns: ['**/*.{js,css,html,ico,png,svg,woff2}'],
        runtimeCaching: [
          {
            urlPattern: /^\/api\//,
            handler: 'NetworkFirst',
            options: {
              cacheName: 'pw-api-v1',
              networkTimeoutSeconds: 3,
              cacheKeyWillBeUsed: async ({ request }) => {
                const url = new URL(request.url);
                const sep = url.search.includes('?') ? '&' : '?';
                const auth = request.headers.get('Authorization') || '';
                let h = 5381;
                for (let i = 0; i < auth.length; i++) h = ((h << 5) + h + auth.charCodeAt(i)) >>> 0;
                return `${request.url}${sep}__u=${h.toString(36)}`;
              },
              cacheWillUpdate: async ({ response }) => {
                try {
                  const clone = response.clone();
                  const body = await clone.json();
                  if (body && body.success === true) return response;
                } catch {}
                return false;
              },
            },
          },
          {
            urlPattern: ({ request }) => request.mode === 'navigate',
            handler: 'NetworkFirst',
            options: {
              cacheName: 'pw-precache-v1',
              networkTimeoutSeconds: 3,
              fallbackToCache: true,
            },
          },
          {
            urlPattern: /^\/assets\//,
            handler: 'StaleWhileRevalidate',
            options: { cacheName: 'pw-assets-v1' },
          },
          {
            urlPattern: /^\/uploads\//,
            handler: 'CacheFirst',
            options: {
              cacheName: 'pw-uploads-v1',
              expiration: { maxEntries: 100 },
            },
          },
          {
            urlPattern: /^\/hub\//,
            handler: 'NetworkOnly',
          },
          {
            urlPattern: /^https?:\/\//,
            handler: 'StaleWhileRevalidate',
            options: {
              cacheName: 'pw-static-v1',
              expiration: { maxEntries: 50 },
            },
          },
        ],
        navigateFallback: '/index.html',
      },
      devOptions: { enabled: false },
    }),
  ],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) }
  },
  server: {
    host: true,
    port: 5174,
    proxy: {
      '/api': {
        target: 'http://localhost:5199',
        changeOrigin: true
      },
      '/uploads': {
        target: 'http://localhost:5199',
        changeOrigin: true
      },
      '/hub': {
        target: 'http://localhost:5199',
        changeOrigin: true,
        ws: true
      }
    }
  },
  build: {
    emptyOutDir: false,
    target: 'es2020',
    sourcemap: false,
    cssCodeSplit: true,
    chunkSizeWarningLimit: 900,
    rollupOptions: {
      output: {
        manualChunks: {
          echarts: ['echarts'],
          naive: ['naive-ui'],
          vue: ['vue', 'vue-router', 'pinia'],
          editor: ['@wangeditor/editor', '@wangeditor/editor-for-vue'],
          signalr: ['@microsoft/signalr'],
          gsap: ['gsap'],
          icons: ['@iconify/vue', 'lucide-vue-next'],
        },
      },
    },
  },
});
