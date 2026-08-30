// @vitest-environment jsdom
/**
 * MilestonePoster 纪念海报测试：弹窗显隐、天数/标语渲染。
 * 组件内容经 <Teleport to="body"> 挂到 body，断言一律查 document.body。
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount } from '@vue/test-utils';
import MilestonePoster from './MilestonePoster.vue';

vi.mock('html2canvas', () => ({ default: vi.fn().mockResolvedValue({ toBlob: (cb: (b: Blob | null) => void) => cb(new Blob(['x'], { type: 'image/png' })) }) }));
// jsdom 无 n-message-provider，useMessage 会直接 throw
vi.mock('naive-ui', async (importOriginal) => {
  const actual = await importOriginal<typeof import('naive-ui')>();
  return { ...actual, useMessage: () => ({ success: vi.fn(), error: vi.fn() }) };
});

describe('MilestonePoster', () => {
  let w: ReturnType<typeof mount>;

  beforeEach(() => {
    w = mount(MilestonePoster, {
      props: {
        days: 100,
        label: '在一起 100 天 · 小小里程碑',
        name: '小兰',
        date: '2026.08.30',
      },
    });
  });

  afterEach(() => {
    w.unmount();
    document.body.innerHTML = '';
  });

  it('默认关闭，open() 后显示弹窗', async () => {
    expect(document.body.querySelector('.mp-mask')).toBeNull();
    (w.vm as any).open();
    await w.vm.$nextTick();
    expect(document.body.querySelector('.mp-mask')).not.toBeNull();
  });

  it('渲染天数、标语与落款', async () => {
    (w.vm as any).open();
    await w.vm.$nextTick();
    const text = (document.body.querySelector('.mp-dialog') as HTMLElement).textContent ?? '';
    expect(text).toContain('100');
    expect(text).toContain('在一起 100 天 · 小小里程碑');
    expect(text).toContain('小兰');
    expect(text).toContain('2026.08.30');
  });

  it('close() 关闭弹窗', async () => {
    (w.vm as any).open();
    await w.vm.$nextTick();
    (w.vm as any).close();
    await w.vm.$nextTick();
    expect(document.body.querySelector('.mp-mask')).toBeNull();
  });
});
