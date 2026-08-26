# 微交互动效参考指南（Finesse Interaction Guide）

> 本指南把来自 GitHub 的三个 `mouse-lin` 设计技能（`finesse-ui` / `finesse-brief` / `frame-smith`）
> 的「高级感哲学」落地为本项目的 12 项微交互。核心主张只有一句：
>
> **不靠堆特效，给每一次操作恰到好处的反馈。**

技能已安装到用户级目录 `~/.workbuddy/skills/`（`finesse-ui`、`finesse-brief`、`frame-smith`），
本指南是其方法论在本情侣系统前端的具体化。

---

## 0. 哲学内核（来自技能，已提炼）

从三个技能提炼出 6 条本库必须遵守的铁律：

1. **只动 `transform` / `opacity`** —— 绝不动 `top/left/width/height`，避免重排与卡顿。
2. **尊重降级** —— 所有动效 gate 在 `html.reduce-motion`（用户设置）与系统 `prefers-reduced-motion` 之后；
   降级时直接静态呈现，不做位移。
3. **力度匹配操作** —— 轻点轻回弹，重操作才有重量；不要每个按钮都做夸张动画。
4. **一次只让一个元素抢戏** —— 同一画面避免多个 heavy 动效同时发生（frame-smith：one job per beat）。
5. **指针动效要在 `pointer:fine` 后触发** —— 触摸端不播「磁吸 / 悬停跟随」类效果（本库已用 `v-ripple` 等
   跟随手指的动效替代纯 hover 跟随，天然适配移动端）。
6. **完成感** —— 动效要有「落点」：对勾要画完、数字要减速停稳、抽屉要停一下再收，
   不中途生硬骤停（frame-smith：the clip lands, it doesn't just stop）。

> 反廉价黑名单（finesse-ui）节选：禁用线性缓动做可见位移、禁用「统一淡入上移+0.5s」模板、
> 禁用纯 `#fff/#000`、禁用 AI 紫光 / 默认玻璃拟态。本库全部采用项目既有浪漫柔光令牌
> （`--color-rose` 等），与之天然契合。

---

## 1. 设计令牌（统一调校入口）

定义在 `src/interactions/finesse.css` 的 `:root`，改这里即可全站调手感：

```css
--fx-dur-micro:  140ms;   /* 按压下压 */
--fx-dur-pop:    320ms;   /* 通用弹出 / 回弹 */
--fx-dur-settle: 420ms;   /* 数字收尾 / 骨架落位 */
--fx-dur-drawer: 380ms;   /* 抽屉 */
--fx-ease-back:  cubic-bezier(0.34, 1.56, 0.64, 1);  /* 带克制的回弹（重量感） */
--fx-ease-out:   cubic-bezier(0.16, 1, 0.3, 1);       /* 减速收尾 */
--fx-ease-soft:  cubic-bezier(0.4, 0, 0.2, 1);
```

---

## 2. 12 项交互总览

| # | 交互 | 传达的感受 | 实现（文件） | 已接入位置 |
|---|------|-----------|--------------|-----------|
| 01 | 按压回弹 | 按键有重量，下压过基准再回弹 | 指令 `v-press-bounce`（`directives.ts`） | **设置/愿望/记账/待办 主操作按钮**已接入 |
| 02 | 弹性开关 | 滑块有质量，起步先拉长再归位 | `FinesseSwitch.vue` | **设置页 减少动效 / 系统通知 两开关**已接入 |
| 03 | 水波按钮 | 反馈跟随手指，从点击点扩散 | 指令 `v-ripple`（`directives.ts`） | **侧边栏导航项 / 消息铃**已接入 |
| 04 | 点击爆散 | 力度匹配操作，强化「按到了」 | 指令 `v-click-burst`（`directives.ts`） | **记账·保存到相册 / 愿望·完成啦**已接入 |
| 05 | 成功勾选 | 对勾逐笔绘制，体现完成过程 | `SuccessCheck.vue` | **待办完成勾选**已接入 |
| 06 | 液态滑块 | 轨道/滑钮/数值三组同频同步 | `LiquidSlider.vue` | **记账·当月总预算**已接入 |
| 07 | 数字滚动 | 缓慢减速停下，不硬切 | `NumberRoll.vue` + `useNumberRoll` | **年度统计 KPI**已接入 |
| 08 | 骨架落位 | 骨架与内容同尺寸，切换不跳动 | `SkeletonSettle.vue` | 组件库；Showcase 演示 |
| 09 | 卡片翻面 | 正反面预藏，翻转不穿帮 | `FlipCard.vue` | 组件库；Showcase 演示 |
| 10 | 汉堡变叉 | 单一元件形变，不丢视觉锚点 | `HamburgerIcon.vue` | **AppShell 移动端菜单键**已接入 |
| 11 | 卡片抽走 | 上层移开露出下层，体现堆叠 | `SwipeCard.vue` | 组件库；Showcase 演示 |
| 12 | 底部抽屉 | 关键帧留停顿，让人「看见」停下了 | `BottomDrawer.vue` | **愿望·加愿望 / 编辑表单**已接入 |

