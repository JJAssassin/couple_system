// 服务器地址（API 根）可配置模块。
//
// 设计目标：让同一套前端既能「同源运行（开发 / 反代同域 / PWA）」，
// 也能「打包进 Capacitor APK 后指向任意后端（自有域名 / 局域网 IP）」。
//
// 取值优先级：运行时用户设置(localStorage) > 构建期默认(VITE_SERVER_BASE) > 空(同源相对路径)。
// - 为空：axios/SignalR/资源走相对路径（开发代理 / 反向代理同域），行为不变。
// - 非空（如 https://love.example.com 或 http://192.168.1.50:5199）：
//     API 走 <base>/api，SignalR 走 <base>/hub/sync，用户上传资源 /uploads/* 拼 <base>。
//
// 变更通过 window 事件 'couple-server-base-changed' 广播，request/useRealtime 监听后热更新。

import { Capacitor } from '@capacitor/core';

const LS_KEY = 'couple_server_base';
const EVT = 'couple-server-base-changed';

function lsBase(): string {
  try {
    return localStorage.getItem(LS_KEY) || '';
  } catch {
    return '';
  }
}

// 运行时覆盖（用户设置页填写），进程内缓存，避免反复读 localStorage。
let runtimeBase = lsBase();

/** 是否运行在 Capacitor 原生壳（APK / iOS）内 */
export function isNative(): boolean {
  try {
    return Capacitor.isNativePlatform();
  } catch {
    return false;
  }
}

/** 归一化后的服务器根（去掉末尾斜杠）。为空表示同源。 */
export function getServerBase(): string {
  const b = (runtimeBase || (import.meta.env.VITE_SERVER_BASE as string | undefined) || '').trim();
  return b.replace(/\/+$/, '');
}

/** 设置服务器地址（运行时），持久化并广播变更。 */
export function setServerBase(v: string) {
  runtimeBase = (v || '').trim().replace(/\/+$/, '');
  try {
    if (runtimeBase) localStorage.setItem(LS_KEY, runtimeBase);
    else localStorage.removeItem(LS_KEY);
  } catch {
    /* 隐私模式忽略 */
  }
  window.dispatchEvent(new Event(EVT));
}

/** API 基础地址：<base>/api，空时回退相对 /api。 */
export function getApiBase(): string {
  const b = getServerBase();
  return b ? `${b}/api` : '/api';
}

/** SignalR 握手地址：<base>/hub/sync，空时回退相对 /hub/sync。 */
export function getHubUrl(): string {
  const b = getServerBase();
  return b ? `${b}/hub/sync` : '/hub/sync';
}

/**
 * 应用内更新清单地址：与服务器同源的 /app/version.json。
 * 空（同源）时走相对路径（PWA / 远程 WebView 壳）；
 * 配置了服务器地址（打包 APK）时指向该服务器的清单，使更新检查能触及后端域名。
 */
export function getUpdateManifestUrl(): string {
  const b = getServerBase();
  return b ? `${b}/app/version.json` : '/app/version.json';
}

/**
 * 资源 URL 解析：
 * - 绝对地址(http/https)、data:/blob: 原样返回；
 * - 后端用户内容 /uploads/* 拼服务器根（APK 跨域必需）；
 * - 本地 public 资源(如 /ip/emoji_x.png、/favicon) 原样返回（由 WebView 同源 bundle 提供）。
 */
export function assetUrl(path?: string | null): string {
  if (!path) return '';
  if (/^https?:\/\//i.test(path) || path.startsWith('data:') || path.startsWith('blob:')) return path;
  if (path.startsWith('/uploads')) {
    const b = getServerBase();
    return b ? `${b}${path}` : path;
  }
  return path;
}

/** 订阅服务器地址变更（热更新 axios/SignalR）。 */
export function onServerBaseChanged(cb: () => void) {
  window.addEventListener(EVT, cb);
}

/** 构建期注入的应用版本（见 vite.config.ts define）。 */
export const APP_VERSION = (import.meta.env.VITE_APP_VERSION as string) || '0.1.0';
