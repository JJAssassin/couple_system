import { defineStore } from 'pinia';
import { ref } from 'vue';

const LS_DARK = 'cl_dark';
const LS_MOTION = 'cl_reduce_motion';

export const useSettingStore = defineStore('setting', () => {
  const dark = ref(localStorage.getItem(LS_DARK) === '1');
  const reduceMotion = ref(localStorage.getItem(LS_MOTION) === '1');

  function apply() {
    document.documentElement.classList.toggle('dark', dark.value);
    document.documentElement.classList.toggle('reduce-motion', reduceMotion.value);
  }
  // 启动即应用已保存偏好（main 中调用）
  function hydrate() {
    apply();
  }
  function toggleDark() {
    dark.value = !dark.value;
    localStorage.setItem(LS_DARK, dark.value ? '1' : '0');
    apply();
  }
  function toggleMotion() {
    reduceMotion.value = !reduceMotion.value;
    localStorage.setItem(LS_MOTION, reduceMotion.value ? '1' : '0');
    apply();
  }

  return { dark, reduceMotion, hydrate, toggleDark, toggleMotion };
});
