import { describe, it, expect } from 'vitest';
import { requiredRule, selectRule, maxLenRule, urlRule, dateRule } from './formRules';

// naive-ui FormItemRule 的 validator 形如 (rule, value) => boolean | Error
type Validator = (rule: unknown, value: unknown) => boolean | Error;

describe('formRules', () => {
  it('requiredRule 默认文案与 trigger 集合', () => {
    const r = requiredRule();
    expect(r.required).toBe(true);
    expect(r.trigger).toEqual(['input', 'blur', 'change']);
    expect(r.message).toBe('这一项填一下吧～');
  });

  it('requiredRule 支持自定义文案', () => {
    expect(requiredRule('必填哦').message).toBe('必填哦');
  });

  it('selectRule 强制 number 类型（下拉用数值）', () => {
    const r = selectRule('请选择');
    expect(r.required).toBe(true);
    expect(r.type).toBe('number');
    expect(r.message).toBe('请选择');
  });

  it('dateRule 强制 number 类型（时间戳）', () => {
    expect(dateRule().type).toBe('number');
    expect(dateRule('选日期').message).toBe('选日期');
  });

  it('maxLenRule 文案包含最大值', () => {
    const r = maxLenRule(20);
    expect(r.max).toBe(20);
    expect(r.message).toContain('20');
  });

  it('urlRule 允许为空（可选字段）', () => {
    const v = urlRule().validator as Validator;
    expect(v(null, '')).toBe(true);
    expect(v(null, undefined)).toBe(true);
  });

  it('urlRule 非法网址返回 Error', () => {
    const v = urlRule().validator as Validator;
    const res = v(null, 'not-a-url');
    expect(res).toBeInstanceOf(Error);
  });

  it('urlRule 合法 http/https 通过', () => {
    const v = urlRule().validator as Validator;
    expect(v(null, 'https://example.com/x')).toBe(true);
    expect(v(null, 'http://a.b')).toBe(true);
  });
});
