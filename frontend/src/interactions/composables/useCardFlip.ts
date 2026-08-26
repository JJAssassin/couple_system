import { ref } from 'vue';

/** 卡片翻面：正反面提前藏好，翻转不穿帮（组件内用 3D 翻转实现，这里只管状态） */
export function useCardFlip(initial = false) {
  const flipped = ref(initial);
  const toggle = () => (flipped.value = !flipped.value);
  const flip = (v: boolean) => (flipped.value = v);
  return { flipped, toggle, flip };
}
