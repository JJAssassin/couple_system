<template>
  <div class="show">
    <header class="show-head">
      <h1>微交互动效 · 恰到好处的反馈</h1>
      <p class="sub">
        12 项高级交互细节 · 取自 mouse-lin/finesse 技能哲学：<b>不靠堆特效</b>，让每一次操作有刚好的回应。
        所有动效尊重「设置 → 减弱动效」与系统 prefers-reduced-motion。
      </p>
    </header>

    <section class="grid">
      <!-- 1 按压回弹 -->
      <div class="cell">
        <div class="tag">01 · 按压回弹</div>
        <p class="desc">按键下压过基准再回弹，赋予重量手感。</p>
        <button class="demo-btn" v-press-bounce @click="noop">按住我</button>
      </div>

      <!-- 2 弹性开关 -->
      <div class="cell">
        <div class="tag">02 · 弹性开关</div>
        <p class="desc">滑块起步先横向拉长，落位再恢复宽度，有质量感。</p>
        <FinesseSwitch v-model="sw" />
        <span class="state">{{ sw ? '开' : '关' }}</span>
      </div>

      <!-- 3 水波按钮 -->
      <div class="cell">
        <div class="tag">03 · 水波按钮</div>
        <p class="desc">波纹从点击位置扩散，反馈跟随手指。</p>
        <button class="demo-btn rose" v-ripple @click="noop">点我泛波</button>
      </div>

      <!-- 4 点击爆散 -->
      <div class="cell">
        <div class="tag">04 · 点击爆散</div>
        <p class="desc">动效力度匹配操作，强化操作感知。</p>
        <button class="demo-btn" v-click-burst @click="noop">点我迸发</button>
      </div>

      <!-- 5 成功勾选 -->
      <div class="cell">
        <div class="tag">05 · 成功勾选</div>
        <p class="desc">对勾逐笔绘制，体现完成的过程感。</p>
        <button class="demo-btn ghost" @click="done = !done">
          <SuccessCheck :active="done" :size="26" /> {{ done ? '已完成' : '点我完成' }}
        </button>
      </div>

      <!-- 6 液态滑块 -->
      <div class="cell">
        <div class="tag">06 · 液态滑块</div>
        <p class="desc">轨道、滑钮、数值三组动画同频同步。</p>
        <LiquidSlider v-model="lv" :min="0" :max="100" :step="1" suffix="%" />
      </div>

      <!-- 7 数字滚动 -->
      <div class="cell">
        <div class="tag">07 · 数字滚动</div>
        <p class="desc">缓慢减速停下，避免中途生硬骤停。</p>
        <div class="big"><NumberRoll :value="num" :duration="760" /></div>
        <button class="demo-btn ghost sm" @click="num = Math.floor(Math.random() * 9000) + 1000">换个数字</button>
      </div>

      <!-- 8 骨架落位 -->
      <div class="cell">
        <div class="tag">08 · 骨架落位</div>
        <p class="desc">骨架与内容同尺寸，切换不跳动。</p>
        <button class="demo-btn ghost sm" @click="loading = !loading">{{ loading ? '显示内容' : '显示骨架' }}</button>
        <div class="sk-demo">
          <SkeletonSettle :loading="loading" :lines="3">
            <div class="real">
              <b>我们的第 128 天</b>
              <span>今天一起看了日落，你笑得很好看。</span>
            </div>
          </SkeletonSettle>
        </div>
      </div>

      <!-- 9 卡片翻面 -->
      <div class="cell">
        <div class="tag">09 · 卡片翻面</div>
        <p class="desc">正反面提前藏好，翻转不穿帮。</p>
        <FlipCard v-model="flipped" interactive style="height: 120px">
          <template #front>
            <div class="face front">正面 · 点我翻面</div>
          </template>
          <template #back>
            <div class="face back">背面 · 藏好的话</div>
          </template>
        </FlipCard>
      </div>

      <!-- 10 汉堡变叉 -->
      <div class="cell">
        <div class="tag">10 · 汉堡变叉</div>
        <p class="desc">单一元件形变，不丢失视觉位置。</p>
        <button class="burger-demo" @click="burger = !burger">
          <HamburgerIcon v-model="burger" />
        </button>
        <span class="state">{{ burger ? '菜单开' : '菜单关' }}</span>
      </div>

      <!-- 11 卡片抽走 -->
      <div class="cell">
        <div class="tag">11 · 卡片抽走</div>
        <p class="desc">上层移开时下层可见，体现堆叠关系。</p>
        <div class="stack">
          <div class="stack-base">下层卡片（可见）</div>
          <SwipeCard v-if="cardThere" class="stack-top" hint="抽走" hint-color="#FF6F7D" @dismiss="cardThere = false">
            <div class="face front">上层 · 横滑抽走我</div>
          </SwipeCard>
          <button v-else class="demo-btn ghost sm" @click="cardThere = true">恢复上层</button>
        </div>
      </div>

      <!-- 12 底部抽屉 -->
      <div class="cell">
        <div class="tag">12 · 底部抽屉</div>
        <p class="desc">关键帧留停顿，靠停留感知状态变化。</p>
        <button class="demo-btn rose" @click="drawer = true">从底部升起</button>
        <BottomDrawer v-model="drawer" title="给 TA 的一句话">
          <p style="margin: 4px 0 14px; color: var(--color-ink-2)">抽屉升起时会有一次轻微“落定”的停顿。</p>
          <button class="demo-btn rose w-full" v-ripple @click="drawer = false">发送并收起</button>
        </BottomDrawer>
      </div>
    </section>

    <p class="foot">提示：在「设置 → 减弱动效」开启后，以上动效会即时降级为无位移的静态呈现。</p>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import {
  FinesseSwitch, LiquidSlider, SuccessCheck, NumberRoll, SkeletonSettle,
  FlipCard, HamburgerIcon, SwipeCard, BottomDrawer,
} from './index';

