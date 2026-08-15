import { ref, shallowRef, onUnmounted } from 'vue';
import * as signalR from '@microsoft/signalr';
import { useAuthStore } from '@/store/authStore';
import { authenticateSync } from '@/api/sync';

// 模块级单例：全局仅一条 SignalR 连接
const partnerOnline = ref(false);
// 用 shallowRef 而非 ref：HubConnection 是带私有成员的类实例，ref 的 UnwrapRef 会剥离私有成员，
// 导致 .value 与 HubConnection 类型不兼容（TS2740）。shallowRef 不做深解包，保留完整类类型。
const connection = shallowRef<signalR.HubConnection | null>(null);
const starting = ref(false);

// 增量同步信号类型（与后端 SyncSignal 对齐）：kind ∈ created/updated/deleted/reload；id 为变更实体主键
export interface SyncChange { kind: 'created' | 'updated' | 'deleted' | 'reload'; id: number | null; }
export interface SyncSignal { module: string; changes: SyncChange[]; }

const listeners = new Map<string, Set<(sig: SyncSignal) => void>>();

// 握手：匿名连上 WebSocket 后，携带 JWT 上报 connectionId，后端据此登记并加入对应情侣组
async function authenticate(conn: signalR.HubConnection) {
  const id = conn.connectionId;
  if (id) await authenticateSync(id);
}

async function ensure(): Promise<signalR.HubConnection | null> {
  const token = useAuthStore().accessToken;
  if (!token) return null;
  if (connection.value) return connection.value;
  if (starting.value) return null;
  starting.value = true;
  try {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl('/hub/sync') // 不再在 URL 带 JWT；令牌仅通过 /api/sync/authenticate 的 Authorization 头上报
      .withAutomaticReconnect([0, 1000, 2000, 5000, 10000])
      .build();
    conn.on('Presence', (p: { online: boolean }) => {
      partnerOnline.value = !!p?.online;
    });
    conn.on('Sync', (sig: SyncSignal) => {
      if (!sig?.module) return;
      listeners.get(sig.module)?.forEach((cb) => {
        // 订阅回调可能触发刷新请求并 reject（如实时同步时后端/网络异常）。
        // 在分发处统一吞掉 rejection，避免未处理 Promise Rejection 刷屏（拦截器已弹 toast）。
        Promise.resolve(cb(sig)).catch(() => {});
      });
    });
    // 重连后 connectionId 会变，必须重新握手绑定情侣组
    conn.onreconnected(async () => {
      await authenticate(conn);
      conn.invoke('Ping').catch(() => {});
    });
    await conn.start();
    connection.value = conn;
    // 握手：上报 connectionId，后端登记并加入对应情侣组（情侣组隔离，杜绝跨情侣串台）
    await authenticate(conn);
    conn.invoke('Ping').catch(() => {}); // 主动探测一次在线状态
    return conn;
  } catch {
    connection.value = null;
    return null;
  } finally {
    starting.value = false;
  }
}

export function useRealtime() {
  const auth = useAuthStore();
  if (auth.accessToken && !connection.value && !starting.value) {
    ensure().catch(() => {});
  }

  // 订阅某模块的实时同步：回调收到结构化信号 { module, changes }。
  // 兼容策略：changes 仅含 reload 或无 id 时，调用方应做全量刷新；否则可按 change.kind / change.id 做局部更新。
  function onSync(module: string, cb: (sig: SyncSignal) => void) {
    if (!listeners.has(module)) listeners.set(module, new Set());
    listeners.get(module)!.add(cb);
    const off = () => listeners.get(module)?.delete(cb);
    onUnmounted(off);
    return off;
  }

  return { partnerOnline, ensure, onSync };
}
