#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
生成 iOS 启动屏三张图（1x / 2x / 3x）。

设计：玫粉径向渐变背景（呼应 AppIcon 的玫瑰心），中央一颗奶白心形 +
「我们的小世界」中文主标题 + 「OUR LITTLE WORLD」英文小字 + 几粒漂浮小爱心。
所有内容严格限制在中心 55% 安全区内，iOS 用 scaleAspectFill 裁切到不同
屏幕比例时也不会切到关键元素。

依赖：Pillow（pip install Pillow）、Windows 自带 msyh.ttc（微软雅黑）渲染中文。
直接运行：`python scripts/generate_ios_splash.py`
输出：覆盖 mobile/ios/App/App/Assets.xcassets/Splash.imageset/ 下三张 png。
"""
from __future__ import annotations

import math
import os
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

# 资源定位（脚本位置无关）
HERE = Path(__file__).resolve().parent
REPO = HERE.parent
OUT_DIR = REPO / "mobile" / "ios" / "App" / "App" / "Assets.xcassets" / "Splash.imageset"
FONT_CANDIDATES = [
    r"C:\Windows\Fonts\msyh.ttc",       # 微软雅黑（首选，渲染中文最稳）
    r"C:\Windows\Fonts\msyhbd.ttc",
    r"C:\Windows\Fonts\simhei.ttf",
    r"C:\Windows\Fonts\NotoSansSC-VF.ttf",
]

# 配色（与 AppIcon 玫瑰心呼应）
BG_CENTER = (247, 163, 163)   # #f7a3a3 玫粉
BG_EDGE = (253, 230, 221)     # #fde6dd 桃奶
HEART_FILL = (251, 242, 240)  # #fbf2f0 奶白心
HEART_SHADOW = (220, 140, 140, 90)
TITLE_COLOR = (255, 255, 255, 255)
TITLE_SHADOW = (190, 90, 90, 110)
SUBTITLE_COLOR = (255, 255, 255, 235)
DECO_HEART = (255, 255, 255, 110)


def _load_font(size: int) -> ImageFont.FreeTypeFont:
    last_err: Exception | None = None
    for p in FONT_CANDIDATES:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size=size)
            except Exception as e:  # pragma: no cover
                last_err = e
    raise RuntimeError(f"找不到可用中文字体：{FONT_CANDIDATES}（{last_err}）")


def _radial_gradient(size: int) -> Image.Image:
    """从中心 BG_CENTER 渐变到四角 BG_EDGE 的方形图。"""
    img = Image.new("RGB", (size, size), BG_EDGE)
    px = img.load()
    cx = cy = size / 2.0
    max_d = math.hypot(cx, cy)
    for y in range(size):
        for x in range(size):
            d = math.hypot(x - cx, y - cy) / max_d  # 0..1
            d = min(max(d, 0.0), 1.0)
            t = d ** 1.35  # 让中心更饱和，边缘更柔
            r = int(BG_CENTER[0] * (1 - t) + BG_EDGE[0] * t)
            g = int(BG_CENTER[1] * (1 - t) + BG_EDGE[1] * t)
            b = int(BG_CENTER[2] * (1 - t) + BG_EDGE[2] * t)
            px[x, y] = (r, g, b)
    return img


def _heart_outline_points(width: float, cx: float, cy: float) -> list[tuple[float, float]]:
    """经典心形参数曲线：x=16 sin³t, y=13cos t − 5cos 2t − 2cos 3t − cos 4t
    采样得到闭合多边形点集（视觉上是个心）。"""
    pts: list[tuple[float, float]] = []
    n = 360
    for i in range(n):
        t = 2 * math.pi * i / n
        # 原方程 y 向下，图片 y 向下，所以取负
        x = 16 * (math.sin(t) ** 3)
        y = -(13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t))
        # 归一化到 [0, ~17] x [0, ~17]，再缩放到目标 width
        pts.append((cx + x * (width / 34.0), cy + y * (width / 34.0)))
    return pts


def _draw_heart(layer: Image.Image, cx: float, cy: float, width: float,
                fill_rgba, shadow: bool = True) -> None:
    """在 layer 上画一个心形（带可选投影）。"""
    if shadow:
        # 软投影：先画一个偏移+模糊的暗心，再画正心
        shadow_layer = Image.new("RGBA", layer.size, (0, 0, 0, 0))
        sd = ImageDraw.Draw(shadow_layer)
        pts = _heart_outline_points(width, cx + width * 0.012, cy + width * 0.018)
        sd.polygon(pts, fill=HEART_SHADOW)
        shadow_layer = shadow_layer.filter(ImageFilter.GaussianBlur(radius=width * 0.025))
        layer.alpha_composite(shadow_layer)

    draw = ImageDraw.Draw(layer)
    pts = _heart_outline_points(width, cx, cy)
    draw.polygon(pts, fill=fill_rgba)


def _draw_deco_hearts(layer: Image.Image, size: int) -> None:
    """在安全区外画几粒漂浮小爱心作装饰。"""
    rng_offsets = [
        (0.18, 0.30, 0.05),
        (0.82, 0.28, 0.045),
        (0.14, 0.72, 0.04),
        (0.86, 0.70, 0.05),
        (0.50, 0.14, 0.035),
        (0.50, 0.88, 0.04),
    ]
    for fx, fy, ratio in rng_offsets:
        cx = size * fx
        cy = size * fy
        w = size * ratio
        _draw_heart(layer, cx, cy, w, DECO_HEART, shadow=False)


def _draw_center_text(layer: Image.Image, size: int) -> None:
    """中央主标题 + 副标题（中英）。"""
    # 主标题在主心下方；整体位于中心 55% 安全区内
    title_font = _load_font(int(size * 0.07))
    sub_font = _load_font(int(size * 0.021))
    title = "我们的小世界"
    subtitle = "OUR  LITTLE  WORLD"

    # 软投影层
    shadow_layer = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(shadow_layer)
    bbox_t = sd.textbbox((0, 0), title, font=title_font)
    bbox_s = sd.textbbox((0, 0), subtitle, font=sub_font)
    tw, th = bbox_t[2] - bbox_t[0], bbox_t[3] - bbox_t[1]
    sw, sh = bbox_s[2] - bbox_s[0], bbox_s[3] - bbox_s[1]
    cx = size / 2
    title_y = size * 0.575
    sub_y = title_y + th * 1.7

    # 投影（轻微下移 + 模糊）
    sd.text((cx - tw / 2 + size * 0.004, title_y + size * 0.006), title,
            font=title_font, fill=TITLE_SHADOW)
    sd.text((cx - sw / 2, sub_y + size * 0.003), subtitle, font=sub_font,
            fill=(190, 90, 90, 80))
    shadow_layer = shadow_layer.filter(ImageFilter.GaussianBlur(radius=size * 0.004))
    layer.alpha_composite(shadow_layer)

    # 正式文字
    draw = ImageDraw.Draw(layer)
    draw.text((cx - tw / 2, title_y), title, font=title_font, fill=TITLE_COLOR)
    draw.text((cx - sw / 2, sub_y), subtitle, font=sub_font, fill=SUBTITLE_COLOR)


def render_splash(size: int) -> Image.Image:
    """渲染一张 size×size 的品牌启动图。"""
    base = _radial_gradient(size).convert("RGBA")
    _draw_deco_hearts(base, size)

    # 中心主心（最大、最显眼）
    main_w = size * 0.22
    cx, cy = size / 2, size * 0.46
    _draw_heart(base, cx, cy, main_w, HEART_FILL + (255,), shadow=True)

    # 文字
    _draw_center_text(base, size)
    return base


def main() -> int:
    if not OUT_DIR.exists():
        print(f"❌ 输出目录不存在：{OUT_DIR}", file=sys.stderr)
        return 1
    # Contents.json：1x=-2, 2x=-1, 3x=plain
    targets = [
        (1366, OUT_DIR / "splash-2732x2732-2.png"),
        (2048, OUT_DIR / "splash-2732x2732-1.png"),
        (2732, OUT_DIR / "splash-2732x2732.png"),
    ]
    for size, out in targets:
        img = render_splash(size)
        img.save(out, format="PNG", optimize=True)
        print(f"✅  {out.name}  {size}×{size}  ({out.stat().st_size / 1024:.0f} KB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
