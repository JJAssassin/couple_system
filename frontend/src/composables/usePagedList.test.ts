import { describe, it, expect, vi } from 'vitest';
import { usePagedList, type PagedFetcher } from './usePagedList';

function fetcherFor(items: any[], total?: number): PagedFetcher<any> & { calls: any[] } {
  const fn = vi.fn(async (p: { page: number; pageSize: number }) => ({
    items: items.slice((p.page - 1) * p.pageSize, p.page * p.pageSize),
    total: total ?? items.length,
  })) as any;
  fn.calls = [];
  return fn;
}

const make = (n: number) => Array.from({ length: n }, (_, i) => ({ id: i }));

describe('usePagedList', () => {
  it('pager 模式：load 替换列表并写入 total/page/pageSize', async () => {
    const all = make(25);
    const fetcher = fetcherFor(all, all.length);
    const { list, total, page, pageSize, load } = usePagedList(fetcher, { pageSize: 10 });

    await load(1);

    expect(total.value).toBe(25);
    expect(page.value).toBe(1);
    expect(pageSize.value).toBe(10);
    expect(list.value.length).toBe(10);
    expect(fetcher).toHaveBeenCalledWith({ page: 1, pageSize: 10 });
  });

  it('pager 模式：nextPage 是替换而非追加', async () => {
    const all = make(25);
    const fetcher = fetcherFor(all, all.length);
    const p = usePagedList(fetcher, { pageSize: 10 });

    await p.loadFirst();
    await p.nextPage();

    expect(p.page.value).toBe(2);
    expect(p.list.value.length).toBe(10); // 替换：仍是单页大小，而非 20
    // 第二页内容应来自 all[10..19]
    expect(p.list.value[0].id).toBe(10);
  });

  it('more 模式：nextPage 累积追加直到 hasMore=false', async () => {
    const all = make(25);
    const fetcher = fetcherFor(all, all.length);
    const p = usePagedList(fetcher, { pageSize: 10, mode: 'more' });

    await p.loadFirst();
    expect(p.list.value.length).toBe(10);
    expect(p.hasMore.value).toBe(true);

    await p.nextPage();
    expect(p.list.value.length).toBe(20);
    expect(p.hasMore.value).toBe(true);

    await p.nextPage();
    expect(p.list.value.length).toBe(25);
    expect(p.hasMore.value).toBe(false); // 已填满 total
  });

  it('more 模式：数据不足一页时 hasMore 立即为 false', async () => {
    const all = make(5);
    const fetcher = fetcherFor(all, 5);
    const p = usePagedList(fetcher, { pageSize: 10, mode: 'more' });

    await p.loadFirst();
    expect(p.list.value.length).toBe(5);
    expect(p.hasMore.value).toBe(false);
  });

  it('refresh 回到第一页重新拉取', async () => {
    const all = make(25);
    const fetcher = fetcherFor(all, all.length);
    const p = usePagedList(fetcher, { pageSize: 10 });

    await p.load(2);
    expect(p.page.value).toBe(2);
    await p.refresh();
    expect(p.page.value).toBe(1);
    expect(p.list.value[0].id).toBe(0);
  });

  it('total 缺失时回退到 items.length', async () => {
    const all = make(7);
    const fetcher = vi.fn(async () => ({ items: all.slice(0, 7), total: 7 })) as PagedFetcher<any>;
    const p = usePagedList(fetcher, { pageSize: 10 });
    await p.loadFirst();
    expect(p.total.value).toBe(7);
  });

  it('fetcher 失败时：error 写入、loading 归 false、list 不被污染', async () => {
    const all = make(10);
    const ok = usePagedList(fetcherFor(all, all.length), { pageSize: 10 });
    await ok.loadFirst();
    expect(ok.list.value.length).toBe(10);

    const fail = vi.fn(async () => {
      throw new Error('network down');
    }) as unknown as PagedFetcher<any>;
    const p = usePagedList(fail, { pageSize: 10 });

    expect(p.list.value.length).toBe(0);
    expect(p.error.value).toBeNull();
    expect(p.loading.value).toBe(false);

    await p.loadFirst();

    expect(p.loading.value).toBe(false);
    expect(p.error.value).toBe('network down');
    expect(p.list.value.length).toBe(0); // 失败不清空已有数据、也不写入脏数据
  });
});
