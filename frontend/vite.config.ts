import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import tailwindcss from '@tailwindcss/vite';
import Components from 'unplugin-vue-components/vite';
import { NaiveUiResolver } from 'unplugin-vue-components/resolvers';
import { fileURLToPath, URL } from 'node:url';
import { gzipSync, brotliCompressSync, constants as zlibConstants } from 'node:zlib';
// PWA：Service Worker 为手写 public/sw.js，由 main.ts 在 PROD 注册；此处不启用 vite-plugin-pwa，
// 避免与手写 SW 产生双 Service Worker 冲突。
import { promises as fs } from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';

// 读取 package.json 的 version，作为 APK/前端的统一版本号，注入到 import.meta.env.VITE_APP_VERSION，
// 并产出 dist/version.json 与 dist/app/version.json（应用内更新检查读取的清单，可由后端域名同址托管）。
const require = createRequire(import.meta.url);
const pkg = require('./package.json');

// 版本号（与 android/app/build.gradle 的 versionCode / versionName 保持一致，发版时同步 +1）。
const APP_VERSION_CODE = 2;
const APP_ANDROID_VERSION_CODE = 2;

// 构建期静态资源压缩（零依赖，使用 Node 内置 zlib）：为 dist 内每个资源产出 .gz 与 .br 副本，
// 由静态服务器/CDN 按需返回以显著降低传输体积。需服务器开启 precompressed 协商（如 nginx gzip_static / brotli_static）。
function compressStaticAssets() {
  const COMPRESS_EXT = /\.(js|css|html|svg|json|ico|png|woff2?)$/;
  const BROTLI_EXT = /\.(js|css|html|svg|json)$/;
  const walk = async (dir: string): Promise<string[]> => {
    const entries = await fs.readdir(dir, { withFileTypes: true });
    const nested = await Promise.all(
      entries.map((e) => {
        const p = path.join(dir, e.name);
        return e.isDirectory() ? walk(p) : [p];
      })
    );
    return nested.flat();
  };
  return {
    name: 'compress-static-assets',
    apply: 'build' as const,
    async closeBundle() {
      const outDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), 'dist');
      const all = await walk(outDir);
      const targets = all.filter((f) => COMPRESS_EXT.test(f));
      for (const f of targets) {
        const buf = await fs.readFile(f);
        await fs.writeFile(`${f}.gz`, gzipSync(buf, { level: 9 }));
        if (BROTLI_EXT.test(f)) {
          await fs.writeFile(
            `${f}.br`,
            brotliCompressSync(buf, {
              params: { [zlibConstants.BROTLI_PARAM_QUALITY]: 11, [zlibConstants.BROTLI_PARAM_MODE]: zlibConstants.BROTLI_MODE_TEXT },
            })
          );
        }
      }
    },
  };
}

// 构建期产出版本清单：dist/version.json 与 dist/app/version.json。
// 应用内更新检查（AppUpdatePrompt）会读取「服务器地址 /app/version.json」，
// 比对 versionCode；其中 apkUrl / releaseUrl 由部署方在托管处填写（指向新 APK 下载地址）。
function emitVersionJson() {
  return {
    name: 'emit-version-json',
    apply: 'build' as const,
    async closeBundle() {
      const outDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), 'dist');
      const manifest = {
        versionName: pkg.version,
        versionCode: APP_VERSION_CODE,
        androidVersionCode: APP_ANDROID_VERSION_CODE,
        changelog: '',
        apkUrl: '',
        releaseUrl: '',
        minSupportedCode: 1,
      };
      await fs.writeFile(path.join(outDir, 'version.json'), JSON.stringify(manifest, null, 2));
      await fs.mkdir(path.join(outDir, 'app'), { recursive: true });
      await fs.writeFile(path.join(outDir, 'app', 'version.json'), JSON.stringify(manifest, null, 2));
    },
  };
}

export default defineConfig({
  // 注入应用版本号（应用内更新检查用它比对远端清单的 versionCode）
  define: {
    'import.meta.env.VITE_APP_VERSION': JSON.stringify(pkg.version),
  },
  plugins: [
    vue(),
    tailwindcss(),
    Components({ resolvers: [NaiveUiResolver()], dts: false }),
    compressStaticAssets(),
    emitVersionJson(),
    // PWA Service Worker 由 public/sw.js（手写）提供，main.ts 在 PROD 注册；不使用 vite-plugin-pwa，避免双 SW 冲突。
  ],
  // dev 预构建目标与 build.target 对齐（es2022），避免开发/生产语法降级不一致。
  optimizeDeps: {
    esbuildOptions: { target: 'es2022' },
  },
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
    emptyOutDir: true,
    // 升级到 es2022：启用原生 class fields / 顶层 await 等，产物更小、运行更高效；
    // 覆盖所有现役浏览器（2022+），对个人情侣 PWA 无兼容风险。
    target: 'es2022',
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
