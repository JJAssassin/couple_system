// 生成「我们的小世界」安卓全套图标（纯 Node + zlib，无第三方依赖）
// 心形函数：(x²+y²-1)³ - x²y³ ≤ 0（与 PWA 图标一致）
// 输出到 mobile/android/app/src/main/res/：
//   mipmap-{mdpi,hdpi,xhdpi,xxhdpi,xxxhdpi}/ic_launcher.png        （legacy：粉色圆角 + 白心）
//   mipmap-{...}/ic_launcher_round.png                             （圆形 + 白心）
//   mipmap-{...}/ic_launcher_foreground.png                        （adaptive 前景：透明 + 白心，安全区缩放）
// 背景色改 values/ic_launcher_background.xml（由调用方完成）
import { deflateSync } from 'zlib';
import { writeFileSync, mkdirSync } from 'fs';
import { join } from 'path';

const RES = 'D:/Code/My_vscode/couple-love-system/mobile/android/app/src/main/res';
const ROSE = [255, 111, 125];          // #ff6f7d 品牌粉
const ROSE_DEEP = [255, 136, 147];     // 渐变深端（legacy 背景用浅→深微渐变）

// ---------- PNG 编码（RGBA，无滤波） ----------
function crc32(buf) {
  let c = ~0;
  for (let i = 0; i < buf.length; i++) {
    c ^= buf[i];
    for (let k = 0; k < 8; k++) c = c & 1 ? (c >>> 1) ^ 0xedb88320 : c >>> 1;
  }
  return ~c >>> 0;
}
function chunk(type, data) {
  const len = Buffer.alloc(4);
  len.writeUInt32BE(data.length);
  const t = Buffer.from(type, 'ascii');
  const crc = Buffer.alloc(4);
  crc.writeUInt32BE(crc32(Buffer.concat([t, data])));
  return Buffer.concat([len, t, data, crc]);
}
function encodePng(size, rgba) {
  const sig = Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(size, 0);
  ihdr.writeUInt32BE(size, 4);
  ihdr[8] = 8; ihdr[9] = 6; // 8bit RGBA
  const raw = Buffer.alloc(size * (size * 4 + 1));
  for (let y = 0; y < size; y++) {
    raw[y * (size * 4 + 1)] = 0; // filter: None
    rgba.copy(raw, y * (size * 4 + 1) + 1, y * size * 4, (y + 1) * size * 4);
  }
  return Buffer.concat([sig, chunk('IHDR', ihdr), chunk('IDAT', deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]);
}

// ---------- 形状判定 ----------
function heart(nx, ny) { // nx,ny ∈ [-1,1]
  const a = nx * nx + ny * ny - 1;
  return a * a * a - nx * nx * ny * ny * ny <= 0;
}
function roundedRect(nx, ny, r) {
  const ax = Math.abs(nx), ay = Math.abs(ny);
  if (ax > 1 - r || ay > 1 - r) {
    const cx = Math.max(ax - (1 - r), 0), cy = Math.max(ay - (1 - r), 0);
    return cx * cx + cy * cy <= r * r;
  }
  return true;
}

// ---------- 渲染（4x 超采样抗锯齿） ----------
function render(size, opts) {
  const { bg, heartR, shape } = opts; // shape: 'rect'|'round'|'none'
  const rgba = Buffer.alloc(size * size * 4);
  const ss = 4; // 超采样
  for (let py = 0; py < size; py++) {
    for (let px = 0; px < size; px++) {
      let aH = 0, aBg = 0;
      for (let sy = 0; sy < ss; sy++) {
        for (let sx = 0; sx < ss; sx++) {
          const fx = px + (sx + 0.5) / ss;
          const fy = py + (sy + 0.5) / ss;
          const nx = (fx / size) * 2 - 1;
          const ny = (fy / size) * 2 - 1; // y 向下
          if (heart(nx / heartR, -ny / heartR)) aH += 1; // 心形（v 翻转）
          else if (shape === 'rect' && roundedRect(nx, ny, 0.18)) aBg += 1;
          else if (shape === 'round' && nx * nx + ny * ny <= 1) aBg += 1;
        }
      }
      const h = aH / (ss * ss), b = aBg / (ss * ss);
      const i = (py * size + px) * 4;
      if (h > 0) {
        rgba[i] = 255; rgba[i + 1] = 255; rgba[i + 2] = 255; rgba[i + 3] = Math.round(h * 255);
      } else if (b > 0) {
        // 背景微渐变：上浅下深
        const t = py / size;
        rgba[i] = Math.round(ROSE[0] + (ROSE_DEEP[0] - ROSE[0]) * t);
        rgba[i + 1] = Math.round(ROSE[1] + (ROSE_DEEP[1] - ROSE[1]) * t);
        rgba[i + 2] = Math.round(ROSE[2] + (ROSE_DEEP[2] - ROSE[2]) * t);
        rgba[i + 3] = Math.round(b * 255);
      } else {
        rgba[i + 3] = 0; // 透明
      }
    }
  }
  return encodePng(size, rgba);
}

// ---------- 各密度规格 ----------
const DENSITIES = { mdpi: 48, hdpi: 72, xhdpi: 96, xxhdpi: 144, xxxhdpi: 192 };
const FG_DENSITIES = { mdpi: 108, hdpi: 162, xhdpi: 216, xxhdpi: 324, xxxhdpi: 432 };

for (const [d, s] of Object.entries(DENSITIES)) {
  const dir = join(RES, `mipmap-${d}`);
  mkdirSync(dir, { recursive: true });
  writeFileSync(join(dir, 'ic_launcher.png'), render(s, { bg: true, heartR: 0.28, shape: 'rect' }));
  writeFileSync(join(dir, 'ic_launcher_round.png'), render(s, { bg: true, heartR: 0.28, shape: 'round' }));
}
for (const [d, s] of Object.entries(FG_DENSITIES)) {
  // adaptive 前景：透明背景，心形落在安全区（66/108 ≈ 61% → 心形半径 ~0.21 归一）
  writeFileSync(join(RES, `mipmap-${d}`, 'ic_launcher_foreground.png'), render(s, { bg: false, heartR: 0.21, shape: 'none' }));
}
console.log('icons generated:', Object.keys(DENSITIES).length * 3, 'files');
