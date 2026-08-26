import { ref } from 'vue';

/** 底部抽屉状态：开/关。关键帧停顿（落位感）由组件 CSS 负责，这里只管开关 */
export function useBottomDrawer(initial = false) {
  const open = ref(initial);
  const openDrawer = () => (open.value = true);
  const closeDrawer = () => (open.value = false);
  const toggleDrawer = () => (open.value = !open.value);
  return { open, openDrawer, closeDrawer, toggleDrawer };
}
