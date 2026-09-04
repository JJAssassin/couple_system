// @vitest-environment jsdom
/**
 * Album 乐观更新视图接线测试。
 * 契约：图片级删除/批量删除/排序走 useOptimistic(reloadImages)，
 * 失败以 listImages 拉服务端真值回滚并弹可重试错误卡；成功不额外刷新。
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import Album from './Index.vue';

const hoisted = vi.hoisted(() => {
  const requestError = vi.fn();
  const success = vi.fn();
  const api = {
    listAlbum: vi.fn(),
    listImages: vi.fn(),
    createAlbum: vi.fn(),
    deleteAlbum: vi.fn(),
    uploadImage: vi.fn(),
    deleteImage: vi.fn(),
    reorderImages: vi.fn(),
    batchDeleteImages: vi.fn(),
    batchMoveImages: vi.fn(),
    batchUploadImages: vi.fn(),
  };
  return { requestError, success, api };
});

vi.mock('@/api/album', () => hoisted.api);
vi.mock('@/store/notifyStore', () => ({
  useNotifyStore: () => ({ requestError: hoisted.requestError, success: hoisted.success }),
}));
vi.mock('@/composables/useRealtime', () => ({
  useRealtime: () => ({ useModuleSync: vi.fn(), onSync: vi.fn() }),
  overlaySyncMap: { toServer: {}, toClient: {} },
}));
vi.mock('@/composables/useAnimation', () => ({ useStaggerEnter: vi.fn() }));
vi.mock('@/composables/useSyncSettle', () => ({ useSyncSettle: vi.fn() }));
// 共享元素过渡：mock 需执行 swap 才能让 openAlbum 真正切到详情态
vi.mock('@/composables/useViewTransition', () => ({
  startSharedTransition: vi.fn(async (opts: { swap?: () => void }) => { opts?.swap?.(); }),
}));
vi.mock('@/composables/useDevice', () => ({ isMobile: () => false }));
vi.mock('@/utils/feedback', () => ({
  feedback: new Proxy({}, { get: () => vi.fn() }),
}));

const A = (id: number, name: string, extra: Record<string, unknown> = {}) => ({
  id, albumName: name, cover: '', remark: '', imageCount: 0,
  createTime: '2026-09-01T06:00:00.000Z', ...extra,
});
const IMG = (id: number, extra: Record<string, unknown> = {}) => ({
  id, url: `/u/${id}.jpg`, imagePath: `/u/${id}.jpg`, remark: null, shootTime: null,
  createTime: '2026-09-01T06:00:00.000Z', ...extra,
});

describe('Album 乐观更新接线', () => {
  let w: ReturnType<typeof mount>;

  beforeEach(() => {
    setActivePinia(createPinia());
    hoisted.requestError.mockReset();
    hoisted.success.mockReset();
    for (const fn of Object.values(hoisted.api)) fn.mockReset();
    // 每次调用返回新克隆：乐观 apply 会原位改写，共享引用会污染回滚真值
    hoisted.api.listAlbum.mockImplementation(async () => ({
      data: { data: { items: [A(1, '旅行'), A(2, '日常')], total: 2 } },
    }));
    hoisted.api.listImages.mockImplementation(async () => ({
      data: { data: [IMG(1), IMG(2), IMG(3)] },
    }));
    w = mount(Album, { attachTo: document.body });
  });
  afterEach(() => {
    w.unmount();
    document.body.innerHTML = '';
  });

  it('挂载加载相册列表，搜索关键字前端过滤', async () => {
    await flushPromises();
    expect((w.vm as any).albums).toHaveLength(2);
    (w.vm as any).albumKeyword = '旅行';
    await flushPromises();
    expect((w.vm as any).filteredAlbums.map((a: { id: number }) => a.id)).toEqual([1]);
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('删除照片成功：本地过滤 + 计数递减，不额外刷新', async () => {
    await flushPromises();
    await (w.vm as any).openAlbum(A(1, '旅行', { imageCount: 3 }));
    expect(hoisted.api.listImages).toHaveBeenCalledWith(1);

    await (w.vm as any).removeImage(IMG(2));
    await flushPromises();

    expect(hoisted.api.deleteImage).toHaveBeenCalledWith(2);
    expect((w.vm as any).images.map((i: { id: number }) => i.id)).toEqual([1, 3]);
    expect((w.vm as any).currentAlbum.imageCount).toBe(2);
    expect(hoisted.api.listImages).toHaveBeenCalledTimes(1); // 成功路径不 reload
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('删除照片失败：reloadImages 拉服务端真值回滚并弹可重试错误卡', async () => {
    await flushPromises();
    await (w.vm as any).openAlbum(A(1, '旅行', { imageCount: 3 }));
    hoisted.api.deleteImage.mockRejectedValue(new Error('boom'));

    await (w.vm as any).removeImage(IMG(2));
    await flushPromises();

    expect(hoisted.api.listImages).toHaveBeenCalledTimes(2); // openAlbum + 回滚
    expect((w.vm as any).images.map((i: { id: number }) => i.id)).toEqual([1, 2, 3]);
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('删除照片');
  });

  it('批量删除成功：ids 原样回写后端并自动退出选择模式', async () => {
    await flushPromises();
    await (w.vm as any).openAlbum(A(1, '旅行', { imageCount: 3 }));
    hoisted.api.batchDeleteImages.mockResolvedValue({});
    (w.vm as any).selectedIds = new Set([1, 3]);
    (w.vm as any).selectMode = true;

    await (w.vm as any).batchDelete();
    await flushPromises();

    expect(hoisted.api.batchDeleteImages).toHaveBeenCalledWith([1, 3]);
    expect((w.vm as any).images.map((i: { id: number }) => i.id)).toEqual([2]);
    expect((w.vm as any).selectMode).toBe(false);
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('拖拽排序成功：新顺序原样回写后端且不回弹', async () => {
    await flushPromises();
    await (w.vm as any).openAlbum(A(1, '旅行', { imageCount: 3 }));
    hoisted.api.reorderImages.mockResolvedValue({});
    (w.vm as any).images = [IMG(3), IMG(1), IMG(2)];

    await (w.vm as any).onReorder();
    await flushPromises();

    expect(hoisted.api.reorderImages).toHaveBeenCalledWith([3, 1, 2]);
    expect((w.vm as any).images.map((i: { id: number }) => i.id)).toEqual([3, 1, 2]); // 成功不回弹
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });
});