const sw = ref(false);
const lv = ref(40);
const done = ref(false);
const num = ref(3650);
const loading = ref(false);
const flipped = ref(false);
const burger = ref(false);
const cardThere = ref(true);
const drawer = ref(false);
function noop() {}
</script>

<style scoped>
.show { max-width: 980px; margin: 0 auto; padding: 8px 4px 40px; }
.show-head h1 { font-size: 22px; margin: 0 0 6px; }
.sub { color: var(--color-ink-2); font-size: 14px; line-height: 1.7; margin: 0 0 18px; }
.grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 16px; }
.cell {
  background: var(--color-surface); border: 1px solid var(--color-border);
  border-radius: var(--radius-lg); padding: 16px; display: flex; flex-direction: column; gap: 10px;
  box-shadow: 0 1px 2px rgba(31,41,55,0.04), 0 10px 28px -10px rgba(122,100,98,0.14);
}
.tag { font-family: var(--font-mono); font-size: 12px; letter-spacing: 0.04em; color: var(--color-rose); font-weight: 600; }
.desc { color: var(--color-ink-2); font-size: 13px; margin: 0; line-height: 1.6; min-height: 38px; }
.state { font-size: 13px; color: var(--color-ink-3); }
.demo-btn {
  align-self: flex-start; font-family: var(--font-sans); font-weight: 600; font-size: 14px;
  padding: 10px 18px; border-radius: var(--radius-md); cursor: pointer; border: 1px solid var(--color-border);
  background: var(--color-surface); color: var(--color-ink); transition: all var(--fx-dur-micro, 140ms) var(--fx-ease-soft, ease);
}
.demo-btn.rose { background: var(--color-rose); color: #fff; border-color: var(--color-rose); }
.demo-btn.ghost { display: inline-flex; align-items: center; gap: 8px; }
.demo-btn.sm { padding: 7px 12px; font-size: 13px; }
.demo-btn.w-full { width: 100%; align-self: stretch; }
.demo-btn:active { transform: scale(0.97); }
.big { font-size: 40px; font-weight: 800; color: var(--color-rose); font-variant-numeric: tabular-nums; line-height: 1.1; }
.sk-demo { width: 100%; }
.real { display: flex; flex-direction: column; gap: 4px; }
.real b { color: var(--color-ink); }
.real span { color: var(--color-ink-2); font-size: 13px; }
.face { width: 100%; height: 100%; display: grid; place-items: center; border-radius: var(--radius-lg); font-weight: 600; color: #fff; }
.face.front { background: linear-gradient(135deg, var(--color-rose), var(--color-rose-deep)); }
.face.back { background: linear-gradient(135deg, var(--color-cocoa), var(--color-ink-2)); }
.burger-demo { border: none; background: none; cursor: pointer; color: var(--color-ink); padding: 8px; }
.stack { position: relative; height: 120px; }
.stack-base, .stack-top {
  position: absolute; inset: 0; border-radius: var(--radius-lg); display: grid; place-items: center;
  font-weight: 600; color: #fff;
}
.stack-base { background: linear-gradient(135deg, var(--color-cocoa), var(--color-ink-2)); }
.stack-top { background: linear-gradient(135deg, var(--color-rose), var(--color-rose-deep)); box-shadow: 0 10px 30px -8px rgba(255,111,125,0.5); z-index: 2; }
.foot { margin-top: 22px; color: var(--color-ink-3); font-size: 12px; text-align: center; }
</style>
