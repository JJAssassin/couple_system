import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'com.couple.world',
  appName: '我们的小世界',
  webDir: 'dist',
  // 用 https scheme 启动 WebView，保证页面处于安全上下文，
  // 与公网域名(https)形态一致，便于后续原生能力/安全 API 接入。
  server: {
    androidScheme: 'https'
  },
  android: {
    // 允许在 file:// 资源混合加载时仍可使用 cleartext(非 https) 调试后端，
    // 生产改用自有域名 https 后无影响。
    allowMixedContent: true,
    // 隐藏 WebView 顶部默认的加载进度条，体验更接近原生
    backgroundColor: '#fff5f2'
  }
};

export default config;
