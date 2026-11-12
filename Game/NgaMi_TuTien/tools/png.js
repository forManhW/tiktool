// Minimal PNG decode/encode + resize helpers (8-bit RGB/RGBA, non-interlaced).
const fs = require("fs"), zlib = require("zlib");

function decode(file) {
  const buf = fs.readFileSync(file);
  let pos = 8, W, H, ct, idat = [];
  while (pos < buf.length) {
    const len = buf.readUInt32BE(pos), type = buf.toString("ascii", pos + 4, pos + 8), data = buf.subarray(pos + 8, pos + 8 + len);
    if (type === "IHDR") { W = data.readUInt32BE(0); H = data.readUInt32BE(4); ct = data[9]; if (data[8] !== 8 || data[12] || (ct !== 2 && ct !== 6)) throw new Error("unsupported png " + file); }
    if (type === "IDAT") idat.push(data);
    pos += 12 + len;
  }
  const bpp = ct === 6 ? 4 : 3, raw = zlib.inflateSync(Buffer.concat(idat)), stride = W * bpp, px = Buffer.alloc(W * H * 4);
  let prev = Buffer.alloc(stride);
  for (let y = 0; y < H; y++) {
    const f = raw[y * (stride + 1)], line = Buffer.from(raw.subarray(y * (stride + 1) + 1, (y + 1) * (stride + 1)));
    for (let x = 0; x < stride; x++) {
      const a = x >= bpp ? line[x - bpp] : 0, b = prev[x], c = x >= bpp ? prev[x - bpp] : 0;
      let v = line[x];
      if (f === 1) v += a; else if (f === 2) v += b; else if (f === 3) v += (a + b) >> 1;
      else if (f === 4) { const p = a + b - c, pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c); v += pa <= pb && pa <= pc ? a : pb <= pc ? b : c; }
      line[x] = v & 255;
    }
    for (let x = 0; x < W; x++) { const o = (y * W + x) * 4; px[o] = line[x * bpp]; px[o + 1] = line[x * bpp + 1]; px[o + 2] = line[x * bpp + 2]; px[o + 3] = bpp === 4 ? line[x * bpp + 3] : 255; }
    prev = line;
  }
  return { W, H, ct, px };
}

function crc32(b) { let crc = ~0; for (let n = 0; n < b.length; n++) { let c = (crc ^ b[n]) & 255; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; crc = (crc >>> 8) ^ c; } return ~crc >>> 0; }
function chunk(t, d) { const l = Buffer.alloc(4); l.writeUInt32BE(d.length); const td = Buffer.concat([Buffer.from(t), d]); const c = Buffer.alloc(4); c.writeUInt32BE(crc32(td)); return Buffer.concat([l, td, c]); }
// Encodes RGBA (or RGB when alpha is unused) with Paeth filter for better compression.
function encode(img, { rgb = false } = {}) {
  const { W, H, px } = img, bpp = rgb ? 3 : 4, stride = W * bpp, rows = Buffer.alloc((stride + 1) * H);
  let prev = Buffer.alloc(stride);
  for (let y = 0; y < H; y++) {
    const line = Buffer.alloc(stride);
    for (let x = 0; x < W; x++) for (let k = 0; k < bpp; k++) line[x * bpp + k] = px[(y * W + x) * 4 + k];
    const o = y * (stride + 1); rows[o] = 4;
    for (let x = 0; x < stride; x++) {
      const a = x >= bpp ? line[x - bpp] : 0, b = prev[x], c = x >= bpp ? prev[x - bpp] : 0, p = a + b - c, pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c);
      rows[o + 1 + x] = (line[x] - (pa <= pb && pa <= pc ? a : pb <= pc ? b : c)) & 255;
    }
    prev = line;
  }
  const ih = Buffer.alloc(13); ih.writeUInt32BE(W, 0); ih.writeUInt32BE(H, 4); ih[8] = 8; ih[9] = rgb ? 2 : 6;
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk("IHDR", ih), chunk("IDAT", zlib.deflateSync(rows, { level: 9 })), chunk("IEND", Buffer.alloc(0))]);
}

// Area-average resize with premultiplied alpha (no dark/white fringes).
function resize(img, tw, th) {
  const { W, H, px } = img, out = Buffer.alloc(tw * th * 4);
  for (let y = 0; y < th; y++) {
    const y0 = y * H / th, y1 = (y + 1) * H / th;
    for (let x = 0; x < tw; x++) {
      const x0 = x * W / tw, x1 = (x + 1) * W / tw;
      let r = 0, g = 0, b = 0, a = 0, wsum = 0;
      for (let yy = Math.floor(y0); yy < Math.ceil(y1); yy++) {
        const wy = Math.min(yy + 1, y1) - Math.max(yy, y0);
        for (let xx = Math.floor(x0); xx < Math.ceil(x1); xx++) {
          const w = wy * (Math.min(xx + 1, x1) - Math.max(xx, x0)), i = (yy * W + xx) * 4, al = px[i + 3] / 255;
          r += px[i] * al * w; g += px[i + 1] * al * w; b += px[i + 2] * al * w; a += al * w; wsum += w;
        }
      }
      const o = (y * tw + x) * 4;
      if (a > 0) { out[o] = Math.round(r / a); out[o + 1] = Math.round(g / a); out[o + 2] = Math.round(b / a); }
      out[o + 3] = Math.round(255 * a / wsum);
    }
  }
  return { W: tw, H: th, px: out };
}

function stats(img) {
  const { W, H, px } = img;
  let transparent = 0, opaque = 0, semi = 0, minX = W, minY = H, maxX = -1, maxY = -1;
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const al = px[(y * W + x) * 4 + 3];
    if (al === 0) transparent++; else if (al === 255) opaque++; else semi++;
    if (al > 16) { if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y; }
  }
  const n = W * H;
  return { W, H, transparentPct: +(100 * transparent / n).toFixed(1), semiPct: +(100 * semi / n).toFixed(1), bbox: [minX, minY, maxX, maxY] };
}

function blank(W, H, rgba) { const px = Buffer.alloc(W * H * 4); for (let i = 0; i < W * H; i++) px.set(rgba, i * 4); return { W, H, px }; }
// Alpha-composite src onto dst at (ox, oy).
function blit(dst, src, ox, oy) {
  for (let y = 0; y < src.H; y++) for (let x = 0; x < src.W; x++) {
    const dx = ox + x, dy = oy + y; if (dx < 0 || dy < 0 || dx >= dst.W || dy >= dst.H) continue;
    const s = (y * src.W + x) * 4, d = (dy * dst.W + dx) * 4, a = src.px[s + 3] / 255;
    for (let k = 0; k < 3; k++) dst.px[d + k] = Math.round(src.px[s + k] * a + dst.px[d + k] * (1 - a));
    dst.px[d + 3] = 255;
  }
}

module.exports = { decode, encode, resize, stats, blank, blit };
