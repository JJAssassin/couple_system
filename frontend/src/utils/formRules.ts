import type { FormItemRule } from 'naive-ui';

/**
 * 表单校验规则工厂。统一 trigger（输入 + 失焦 + 变更），
 * 配合 NForm 的 inline feedback 给出即时、友好的提示。
 */
export const requiredRule = (msg = '这一项填一下吧～'): FormItemRule => ({
  required: true,
  message: msg,
  trigger: ['input', 'blur', 'change'],
});

export const selectRule = (msg = '请选择一个选项'): FormItemRule => ({
  required: true,
  type: 'number',
  message: msg,
  trigger: ['change', 'blur'],
});

export const maxLenRule = (max: number, msg?: string): FormItemRule => ({
  max,
  message: msg ?? `最多 ${max} 个字哦`,
  trigger: ['input', 'blur'],
});

export const urlRule = (msg = '请填写合法的网址（以 http 开头）'): FormItemRule => ({
  trigger: ['input', 'blur'],
  validator(_rule, value: string) {
    if (!value) return true; // 允许为空（可选字段）
    if (!/^https?:\/\/.+/i.test(value)) return new Error(msg);
    return true;
  },
});

export const dateRule = (msg = '请选择一个日期'): FormItemRule => ({
  required: true,
  type: 'number',
  message: msg,
  trigger: ['change', 'blur'],
});
