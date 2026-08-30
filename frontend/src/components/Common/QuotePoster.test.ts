// @vitest-environment jsdom
/**
 * QuotePoster 金句海报测试：弹窗显隐、金句与元数据渲染。
 * 组件内容经 <Teleport to="body"> 挂到 body，断言一律查 document.body。
 * html2canvas 仅用于导出，测试中 mock 掉避免引入画布依赖。
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount } from '@vue/test-utils';
import QuotePoster from './QuotePoster.vue';

vi.mock('html2canvas', () => ({ default: vi.fn().mockResolvedValue({ toBlob: (cb: (b: Blob | null) => void) => cb(new Blob(['x'], { type: 'image/png' })) }) }));
// jsdom 无 n-message-provider，useMessage 会直接 throw
vi.mock('naive-ui', async (importOriginal) => {
  const actual = await importOriginal<typeof import('naive-ui')>();
  return { ...actual, useMessage: () => ({ success: vi.fn(), error: vi.fn() }) };
});

describe('QuotePoster', () => {
  let w: ReturnType<typeof mount>;

  beforeEach(() => {
    w = mount(QuotePoster, {
      props: {
        quote: '喜欢你，是我做过最不后悔的决定。',
        author: '阿兰',
        name: '小兰',
        days: 123,
        date: '2026.08.30',
      },
    });
  });

  afterEach(() => {
    w.unmount();
    document.body.innerHTML = '';
  });

  it('默认关闭，open() 后显示弹窗', async () => {
    expect(document.body.querySelector('.qp-mask')).toBeNull();
    (w.vm as any).open();
    await w.vm.$nextTick();
    expect(document.body.querySelector('.qp-mask')).not.toBeNull();
  });

  it('渲染金句、作者与落款', async () => {
    (w.vm as any).open();
    await w.vm.$nextTick();
    const text = (document.body.querySelector('.qp-dialog') as HTMLElement).textContent ?? '';
    expect(text).toContain('喜欢你，是我做过最不后悔的决定。');
    expect(text).toContain('—— 阿兰');
    expect(text).toContain('小兰');
    expect(text).toContain('123');
    expect(text).toContain('2026.08.30');
  });

  it('close() 关闭弹窗', async () => {
    (w.vm as any).open();
    await w.vm.$nextTick();
    (w.vm as any).close();
    await w.vm.$nextTick();
    expect(document.body.querySelector('.qp-mask')).toBeNull();
  });
});
