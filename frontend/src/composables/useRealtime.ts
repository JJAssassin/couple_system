import { ref, shallowRef, onUnmounted, type Ref } from 'vue';
import * as signalR from '@microsoft/signalr';
import { useAuthStore } from '@/store/authStore';
import { authenticateSync } from '@/api/sync';
import { getHubUrl, onServerBaseChanged } from '@/config/server';

// 模块级单例：全局仅一条 SignalR 连接
const partnerOnline = ref(false);
// 用 shallowRef 而非 ref：HubConnection 是带私有成员的类实例，ref 的 UnwrapRef 会剥离私有成员，
// 导致 .value 与 HubConnection 类型不兼容（TS2740）。shallowRef 不做深解包，保留完整类类型。
const connection = shallowRef<signalR.HubConnection | null>(null);
const starting = ref(false);
// 连接状态：驱动顶部实时连接提示条（断线 / 重连中）。
// idle=未登录未尝试；connecting=握手/重连中（瞬时）；connected=已连；
// reconnecting=自动重连尝试中；disconnected=自动重连耗尽，需手动重连。
const connState = ref<'idle' | 'connecting' | 'connected' | 'reconnecting' | 'disconnected'>('idle');

// 增量同步信号类型（与后端 SyncSignal 对齐）：kind ∈ created/updated/deleted/reload；id 为变更实体主键
// payload：后端携带的实体标量投影（camelCase），供前端就地 upsert；reload / deleted 时为 undefined
// senderId：触发本次变更的操作用户 Id（后端写入）；前端据此区分"自己/伴侣"的改动，仅对伴侣的变更做提示
export interface SyncChange { kind: 'created' | 'updated' | 'deleted' | 'reload'; id: number | null; payload?: any; }
export interface SyncSignal { module: string; changes: SyncChange[]; senderId?: number | null; }

// [约定] 聚合视图（Stats / Timeline）实时刷新的唯一内容源模块集合。
// 后端 15 个 [Broadcast] 中进入"聚合流"的实体；Stats 与 Timeline 必须订阅同一集合，避免漂移。
// 规则：新增聚合内容源时，必须同步把对应广播模块名追加到本数组，否则聚合视图不会随伴侣刷新。
// 单实体视图不要进这个数组，应直接订阅自身模块。
export const AGGREGATE_SYNC_MODULES = [
  'diary', 'album', 'wish', 'quiz', 'board', 'footprint', 'todo', 'conflict', 'budget', 'anniversary', 'date',
] as const;

const listeners = new Map<string, Set<(sig: SyncSignal) => void>>();
// 全局监听：收到任意模块信号时都会触发（无论是否订阅了该模块），用于"伴侣更新"等跨模块提示
const anyListeners = new Set<(sig: SyncSignal) => void>();

