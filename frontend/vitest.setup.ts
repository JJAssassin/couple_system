import { config } from '@vue/test-utils';

// 测试环境不经由 main.ts 注册全局微交指令；在此统一注册空指令，
// 消除组件测试中 "Failed to resolve directive: press-bounce" 之类的 Vue 警告。
// 指令的动画行为（ripple / press-bounce / click-burst）不涉及测试断言，用空实现即可。
config.global.directives = {
  ripple: { mounted() {}, unmounted() {} },
  'press-bounce': { mounted() {}, unmounted() {} },
  'click-burst': { mounted() {}, unmounted() {} },
};
