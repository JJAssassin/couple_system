// 生成 iOS App 图标（1024x1024，不透明粉底 + 白心，无圆角——iOS 系统自动裁圆角）
// 输出到 mobile/ios/App/App/Assets.xcassets/AppIcon.appiconset/AppIcon-512@2x.png
import { deflateSync } from 'zlib';
import { writeFileSync } from 'fs';

const SIZE = 1024;
const ROSE = [255, 111, 125];
const ROSE_DEEP = [255, 148, 158];

function crc32(buf) {
  let c = ~0;
  for (let i = 0; i < buf.length; i++) {
    c ^= buf[i];
    for (let k = 0; k < 8; k++) c = c & 1 ? (c >>> 1) ^ 0xedb88320 : c >>> 1;
  }
  return ~c >>> 0;
}
function chunk(type, data) {
  const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
  const t = Buffer.from(type, 'ascii');
  const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(Buffer.concat([t, data])));
  return Buffer.concat([len, t, data, crc]);
}
function encodePng(size, rgba) {
  const sig = Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]);
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(size, 0); ihdr.writeUInt32BE(size, 4);
  ihdr[8] = 8; ihdr[9] = 6;
  const raw = Buffer.alloc(size * (size * 4 + 1));
  for (let y = 0; y < size; y++) {
    raw[y * (size * 4 + 1)] = 0;
    rgba.copy(raw, y * (size * 4 + 1) + 1, y * size * 4, (y + 1) * size * 4);
  }
  return Buffer.concat([sig, chunk('IHDR', ihdr), chunk('IDAT', deflateSync(raw)), chunk('IEND', Buffer.alloc(0))]);
}
function heart(nx, ny) {
  const a = nx * nx + ny * ny - 1;
  return a * a * a - nx * nx * ny * ny * ny <= 0;
}

const rgba = Buffer.alloc(SIZE * SIZE * 4);
const ss = 4;
for (let py = 0; py < SIZE; py++) {
  for (let px = 0; px < SIZE; px++) {
    let aH = 0;
    for (let sy = 0; sy < ss; sy++) {
      for (let sx = 0; sx < ss; sx++) {
        const fx = px + (sx + 0.5) / ss;
        const fy = py + (sy + 0.5) / ss;
        const nx = (fx / SIZE) * 2 - 1;
        const ny = (fy / SIZE) * 2 - 1;
        if (heart(nx / 0.42, -ny / 0.42)) aH += 1; // 心形占 ~42% 半径
      }
    }
    const h = aH / (ss * ss);
    const i = (py * SIZE + px) * 4;
    if (h > 0) {
      rgba[i] = 255; rgba[i + 1] = 255; rgba[i + 2] = 255; rgba[i + 3] = 255;
    } else {
      const t = py / SIZE; // 背景微渐变（不透明）
      rgba[i] = Math.round(ROSE[0] + (ROSE_DEEP[0] - ROSE[0]) * t);
      rgba[i + 1] = Math.round(ROSE[1] + (ROSE_DEEP[1] - ROSE[1]) * t);
      rgba[i + 2] = Math.round(ROSE[2] + (ROSE_DEEP[2] - ROSE[2]) * t);
      rgba[i + 3] = 255;
    }
  }
}

const out = 'D:/Code/My_vscode/couple-love-system/mobile/ios/App/App/Assets.xcassets/AppIcon.appiconset/AppIcon-512@2x.png';
writeFileSync(out, encodePng(SIZE, rgba));
console.log('iOS icon generated:', out);
