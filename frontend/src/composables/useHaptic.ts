import { ref, computed } from 'vue';
import { useSettingStore } from '@/store/settingStore';

/** 触觉反馈强度 */
export type HapticStyle = 'light' | 'medium' | 'heavy' | 'success' | 'warning' | 'error';

const DURATION: Record<HapticStyle, number> = {
  light: 10,
  medium: 20,
  heavy: 30,
  success: 25,
  warning: 30,
  error: 40,
};

/** 是否支持触觉反馈（Vibration API） */
export function hapticSupported(): boolean {
  return typeof window !== 'undefined' && 'vibrate' in navigator;
}

/** 轻触反馈：按钮、导航点击 */
export function hapticLight() {
  if (!hapticSupported()) return;
  const setting = useSettingStore();
  if (setting.reduceMotion) return;
  navigator.vibrate(DURATION.light);
}

/** 中等反馈：确认操作、开关切换 */
export function hapticMedium() {
  if (!hapticSupported()) return;
  const setting = useSettingStore();
  if (setting.reduceMotion) return;
  navigator.vibrate(DURATION.medium);
}

/** 强反馈：删除、重要操作 */
export function hapticHeavy() {
  if (!hapticSupported()) return;
  const setting = useSettingStore();
  if (setting.reduceMotion) return;
  navigator.vibrate(DURATION.heavy);
}

/** 成功反馈：提交成功、完成任务 */
export function hapticSuccess() {
  if (!hapticSupported()) return;
  const setting = useSettingStore();
  if (setting.reduceMotion) return;
  navigator.vibrate([DURATION.light, 50, DURATION.light]);
}

/** 警告反馈：边界提示 */
export function hapticWarning() {
  if (!hapticSupported()) return;
  const setting = useSettingStore();
  if (setting.reduceMotion) return;
  navigator.vibrate([DURATION.medium, 60, DURATION.medium]);
}

/** 错误反馈：失败提示 */
export function hapticError() {
  if (!hapticSupported()) return;
  const setting = useSettingStore();
  if (setting.reduceMotion) return;
  navigator.vibrate([DURATION.heavy, 80, DURATION.heavy, 80, DURATION.heavy]);
}

/** 自定义振动模式 */
export function hapticPattern(pattern: number | number[]) {
  if (!hapticSupported()) return;
  const setting = useSettingStore();
  if (setting.reduceMotion) return;
  try {
    navigator.vibrate(pattern);
  } catch {
    /* 静默忽略 */
  }
}

/** 停止振动 */
export function hapticStop() {
  if (!hapticSupported()) return;
  try {
    navigator.vibrate(0);
  } catch {
    /* 静默忽略 */
  }
}

/** 组合：根据操作类型自动选择反馈强度 */
export function hapticForAction(type: 'tap' | 'toggle' | 'delete' | 'success' | 'warning' | 'error') {
  switch (type) {
    case 'tap':
      return hapticLight();
    case 'toggle':
      return hapticMedium();
    case 'delete':
      return hapticHeavy();
    case 'success':
      return hapticSuccess();
    case 'warning':
      return hapticWarning();
    case 'error':
      return hapticError();
  }
}
