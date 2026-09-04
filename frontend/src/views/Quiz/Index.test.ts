// @vitest-environment jsdom
/**
 * Quiz 乐观更新视图接线测试。
 * 契约（见视图注释）：onStart/onAnswer 不做乐观（结果由服务端计算）；
 * 乐观化仅覆盖「本地可确定」的增删——放弃对局 / 删战绩 / 删题目（本地先移除，
 * 失败回滚可重试）/ 添加题目（占位行，成功后 listQuizQuestions 校正真实 id）。
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { mount, flushPromises } from '@vue/test-utils';
import { setActivePinia, createPinia } from 'pinia';
import Quiz from './Index.vue';

const hoisted = vi.hoisted(() => {
  const requestError = vi.fn();
  const success = vi.fn();
  const api = {
    listQuizQuestions: vi.fn(),
    createQuizQuestion: vi.fn(),
    deleteQuizQuestion: vi.fn(),
    listQuizRounds: vi.fn(),
    startQuizRound: vi.fn(),
    answerQuizRound: vi.fn(),
    deleteQuizRound: vi.fn(),
    getQuizStats: vi.fn(),
  };
  return { requestError, success, api };
});

vi.mock('@/api/quiz', () => hoisted.api);
vi.mock('@/store/notifyStore', () => ({
  useNotifyStore: () => ({ requestError: hoisted.requestError, success: hoisted.success }),
}));
vi.mock('@/composables/useRealtime', () => ({
  useRealtime: () => ({ onSync: vi.fn(), useModuleSync: vi.fn() }),
  overlaySyncMap: { toServer: {}, toClient: {} },
}));
vi.mock('@/composables/useAnimation', () => ({ useStaggerEnter: vi.fn() }));
vi.mock('@/composables/useConfetti', () => ({ fireHearts: vi.fn() }));
vi.mock('@/utils/feedback', () => ({
  feedback: { deleted: vi.fn(), created: vi.fn(), updated: vi.fn(), warn: vi.fn() },
}));

const ROUND = (id: number) => ({
  id, questionId: 100 + id, questionText: `题目${id}`, options: ['A', 'B'], category: null,
  firstUserId: 1, firstAnswer: 'A', secondUserId: null, secondAnswer: null,
  isRevealed: false, isMatched: false, createTime: '2026-09-01T06:00:00.000Z',
});
const Q = (id: number, text: string) => ({ id, text, options: ['A', 'B'], category: null, isBuiltin: false });
const STATS = () => ({ totalRounds: 2, revealedRounds: 1, matchedRounds: 1, matchRate: 100, pendingRounds: 1 });

describe('Quiz 乐观更新接线', () => {
  let w: ReturnType<typeof mount>;

  beforeEach(() => {
    setActivePinia(createPinia());
    hoisted.requestError.mockReset();
    hoisted.success.mockReset();
    for (const fn of Object.values(hoisted.api)) fn.mockReset();
    hoisted.api.listQuizRounds.mockImplementation(async () => ({ items: [ROUND(1), ROUND(2)], total: 2 }));
    hoisted.api.getQuizStats.mockImplementation(async () => STATS());
    hoisted.api.listQuizQuestions.mockImplementation(async () => [Q(10, '今天吃什么'), Q(11, '周末去哪')]);
    w = mount(Quiz, { attachTo: document.body });
  });
  afterEach(() => {
    w.unmount();
    document.body.innerHTML = '';
  });

  it('放弃对局：本地立即移除，成功后 load 校正', async () => {
    await flushPromises();
    hoisted.api.deleteQuizRound.mockResolvedValue(undefined);
    // 服务端此刻已没有 id=1：成功后的 load() 以服务端真值覆盖本地
    hoisted.api.listQuizRounds.mockImplementation(async () => ({ items: [ROUND(2)], total: 1 }));

    await (w.vm as any).onAbandon(1);
    await flushPromises();

    expect(hoisted.api.deleteQuizRound).toHaveBeenCalledWith(1);
    expect(hoisted.success).toHaveBeenCalledWith('已放弃这局');
    expect((w.vm as any).rounds.map((r: { id: number }) => r.id)).toEqual([2]);
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('放弃对局失败：回滚恢复该局并弹可重试错误卡', async () => {
    await flushPromises();
    hoisted.api.deleteQuizRound.mockRejectedValue(new Error('boom'));

    await (w.vm as any).onAbandon(1);
    await flushPromises();

    expect((w.vm as any).rounds.some((r: { id: number }) => r.id === 1)).toBe(true);
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('放弃这局');
  });

  it('删除题目成功：本地先移除，listQuizQuestions 校正真实列表', async () => {
    await flushPromises();
    hoisted.api.deleteQuizQuestion.mockResolvedValue(undefined);
    hoisted.api.listQuizQuestions.mockResolvedValue([Q(11, '周末去哪')]);

    await (w.vm as any).onDeleteQuestion(10);
    await flushPromises();

    expect(hoisted.api.deleteQuizQuestion).toHaveBeenCalledWith(10);
    expect((w.vm as any).questions.map((q: { id: number }) => q.id)).toEqual([11]);
    expect(hoisted.requestError).not.toHaveBeenCalled();
  });

  it('添加题目成功：占位负 id 行被服务端列表替换', async () => {
    await flushPromises();
    (w.vm as any).newText = '晚饭吃什么';
    (w.vm as any).newOptions = ['火锅', '烧烤'];
    (w.vm as any).newCategory = '';
    hoisted.api.createQuizQuestion.mockResolvedValue(undefined);
    hoisted.api.listQuizQuestions.mockResolvedValue([Q(12, '晚饭吃什么'), Q(10, '今天吃什么'), Q(11, '周末去哪')]);

    await (w.vm as any).onAddQuestion();
    await flushPromises();

    expect(hoisted.api.createQuizQuestion).toHaveBeenCalledWith({
      text: '晚饭吃什么', options: ['火锅', '烧烤'], category: undefined,
    });
    expect((w.vm as any).questions.every((q: { id: number }) => q.id > 0)).toBe(true);
    expect((w.vm as any).questions[0].text).toBe('晚饭吃什么'); // 服务端列表置顶
  });

  it('添加题目失败：占位行被回滚清除且表单保留', async () => {
    await flushPromises();
    (w.vm as any).newText = '晚饭吃什么';
    (w.vm as any).newOptions = ['火锅', '烧烤'];
    hoisted.api.createQuizQuestion.mockRejectedValue(new Error('boom'));

    await (w.vm as any).onAddQuestion();
    await flushPromises();

    expect((w.vm as any).questions.some((q: { id: number }) => q.id < 0)).toBe(false);
    expect((w.vm as any).newText).toBe('晚饭吃什么'); // 失败不清表单，便于重试
    expect(hoisted.requestError).toHaveBeenCalledTimes(1);
    expect(hoisted.requestError.mock.calls[0][0]).toContain('添加题目');
  });
});
