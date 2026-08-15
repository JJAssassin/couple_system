import { ref, computed, type Ref } from 'vue';

/** 分页拉取函数：给定页码/页大小，返回 { items, total } */
export type PagedFetcher<T> = (p: { page: number; pageSize: number }) => Promise<{ items: T[]; total: number }>;

/**
 * 通用分页 / 加载更多 组合式。
 * - pager 模式：翻页，每页替换列表。
 * - more 模式：累积追加，直到 hasMore=false。
 * 支持 refresh() 回到第一页重新拉取（用于实时同步后刷新）。
 */
export function usePagedList<T>(fetcher: PagedFetcher<T>, opts?: { pageSize?: number; mode?: 'pager' | 'more' }) {
  const pageSize = ref(opts?.pageSize ?? 12);
  const mode = ref<'pager' | 'more'>(opts?.mode ?? 'pager');

  const list = ref([]) as Ref<T[]>;
  const page = ref(1);
  const total = ref(0);
  const loading = ref(false);
  // 加载失败信息：fetcher reject 时写入，供视图展示「加载失败 / 重试」。
  // 注意全局拦截器已弹 toast 提示，这里仅为视图提供错误态（如重试入口）。
  const error = ref<string | null>(null);

  const pages = computed(() => Math.max(1, Math.ceil(total.value / Math.max(1, pageSize.value))));
  const hasMore = computed(() => mode.value === 'more' && list.value.length < total.value);

  async function load(pageNo = 1, append = false) {
    loading.value = true;
    error.value = null;
    try {
      const res = await fetcher({ page: pageNo, pageSize: pageSize.value });
      total.value = res.total ?? res.items.length;
      page.value = pageNo;
      list.value = append ? [...list.value, ...res.items] : res.items;
    } catch (e) {
      error.value = e instanceof Error ? e.message : '加载失败';
    } finally {
      loading.value = false;
    }
  }

  function loadFirst() {
    return load(1, false);
  }
  function nextPage() {
    if (mode.value === 'pager') return load(page.value + 1, false);
    if (hasMore.value) return load(page.value + 1, true);
  }
  function refresh() {
    return load(1, false);
  }

  return { list, page, pageSize, total, loading, error, pages, hasMore, load, loadFirst, nextPage, refresh };
}
