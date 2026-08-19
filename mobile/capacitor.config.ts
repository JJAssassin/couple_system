import type { CapacitorConfig } from '@capacitor/cli';

/**
 * 情侣小世界 · 原生 App 壳（Capacitor）
 * 在线模式：App = 原生 WebView 壳，内容加载公网 https://7182629.xyz
 * （始终最新、无需随版本打包静态资源；登录态存 WebView 的 localStorage/cookie，与浏览器互通）
 */
const config: CapacitorConfig = {
  appId: 'com.couplelove.app',
  appName: '我们的小世界',
  webDir: 'www',
  server: {
    url: 'https://7182629.xyz',
    cleartext: false,
    androidScheme: 'https',
  },
  android: {
    allowMixedContent: false,
  },
};

export default config;
