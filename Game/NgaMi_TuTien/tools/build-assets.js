// Thu nhỏ tranh gốc (src/assets/raw) thành bản nhẹ cho game (src/assets/game), chép âm thanh.
//   node tools/build-assets.js
// Chạy lại mỗi khi anh thêm/sửa tranh gốc (vd. khi có hoaThan.png, luyenHu.png).
const fs = require("fs"), path = require("path"), P = require("./png");
const RAW = path.join(__dirname, "..", "src", "assets", "raw"), OUT = path.join(__dirname, "..", "src", "assets", "game");
fs.mkdirSync(OUT, { recursive: true });

// Kích thước đích: đủ nét khi hiện ở cỡ lớn nhất trong game (Chưởng môn cao 300 px trên khung 1080x1920).
const TARGETS = {
  character: { h: 480 },        // nhân vật: 1122x1402 -> 384x480
  seat_chuongMon: { w: 640 },
  seat_hoPhap: { w: 460 },
  title_NgaMi: { w: 900 },
  boss_dragon: { w: 900 },            // quái rồng: hiện khoảng 640 px, để 900 cho nét
  boss_dragon_hurt: { w: 900 },
  boss_dragon_defeated: { w: 900 },
  tauHoaNhapMa: { h: 700 },           // người tẩu hỏa nhập ma: hiện to ở chỗ quái (khoảng 560 px)
  bg_main: { copy: true },      // nền gốc 941x1672 nhỏ hơn khung: giữ nguyên, để trình duyệt tự phóng mượt
  bg_main_wide: { skip: true }, // nền rộng 2 bên: tools/build-ban3.py nén thành bg_main_wide.jpg
  // Bản 2 (12/11): kiếm bay, mây thiên kiếp, pháp bảo, trứng, linh thú.
  fx_sword: { w: 480, trim: true },   // kiếm bay: cắt bỏ viền trong suốt (ảnh gốc 2172x724, kiếm chỉ cao ~330) cho lưỡi kiếm không bị mảnh
  fx_storm_cloud: { w: 620 },
  item: { w: 260 },                   // pháp bảo hiện tối đa khoảng 240 px
  egg: { w: 260 }, egg_crack: { w: 260 },
  pet: { h: 240 }                     // linh thú: cạnh chân (nhỏ) và lúc vừa nở (tối đa khoảng 220 px)
};
const CHARACTERS = ["luyenKhi", "trucCo", "ketDan", "nguyenAnh", "hoaThan", "luyenHu", "hopThe", "daiThua", "doKiep", "chanTien"];

// Tên file gốc khác tên game dùng: dragonBoss.png (anh đặt) -> boss_dragon.png.
const ALIAS = { dragonBoss: "boss_dragon" };
for (const file of fs.readdirSync(RAW).filter((f) => f.endsWith(".png"))) {
  const raw = file.replace(/\.png$/, ""), key = ALIAS[raw] || raw;
  const t = CHARACTERS.includes(key) ? TARGETS.character : TARGETS[key] || (key.startsWith("item_") ? TARGETS.item : key.startsWith("pet_") ? TARGETS.pet : null);
  if (!t) { console.warn("bỏ qua (không có trong danh sách):", file); continue; }
  if (t.skip) continue;
  if (t.copy) { fs.copyFileSync(path.join(RAW, file), path.join(OUT, key + ".png")); console.log(file.padEnd(22), "giữ nguyên"); continue; }
  let img = P.decode(path.join(RAW, file));
  if (t.trim) img = trim(img, 6);
  // Tranh AI hay có alpha 251-254 ở phần thân: làm đặc hẳn để nhân vật không bị trong mờ 1-2%.
  for (let i = 3; i < img.px.length; i += 4) if (img.px[i] >= 248) img.px[i] = 255;
  const scale = t.h ? t.h / img.H : t.w / img.W;
  const out = P.resize(img, Math.round(img.W * scale), Math.round(img.H * scale));
  const data = P.encode(out);
  fs.writeFileSync(path.join(OUT, key + ".png"), data);
  console.log(file.padEnd(22), `${img.W}x${img.H} -> ${out.W}x${out.H}`, (data.length / 1024).toFixed(0) + " KB");
}
/** Cắt bỏ viền trong suốt quanh vật thể (chừa pad px). */
function trim(img, pad) {
  const [x0, y0, x1, y1] = P.stats(img).bbox, X = Math.max(0, x0 - pad), Y = Math.max(0, y0 - pad);
  const W = Math.min(img.W, x1 + pad + 1) - X, H = Math.min(img.H, y1 + pad + 1) - Y, px = Buffer.alloc(W * H * 4);
  for (let y = 0; y < H; y++) img.px.copy(px, y * W * 4, ((Y + y) * img.W + X) * 4, ((Y + y) * img.W + X + W) * 4);
  return { W, H, px };
}
// Âm thanh (mp3 / ogg / wav): chép nguyên sang bản game.
for (const file of fs.readdirSync(RAW).filter((f) => /\.(mp3|ogg|wav)$/i.test(f))) {
  fs.copyFileSync(path.join(RAW, file), path.join(OUT, file));
  console.log(file.padEnd(22), "âm thanh, chép nguyên", (fs.statSync(path.join(OUT, file)).size / 1024).toFixed(0) + " KB");
}
const missing = CHARACTERS.filter((c) => !fs.existsSync(path.join(RAW, c + ".png")));
if (missing.length) console.warn("THIẾU tranh nhân vật:", missing.join(", "), "-> game tạm dùng bộ của cảnh giới liền trước");
