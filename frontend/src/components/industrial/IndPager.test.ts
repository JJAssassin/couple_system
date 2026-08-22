// @vitest-environment jsdom
/**
 * IndPager 组件测试：验证分页 / 加载更多两种模式。
 */
import { describe, it, expect, vi } from 'vitest';
import { mount } from '@vue/test-utils';
import IndPager from './IndPager.vue';

describe('IndPager', () => {
  const findBtn = (w: ReturnType<typeof mount<IndPager>>, text: string) =>
    w.findAll('button').find((b) => b.text().includes(text));

  it('pager 模式：渲染页码信息与上下页按钮', () => {
    const w = mount(IndPager, {
      props: { page: 2, pageSize: 10, total: 35 },
    });
    expect(w.text()).toContain('第 2 / 4 页');
    expect(w.text()).toContain('共 35 条');
    expect(findBtn(w, '‹ 上一页').exists()).toBe(true);
    expect(findBtn(w, '下一页 ›').exists()).toBe(true);
    w.unmount();
  });

  it('pager 模式：首页时上一页禁用', () => {
    const w = mount(IndPager, {
      props: { page: 1, pageSize: 10, total: 35 },
    });
    expect(findBtn(w, '‹ 上一页').attributes('disabled')).toBeDefined();
    w.unmount();
  });

  it('pager 模式：末页时下一页禁用', () => {
    const w = mount(IndPager, {
      props: { page: 4, pageSize: 10, total: 35 },
    });
    expect(findBtn(w, '下一页 ›').attributes('disabled')).toBeDefined();
    w.unmount();
  });

  it('pager 模式：点击下一页触发 update:page', async () => {
    const w = mount(IndPager, {
      props: { page: 1, pageSize: 10, total: 35 },
    });
    await findBtn(w, '下一页 ›').trigger('click');
    expect(w.emitted('update:page')).toEqual([[2]]);
    w.unmount();
  });

  it('more 模式：渲染加载更多按钮', () => {
    const w = mount(IndPager, {
      props: { mode: 'more', page: 1, pageSize: 10, total: 50, hasMore: true },
    });
    expect(w.text()).toContain('加载更多');
    expect(w.find('button').attributes('disabled')).toBeUndefined();
    w.unmount();
  });

  it('more 模式：加载中显示 spinner', () => {
    const w = mount(IndPager, {
      props: { mode: 'more', page: 1, pageSize: 10, total: 50, loading: true, hasMore: true },
    });
    expect(w.text()).toContain('加载中…');
    expect(w.find('button').attributes('disabled')).toBeDefined();
    w.unmount();
  });

  it('more 模式：没有更多时显示到底提示', () => {
    const w = mount(IndPager, {
      props: { mode: 'more', page: 5, pageSize: 10, total: 50, hasMore: false },
    });
    expect(w.text()).toContain('已经到底啦');
    w.unmount();
  });

  it('more 模式：点击触发 load-more', async () => {
    const w = mount(IndPager, {
      props: { mode: 'more', page: 1, pageSize: 10, total: 50, hasMore: true },
    });
    await w.find('button').trigger('click');
    expect(w.emitted('load-more')).toHaveLength(1);
    w.unmount();
  });
});
