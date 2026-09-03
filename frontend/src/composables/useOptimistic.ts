import { useNotifyStore } from '@/store/notifyStore';

/**
 * 乐观更新助手：先改本地状态（apply），再打后端（api）；
 * 失败自动以服务端真值回滚（load）并弹浪漫风错误卡，可一键重试。
 *
 * 用法（列表视图）：
 *   const { mutate } = useOptimistic(load);
 *   await mutate({ label: '删除愿望', apply: () => removeLocal(id), api: () => deleteWish(id) });
 *
 * 设计要点：
 * - 用 load() 作为失败回滚的「真相源」：直接重新拉取列表，保证与后端一致，免去逐字段快照。
 * - 成功路径不在此处刷新（由调用方在 ok 后 load() 拉取服务端权威数据，如真实 id / 认领人）。
 * - 失败文案提示已恢复，onRetry 重放原 api 并刷新；重试仍失败则再次提示，不无限递归。
 */
export function useOptimistic(load: () => Promise<void>) {
  const notify = useNotifyStore();

  async function mutate(opts: {
    label: string;
    apply: () => void;
    api: () => Promise<unknown>;
  }): Promise<boolean> {
    const { label, apply, api } = opts;
    apply(); // 乐观：立即改本地，UI 瞬时响应
    try {
      await api();
      return true;
    } catch {
      // 回滚到服务端真值，并提示可重试
      await load();
      notify.requestError(`${label}失败了，已恢复到之前的状态～`, async () => {
        try {
          await api();
          await load();
        } catch {
          notify.requestError(`${label}还是失败了，稍后再试试吧～`);
        }
      });
      return false;
    }
  }

  return { mutate };
}
