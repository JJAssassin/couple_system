import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import tailwindcss from '@tailwindcss/vite';
import Components from 'unplugin-vue-components/vite';
import { NaiveUiResolver } from 'unplugin-vue-components/resolvers';
import { fileURLToPath, URL } from 'node:url';

export default defineConfig({
  plugins: [
    vue(),
    tailwindcss(),
    // NaiveUI 按需引入：编译期自动注入模板中用到的 n-* 组件（含 App.vue 的 Provider），
    // 配合 main.ts 去掉全量 app.use(NaiveUi)，把 naive chunk 从整库 ~1.3MB 砍到仅用到的组件。
    Components({ resolvers: [NaiveUiResolver()] }),
  ],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) }
  },
  server: {
    host: true,
    port: 5174,
    proxy: {
      // 开发期把 /api 代理到后端（避免 CORS、统一 baseURL）
      '/api': {
        target: 'http://localhost:5199',
        changeOrigin: true
      },
      // 上传的图片走 /uploads，同样代理到后端静态文件
      '/uploads': {
        target: 'http://localhost:5199',
        changeOrigin: true
      },
      // SignalR 实时同步（WebSocket / 长轮询都走这里）
      '/hub': {
        target: 'http://localhost:5199',
        changeOrigin: true,
        ws: true
      }
    }
  },
  build: {
    target: 'es2020',
    sourcemap: false,
    cssCodeSplit: true,
    chunkSizeWarningLimit: 900,
    rollupOptions: {
      output: {
        // 拆分体积较大的 vendor，提升首屏与移动端加载速度、增强长效缓存命中（设计 §12 性能约束）
        // echarts 已改为按需引入（见 ChartWrap.vue），本 chunk 体积已大幅下降；naive 为按需加载隔离 chunk
        manualChunks: {
          echarts: ['echarts'],
          naive: ['naive-ui'],
          vue: ['vue', 'vue-router', 'pinia'],
          editor: ['@wangeditor/editor', '@wangeditor/editor-for-vue'],
          signalr: ['@microsoft/signalr'],
          gsap: ['gsap'],
          icons: ['@iconify/vue', 'lucide-vue-next'],
        }
      }
    }
  }
});