> 标注「已接入」的为真实页面改动；其余以组件/指令形式就位于 `src/interactions/`，
> 由 Showcase（`/finesse` 路由）统一演示，可按需继续接入任意页面。
> 截至本轮，**12 项中已有 9 项接入真实页面**（01/02/03/04/05/06/07/10/12），
> 仅 08 骨架落位、09 卡片翻面、11 卡片抽走 仍在 Showcase 演示。

---

## 3. 各项实现要点与接入方式

### 01 按压回弹 · `v-press-bounce`
`pointerdown` 时 `transform: scale(0.96)`，`pointerup` 播放 `fx-press-back` 关键帧
（0.96 → 1.015 → 1，越过敏感的基准再落定，制造重量回弹）。不写繁琐 transition 时序。
```html
<button v-press-bounce>按住我</button>
```

### 02 弹性开关 · `FinesseSwitch`
开关切换时，旋钮先 `scaleX(1.35)` 横向拉长、再回收，配合 `translateX` 位移，落位有质量感。
`--knob-x` 用 CSS 变量同步位移与拉伸，避免两处 transform 冲突。
```html
<FinesseSwitch v-model="on" />
```
已接入设置页「减少动效」「系统通知」两处开关（`v-model` 绑定 `setting.reduceMotion` / `setting.notifications`，`update:modelValue` 调 `setReduceMotion` / `onToggleNotify`）。

### 03 水波按钮 · `v-ripple`
`pointerdown` 注入 `<span class="fx-ripple">`，波纹半径取「到最远角距离」，从点击点扩散并淡出；
宿主自动获得 `fx-ripple-host`（`position:relative; overflow:hidden`）。已接入侧边栏导航与消息铃。
```html
<button v-ripple>点我泛波</button>
```

### 04 点击爆散 · `v-click-burst`
`pointerdown` 在点击点迸发 7 个粒子（`--bx/--by` 极坐标发散），力度统一、不喧宾夺主。
适合「点赞 / 收藏」等轻量确认。
```html
<button v-click-burst>点我迸发</button>
```
适合「点赞 / 收藏 / 完成」等轻量确认。已接入记账页「保存到相册」、愿望页「完成啦」。

### 05 成功勾选 · `SuccessCheck`
SVG 对勾用 `stroke-dasharray/dashoffset` 描边动画，**逐笔绘制**；`active` 变 true 时重放，
体现「完成的过程感」。已替代待办勾选处的静态图标。
```html
<SuccessCheck :active="done" :size="16" :show-circle="false" color="#fff" />
```

### 06 液态滑块 · `LiquidSlider`
轨道填充、滑钮、数值三者共用 `--fx-dur-pop / --fx-ease-out`，**同频同步**；拖拽用 `pointer` 事件 +
`touch-action:none`，数值用 `useNumberRoll` 轻微滚动。支持键盘/点击设值。
```html
<LiquidSlider v-model="v" :min="0" :max="100" suffix="%" />
```
已接入记账页「当月总预算」设定（`v-model` 绑定计算属性 `budgetTotal` 桥接 `bForm.total`，范围 0–20000 / 步进 100 / 单位 元）。

### 07 数字滚动 · `NumberRoll`
监听目标值变化，用 `requestAnimationFrame` + `easeOutQuart` 把显示值**缓慢减速**到目标，
绝不在中途硬切；`reduce-motion` 时直接跳变。已接入年度统计的全部 KPI。
```html
<NumberRoll :value="report.loveDays" />  <!-- 自动千分位 -->
```

