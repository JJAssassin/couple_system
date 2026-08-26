/* 微交互动效库统一出口：组件、指令、组合式。
 * 设计哲学（源自 mouse-lin/finesse-* 技能提炼）：
 *   不靠堆特效，给每一次操作恰到好处的反馈。只动 transform/opacity，
 *   尊重 reduce-motion，力度匹配操作，一次只让一个元素抢戏。 */
export { default as FinesseSwitch } from './components/FinesseSwitch.vue';
export { default as LiquidSlider } from './components/LiquidSlider.vue';
export { default as SuccessCheck } from './components/SuccessCheck.vue';
export { default as NumberRoll } from './components/NumberRoll.vue';
export { default as SkeletonSettle } from './components/SkeletonSettle.vue';
export { default as FlipCard } from './components/FlipCard.vue';
export { default as HamburgerIcon } from './components/HamburgerIcon.vue';
export { default as SwipeCard } from './components/SwipeCard.vue';
export { default as BottomDrawer } from './components/BottomDrawer.vue';

export { vRipple, vPressBounce, vClickBurst, registerFinesseDirectives } from './directives';
export { useNumberRoll } from './composables/useNumberRoll';
export { useCardFlip } from './composables/useCardFlip';
export { useBottomDrawer } from './composables/useBottomDrawer';
