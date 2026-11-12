// Tạo dữ liệu khởi đầu đóng kèm bộ cài: vendor/seed/{tiktool.db, mappings.json, assets/}
// - tiktool.db: chỉ bảng gift_catalog (danh sách quà), KHÔNG có event_log/session của máy build.
// - Ảnh quà tải từ CDN TikTok về assets/gifts/ để app có ảnh quà ngay cả khi link CDN hết hạn.
// - mappings.json + ảnh trong data/assets: cấu hình hiệu ứng hiện tại.
// Chạy: npm run build:seed  (cần mạng để tải ảnh quà; ảnh tải lỗi thì giữ link CDN cũ)
const fs = require("fs");
const path = require("path");
const { DatabaseSync } = require("node:sqlite");

const ROOT = path.join(__dirname, ".."), SRC = path.join(ROOT, "data"), OUT = path.join(ROOT, "vendor", "seed");
const GIFTS_DIR = path.join(OUT, "assets", "gifts");

async function download(url, base) {
  const res = await fetch(url, { signal: AbortSignal.timeout(20000) });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  const type = res.headers.get("content-type") || "";
  const ext = type.includes("png") ? ".png" : type.includes("jpeg") ? ".jpg" : type.includes("gif") ? ".gif" : ".webp";
  const name = `${base}${ext}`;
  fs.writeFileSync(path.join(GIFTS_DIR, name), Buffer.from(await res.arrayBuffer()));
  return `/assets/gifts/${name}`;
}

async function main() {
  fs.rmSync(OUT, { recursive: true, force: true });
  fs.mkdirSync(GIFTS_DIR, { recursive: true });

  const src = new DatabaseSync(path.join(SRC, "tiktool.db"), { readOnly: true });
  const gifts = src.prepare("SELECT gift_id, name, diamonds, image_url, last_seen_at FROM gift_catalog").all();
  src.close();

  // Tải ảnh quà (8 luồng song song). Cùng một URL chỉ tải một lần.
  const byUrl = new Map(), urls = [...new Set(gifts.map((g) => g.image_url).filter((u) => /^https?:\/\//.test(u || "")))];
  let done = 0, failed = 0;
  const queue = urls.map((url, i) => ({ url, base: gifts.find((g) => g.image_url === url).gift_id.replace(/[^\w-]/g, "_") || `gift-${i}` }));
  await Promise.all(Array.from({ length: 8 }, async () => {
    for (let job; (job = queue.shift());) {
      try { byUrl.set(job.url, await download(job.url, job.base)); } catch (e) { failed++; console.warn(`  bỏ qua ${job.base}: ${e.message}`); }
      if (++done % 100 === 0) console.log(`  ảnh quà ${done}/${urls.length}`);
    }
  }));

  const db = new DatabaseSync(path.join(OUT, "tiktool.db"));
  db.exec("CREATE TABLE gift_catalog (gift_id TEXT PRIMARY KEY, name TEXT NOT NULL, diamonds INTEGER, image_url TEXT, last_seen_at TEXT NOT NULL)");
  const insert = db.prepare("INSERT INTO gift_catalog VALUES (?, ?, ?, ?, ?)");
  for (const g of gifts) insert.run(g.gift_id, g.name, g.diamonds, byUrl.get(g.image_url) || g.image_url, g.last_seen_at);
  db.close();

  // Mapping: đổi ảnh quà sang bản local nếu đã tải được.
  const mappings = JSON.parse(fs.readFileSync(path.join(SRC, "mappings.json"), "utf8"));
  for (const rule of mappings) if (rule.giftImage && byUrl.has(rule.giftImage)) rule.giftImage = byUrl.get(rule.giftImage);
  fs.writeFileSync(path.join(OUT, "mappings.json"), `${JSON.stringify(mappings, null, 2)}\n`);

  // Ảnh hiệu ứng người dùng đã thêm (data/assets), trừ thư mục gifts vừa tạo.
  for (const entry of fs.readdirSync(path.join(SRC, "assets"), { withFileTypes: true })) {
    if (entry.isFile()) fs.copyFileSync(path.join(SRC, "assets", entry.name), path.join(OUT, "assets", entry.name));
  }

  console.log(`OK: ${gifts.length} quà (${byUrl.size} ảnh tải về, ${failed} lỗi), ${mappings.length} mapping -> vendor/seed`);
}

main().catch((e) => { console.error(e); process.exit(1); });