// 服务器地址变更后，旧连接指向旧 host，必须断开让下次 ensure() 用新 hub 地址重建。
onServerBaseChanged(() => {
  if (connection.value) {
    connection.value.stop().catch(() => {});
    connection.value = null;
  }
});

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
      .withUrl(getHubUrl()) // 不再在 URL 带 JWT；令牌仅通过 /api/sync/authenticate 的 Authorization 头上报
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
      anyListeners.forEach((cb) => {
        Promise.resolve(cb(sig)).catch(() => {});
      });
    });
    // 自动重连进行中：重试耗尽前持续触发，状态置 reconnecting（用户可见"重连中…"）
    conn.onreconnecting(() => {
      connState.value = 'reconnecting';
    });
    // 重连成功后 connectionId 会变，必须重新握手绑定情侣组
    conn.onreconnected(async () => {
      connState.value = 'connected';
      await authenticate(conn);
      conn.invoke('Ping').catch(() => {});
    });
    // 自动重连耗尽（withAutomaticReconnect 次数用尽）后触发：状态置 disconnected，等待用户手动重连
    conn.onclose(() => {
      connState.value = 'disconnected';
    });
    connState.value = 'connecting';
    await conn.start();
    connection.value = conn;
    // 握手：上报 connectionId，后端登记并加入对应情侣组（情侣组隔离，杜绝跨情侣串台）
    await authenticate(conn);
    conn.invoke('Ping').catch(() => {}); // 主动探测一次在线状态
    connState.value = 'connected';
    return conn;
  } catch {
    connection.value = null;
    connState.value = 'disconnected';
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

  /**
   * 重新握手：用「当前最新」accessToken 重新上报 connectionId，使连接迁到正确的情侣组。
   * 绑定 / 解绑后令牌被后端重签（cid 已变），若不重握手，SignalR 连接仍留在旧的 anon 组（或旧情侣组），
   * 实时推送会落到旧组、对方刚绑定却收不到你的实时更新。调用前请确保 accessToken 已是新 cid 的令牌。
   */
  async function rehandshake() {
    const conn = connection.value;
    if (!conn) return;
    const token = useAuthStore().accessToken;
    if (!token) return;
    try {
      await authenticate(conn);
    } catch {
      /* 握手失败不影响主流程，下次连接/重连会自愈 */
    }
  }

  /**
   * 手动重连：自动重连耗尽（onclose → Disconnected）后，用户点提示条按钮主动重建连接。
   * 先停掉旧连接并清空单例，下次 ensure() 走新建分支用最新 JWT + hub 地址重建。
   */
  async function reconnect(): Promise<void> {
    const token = useAuthStore().accessToken;
    if (!token) {
      connState.value = 'disconnected';
      return;
    }
    const old = connection.value;
    if (old) {
      try {
        await old.stop();
      } catch {
        /* 旧连接可能已不可用，忽略 */
      }
    }
    connection.value = null;
    starting.value = false;
    connState.value = 'connecting';
    await ensure();
    // ensure 内部已据结果更新 connState；兜底：若仍无连接则置 disconnected
    if (!connection.value) connState.value = 'disconnected';
  }

  // 订阅所有模块的实时信号（无论是否显式订阅某模块）。用于"伴侣更新"等跨模块轻提示。
  function onAnySync(cb: (sig: SyncSignal) => void) {
    anyListeners.add(cb);
    const off = () => anyListeners.delete(cb);
    onUnmounted(off);
    return off;
  }

  return { partnerOnline, connState, ensure, onSync, onAnySync, rehandshake, useModuleSync, reconnect };
}

// 增量同步助手：在 onSync 基础上，当后端信号携带实体 Payload 时做就地 upsert/remove，避免整表重载。
// 安全策略：reload 信号、未提供 map、或 created（默认）一律回退整表 load()；
// 仅在 updated/deleted 且提供 map 时做局部更新，杜绝因载荷形状不一致导致的显示异常。
export function overlaySyncMap<T>(payload: any, existing: T | undefined): T {
  return { ...(existing ?? {}), ...(payload ?? {}) } as T;
}

export function useModuleSync<T>(
  module: string,
  opts: {
    items: Ref<T[]>;
    getId: (item: T) => number | string | undefined;
    load: () => void | Promise<void>;
    map?: (payload: any, existing: T | undefined) => T;
    allowCreate?: boolean;
  }
): () => void {
  const { onSync } = useRealtime();
  return onSync(module, (sig: SyncSignal) => {
    const changes = sig.changes ?? [];
    // reload 信号（或无法增量）直接整表重载，保持原行为
    if (changes.some((c) => c.kind === 'reload') || !opts.map) {
      opts.load();
      return;
    }
    const map = new Map<number | string, T>();
    for (const it of opts.items.value) {
      const id = opts.getId(it);
      if (id != null) map.set(id, it);
    }
    let mutated = false;
    for (const c of changes) {
      const id = c.id;
      if (id == null) {
        opts.load();
        return;
      }
      if (c.kind === 'deleted') {
        if (map.delete(id)) mutated = true;
      } else if (c.kind === 'updated') {
        map.set(id, opts.map!(c.payload, map.get(id)));
        mutated = true;
      } else if (c.kind === 'created') {
        if (opts.allowCreate) {
          map.set(id, opts.map!(c.payload, undefined));
          mutated = true;
        } else {
          opts.load();
          return;
        }
      } else {
        opts.load();
        return;
      }
    }
    if (mutated) opts.items.value = [...map.values()];
  });
}
