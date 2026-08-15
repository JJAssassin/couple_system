import { ref, onMounted, onUnmounted } from 'vue';

const mobile = ref(false);

function evaluate() {
  mobile.value = window.matchMedia('(max-width: 767px)').matches;
}

export function isMobile() {
  return mobile.value;
}

export function useDevice() {
  onMounted(() => {
    evaluate();
    window.addEventListener('resize', evaluate);
  });
  onUnmounted(() => window.removeEventListener('resize', evaluate));
  return { mobile, evaluate };
}