### 08 骨架落位 · `SkeletonSettle`
骨架与正式内容**同尺寸**（错落行宽避免死板），加载完成切换时只做 `fx-settle-in` 轻位移淡入，
杜绝 CLS 跳动。复用全局 `.sk-base` 流光。
```html
<SkeletonSettle :loading="loading" :lines="3"><!-- 真实内容 --></SkeletonSettle>
```

### 09 卡片翻面 · `FlipCard`
容器 `perspective` + 内层 `preserve-3d`，正反面 `backface-visibility:hidden` 提前藏好背面，
`rotateY(180deg)` 翻转**不穿帮**。`interactive` 可点击翻转。
```html
<FlipCard v-model="flipped" interactive><template #front>…</template><template #back>…</template></FlipCard>
```

### 10 汉堡变叉 · `HamburgerIcon`
三道横杠单一元件形变（旋转 45° + 中杠淡出），形变过程保持居中，**用户不丢失视觉锚点**。
已作为 AppShell 移动端菜单键（外观）接入，由外层按钮负责点击。
```html
<HamburgerIcon :model-value="drawerOpen" />
```

### 11 卡片抽走 · `SwipeCard`
`touch-action:pan-y` 让纵向滚动优先，明确横滑才抽走；超过阈值平移 + 淡出并 `emit('dismiss')`，
下层卡片自然露出，体现**堆叠关系**。适合列表项「左滑删除 / 标记」。
```html
<SwipeCard @dismiss="remove(item)"><!-- 卡片内容 --></SwipeCard>
```

### 12 底部抽屉 · `BottomDrawer`
面板用 `fx-drawer-up` 关键帧：上滑到位（72%）→ 轻微过冲（82%，-6px）→ 落定（100%），
**留一次停顿**让人「看见」状态变化；遮罩同步淡入；Esc / 点遮罩关闭；`reduce-motion` 降级为纯淡入。
```html
<BottomDrawer v-model="open" title="给 TA 的一句话"><!-- 内容 --></BottomDrawer>
```
已接入愿望页「加愿望 / 编辑」表单（替代原居中模态，移动端从底部升起更自然；Esc / 点遮罩关闭）。

---

## 4. 如何预览

开发态访问 **`/finesse`** 路由（需在登录态下），即「微交互动效 · 恰到好处的反馈」演示页，
12 项全部可交互体验；在「设置 → 减弱动效」开启后，可即时对比降级表现。

---

## 5. 反模式（不要这样做）

- ❌ 给每个按钮都加 `v-press-bounce` + `v-ripple` + `v-click-burst` → 信息过载。**精选少数高频操作**。
- ❌ 用 `linear` 做可见位移 → 廉价感（frame-smith 头号 tell）。
- ❌ 动效时长全写 `0.5s` 一刀切 → 缺乏节奏。
- ❌ 骨架屏与真实内容高度不一致 → 加载完「跳一下」（违背「骨架落位」初衷）。
- ❌ 在触摸端依赖 hover 跟随类效果 → 用 `v-ripple`（跟随手指）替代。
- ❌ 忘记 `reduce-motion` 降级 → 前庭敏感用户会不适。

---

## 6. 文件索引

```
src/interactions/
├── finesse.css                # 全局 keyframes + 指令注入元素样式 + 设计令牌
├── directives.ts              # v-ripple / v-press-bounce / v-click-burst + 注册器
├── index.ts                   # 统一出口（组件 / 指令 / 组合式）
├── components/
│   ├── FinesseSwitch.vue      # 02 弹性开关
│   ├── LiquidSlider.vue       # 06 液态滑块
│   ├── SuccessCheck.vue       # 05 成功勾选
│   ├── NumberRoll.vue         # 07 数字滚动
│   ├── SkeletonSettle.vue     # 08 骨架落位
│   ├── FlipCard.vue           # 09 卡片翻面
│   ├── HamburgerIcon.vue      # 10 汉堡变叉
│   ├── SwipeCard.vue          # 11 卡片抽走
│   └── BottomDrawer.vue       # 12 底部抽屉
├── composables/
│   ├── useNumberRoll.ts       # 07 减速滚动核心
│   ├── useCardFlip.ts         # 09 状态
│   └── useBottomDrawer.ts     # 12 状态
└── Showcase.vue               # /finesse 演示页（12 项全演示）
```

接入方式：`main.ts` 已 `import './interactions/finesse.css'` 并 `registerFinesseDirectives(app)`，
指令全局可用；组件按需 `import { Xxx } from '@/interactions'`。
